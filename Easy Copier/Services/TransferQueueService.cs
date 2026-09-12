using Easy_Copier.Models;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Easy_Copier.Services
{
    public interface ITransferQueueService
    {
        ObservableCollection<TransferQueueItem> QueueItems { get; }

        TransferQueueItem Enqueue(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath);

        long GetReservedBytes(string driveLetter);

        void ClearFinished();

        event EventHandler<TransferQueueItem>? ItemCompleted;
        event EventHandler<(string Title, string Message)>? BatchCompleted;
    }

    public class TransferQueueService : ITransferQueueService
    {
        private readonly IFileTransferService _fileTransferService;
        private readonly ILogger<TransferQueueService> _logger;
        private readonly Channel<TransferQueueItem> _channel = Channel.CreateUnbounded<TransferQueueItem>();
        private readonly ConcurrentDictionary<string, Channel<TransferQueueItem>> _driveChannels = new(StringComparer.OrdinalIgnoreCase);
        private readonly Infrastructure.IDispatcherService _dispatcherService;

        public ObservableCollection<TransferQueueItem> QueueItems { get; } = [];

        public event EventHandler<TransferQueueItem>? ItemCompleted;
        public event EventHandler<(string Title, string Message)>? BatchCompleted;

        private readonly ISettingsService _settingsService;
        private readonly IAudioPlaybackService _audioPlaybackService;
        private readonly Infrastructure.IProcessService _processService;
        private readonly Infrastructure.IDialogService _dialogService;

        public TransferQueueService(IFileTransferService fileTransferService, ILogger<TransferQueueService> logger, Infrastructure.IDispatcherService dispatcherService, ISettingsService settingsService, IAudioPlaybackService audioPlaybackService, Infrastructure.IProcessService processService, Infrastructure.IDialogService dialogService)
        {
            _fileTransferService = fileTransferService;
            _logger = logger;
            _dispatcherService = dispatcherService;
            _settingsService = settingsService;
            _audioPlaybackService = audioPlaybackService;
            _processService = processService;
            _dialogService = dialogService;

            _ = Task.Run(ProcessQueueAsync);
        }

        public TransferQueueItem Enqueue(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(targetDrive);

            AppSettings settings = _settingsService.LoadSettingsSync();
            int totalPrice = items.Sum(i => Infrastructure.FormattingHelpers.CalculatePrice(i.Game.TotalBytes, settings));

            TransferQueueItem item = new(items, targetDrive, destinationPath, totalPrice);

            QueueItems.Add(item);
            _ = _channel.Writer.TryWrite(item);

            _logger.LogInformation(
                "Enqueued transfer of {Count} item(s) to {Drive} (queue depth: {Depth})",
                items.Count, targetDrive.DriveLetter, QueueItems.Count);

            return item;
        }

        public long GetReservedBytes(string driveLetter)
        {
            return QueueItems
                .Where(i => i.IsActive && string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.TotalBytes);
        }

        public void ClearFinished()
        {
            List<TransferQueueItem> toRemove = [.. QueueItems.Where(i => !i.IsActive)];
            foreach (TransferQueueItem item in toRemove)
            {
                _ = QueueItems.Remove(item);
            }
        }

        private async Task ProcessQueueAsync()
        {
            await foreach (TransferQueueItem item in _channel.Reader.ReadAllAsync())
            {
                string driveKey = NormalizeDriveKey(item.TargetDrive.DriveLetter);
                Channel<TransferQueueItem> driveChannel = _driveChannels.GetOrAdd(driveKey, key =>
                {
                    Channel<TransferQueueItem> channel = Channel.CreateUnbounded<TransferQueueItem>();
                    _ = Task.Run(() => ProcessDriveQueueAsync(key, channel));
                    return channel;
                });

                _ = driveChannel.Writer.TryWrite(item);
            }
        }

        private async Task ProcessDriveQueueAsync(string driveKey, Channel<TransferQueueItem> driveChannel)
        {
            await foreach (TransferQueueItem item in driveChannel.Reader.ReadAllAsync())
            {
                await ProcessItemAsync(item);
            }

            _ = _driveChannels.TryRemove(driveKey, out _);
        }

        private async Task ProcessItemAsync(TransferQueueItem item)
        {
            RunOnUiThread(() =>
            {
                item.Status = TransferQueueItemStatus.InProgress;
                item.StatusMessage = $"Copying to {item.TargetDrive.DriveLetter}...";
            });

            TransferOutcome? outcome = null;

            try
            {
                Progress<TransferProgress> progress = new(p =>
                {
                    RunOnUiThread(() =>
                    {
                        item.ProgressPercentage = p.Percentage;
                        item.SpeedText = p.SpeedText;
                        item.RemainingTimeText = p.RemainingTimeText;
                    });
                });

                TransferRequest request = new(item.Items, item.TargetDrive, item.DestinationPath);
                outcome = await _fileTransferService.TransferGamesAsync(request, progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue item transfer failed for {Drive}", item.TargetDrive.DriveLetter);
            }

            RunOnUiThread(() =>
            {
                if (outcome != null)
                {
                    item.Status = outcome.Success ? TransferQueueItemStatus.Completed : TransferQueueItemStatus.Failed;
                    item.StatusMessage = outcome.Message;

                    if (outcome.Success)
                    {
                        item.ProgressPercentage = 100.0;
                        item.SpeedText = string.Empty;
                        item.RemainingTimeText = string.Empty;
                    }
                }
                else
                {
                    item.Status = TransferQueueItemStatus.Failed;
                    item.StatusMessage = "Transfer failed unexpectedly";
                }

                item.CompletedAt = DateTime.Now;
                ItemCompleted?.Invoke(this, item);

                CheckAndNotifyBatchCompletion(item.TargetDrive.DriveLetter);
            });
        }

        private void CheckAndNotifyBatchCompletion(string driveLetter)
        {
            AppSettings settings = _settingsService.LoadSettingsSync();

            // Check if there are any active items left for this specific drive
            int activeItemsForDrive = QueueItems.Count(i =>
                i.IsActive && string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase));

            if (activeItemsForDrive == 0)
            {
                var batchItems = QueueItems.Where(i =>
                    !i.IsActive &&
                    string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (batchItems.Count == 0)
                    return;

                // We consider it a "failure" if ANY item in the queue for this drive has a Failed or Cancelled status.
                // NOTE: Once a user hits 'Clear Finished', those items are gone, so this evaluates only the currently visible batch.
                bool anyFailedOrCancelled = batchItems.Any(i => i.Status == TransferQueueItemStatus.Failed || i.Status == TransferQueueItemStatus.Cancelled);

                if (settings.PlayNotificationSounds)
                {
                    if (anyFailedOrCancelled)
                    {
                        _audioPlaybackService.PlayFailureSound();
                    }
                    else
                    {
                        _audioPlaybackService.PlaySuccessSound();
                    }
                }

                if (settings.ShowDesktopNotifications)
                {
                    ShowDesktopNotification(driveLetter, batchItems, !anyFailedOrCancelled);
                }
            }
        }

        private void ShowDesktopNotification(string driveLetter, List<TransferQueueItem> batchItems, bool isSuccess)
        {
            try
            {
                var firstItem = batchItems.First();
                long totalDriveCapacity = firstItem.TargetDrive.TotalBytes;
                string statusText = isSuccess ? "Complete" : "Failed";
                string title = $"{firstItem.TargetDrive.DriveLetter} - {Infrastructure.FormattingHelpers.FormatBytes(totalDriveCapacity)} Capacity - {statusText}";

                long totalBytes = batchItems.Sum(x => x.TotalBytes);
                int totalPrice = batchItems.Sum(x => x.TotalPrice);
                var allGames = batchItems.SelectMany(x => x.Items).Select(x => x.Game.Name).ToList();
                int totalItems = allGames.Count;

                string namesText;
                if (totalItems > 3)
                {
                    namesText = $"{totalItems} items: {string.Join(", ", allGames.Take(3))} and {totalItems - 3} more.";
                }
                else
                {
                    namesText = $"{totalItems} items: {string.Join(", ", allGames)}.";
                }

                string body = $"{namesText} Size: {Infrastructure.FormattingHelpers.FormatBytes(totalBytes)}. Price: Rs. {totalPrice}";

                BatchCompleted?.Invoke(this, (title, body));

                var builder = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(body);

                _logger.LogInformation("Attempting to show desktop notification: {Title}", title);
                var notification = builder.BuildNotification();
                if (!AppNotificationManager.IsSupported())
                {
                    _logger.LogWarning("AppNotificationManager.IsSupported() returned false. Skipping toast display.");

                    if (_processService.IsRunningAsAdministrator())
                    {
                        _logger.LogWarning("Application is running as Administrator (elevated). Toast notifications are officially not supported by the Windows App SDK in elevated contexts.");
                    }
                    else
                    {
                        _logger.LogWarning("AppNotificationManager is not supported on this OS configuration, but the app is NOT elevated. This usually indicates the Windows App SDK Singleton package is missing or not registered for this self-contained deployment.");
                    }

                    return;
                }

                if (AppNotificationManager.Default.Setting == AppNotificationSetting.DisabledForApplication)
                {
                    _logger.LogWarning("Desktop notifications are disabled for this application by the user or system.");
                }
                else if (AppNotificationManager.Default.Setting == AppNotificationSetting.DisabledForUser)
                {
                    _logger.LogWarning("Desktop notifications are disabled globally for this user profile.");
                }
                else if (AppNotificationManager.Default.Setting == AppNotificationSetting.DisabledByGroupPolicy)
                {
                    _logger.LogWarning("Desktop notifications are disabled by Group Policy.");
                }
                else if (AppNotificationManager.Default.Setting == AppNotificationSetting.DisabledByManifest)
                {
                    _logger.LogWarning("Desktop notifications are disabled by the application manifest.");
                }

                AppNotificationManager.Default.Show(notification);

                if (notification.Id != 0)
                {
                    _logger.LogInformation("Desktop notification shown successfully with ID: {Id}", notification.Id);
                }
                else
                {
                    _logger.LogWarning("AppNotificationManager.Default.Show returned without throwing, but the notification ID is 0 (it may have been silently dropped).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show desktop notification.");
            }
        }

        private static string NormalizeDriveKey(string driveLetter)
        {
            return (driveLetter ?? string.Empty).Trim().TrimEnd('\\').ToUpperInvariant();
        }

        private void RunOnUiThread(Action action)
        {
            if (!_dispatcherService.HasThreadAccess)
            {
                _ = _dispatcherService.TryEnqueue(action);
            }
            else
            {
                action();
            }
        }
    }
}
