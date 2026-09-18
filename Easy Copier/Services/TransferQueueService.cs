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
    /// <summary>
    /// Defines operations for managing the background transfer queue, enqueuing transfer items, and listening for completion events.
    /// </summary>
    public interface ITransferQueueService
    {
        /// <summary>
        /// Gets the observable collection of transfer items currently in queue or recently completed.
        /// </summary>
        ObservableCollection<TransferQueueItem> QueueItems { get; }

        /// <summary>
        /// Enqueues a batch of transfer items targeting a specific drive.
        /// </summary>
        /// <param name="items">Collection of transfer items to execute.</param>
        /// <param name="targetDrive">The target drive information.</param>
        /// <param name="destinationPath">The destination root directory path on the drive.</param>
        /// <returns>The created <see cref="TransferQueueItem"/> instance.</returns>
        TransferQueueItem Enqueue(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath);

        /// <summary>
        /// Calculates total bytes reserved by currently queued/active transfers targeting a specific drive letter.
        /// </summary>
        /// <param name="driveLetter">Target drive letter identifier.</param>
        /// <returns>Total active reserved bytes.</returns>
        long GetReservedBytes(string driveLetter);

        /// <summary>
        /// Clears all completed or failed items from the active transfer queue collection.
        /// </summary>
        void ClearFinished();

        /// <summary>
        /// Event raised when an individual transfer queue item finishes processing.
        /// </summary>
        event EventHandler<TransferQueueItem>? ItemCompleted;

        /// <summary>
        /// Event raised when an entire drive batch queue finishes processing, providing title and message details.
        /// </summary>
        event EventHandler<(string Title, string Message)>? BatchCompleted;
    }

    /// <summary>
    /// Service managing background asynchronous file transfer queues with per-drive channels, sound alerts, and toast notifications.
    /// </summary>
    public class TransferQueueService : ITransferQueueService
    {
        /// <summary>
        /// File transfer service instance used to execute operations.
        /// </summary>
        private readonly IFileTransferService _fileTransferService;

        /// <summary>
        /// Logger instance used for diagnostic logging.
        /// </summary>
        private readonly ILogger<TransferQueueService> _logger;

        /// <summary>
        /// Main unbound channel holding incoming queue items.
        /// </summary>
        private readonly Channel<TransferQueueItem> _channel = Channel.CreateUnbounded<TransferQueueItem>();

        /// <summary>
        /// Concurrent dictionary mapping drive keys to per-drive transfer channels to execute transfers sequentially per drive.
        /// </summary>
        private readonly ConcurrentDictionary<string, Channel<TransferQueueItem>> _driveChannels = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Dispatcher service for marshaling updates onto the UI thread.
        /// </summary>
        private readonly Infrastructure.IDispatcherService _dispatcherService;

        /// <summary>
        /// Observable collection of queued transfer items bound to UI views.
        /// </summary>
        public ObservableCollection<TransferQueueItem> QueueItems { get; } = [];

        /// <summary>
        /// Event raised when a single queue item completes execution.
        /// </summary>
        public event EventHandler<TransferQueueItem>? ItemCompleted;

        /// <summary>
        /// Event raised when all items for a target drive complete.
        /// </summary>
        public event EventHandler<(string Title, string Message)>? BatchCompleted;

        /// <summary>
        /// Settings service for loading notification and pricing settings.
        /// </summary>
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// Audio playback service for sound notifications.
        /// </summary>
        private readonly IAudioPlaybackService _audioPlaybackService;

        /// <summary>
        /// Process service for checking privilege levels.
        /// </summary>
        private readonly Infrastructure.IProcessService _processService;

        /// <summary>
        /// Dialog service for prompting user dialogs.
        /// </summary>
        private readonly Infrastructure.IDialogService _dialogService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransferQueueService"/> class and starts background consumer tasks.
        /// </summary>
        /// <param name="fileTransferService">File transfer service implementation.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="dispatcherService">UI dispatcher service.</param>
        /// <param name="settingsService">Settings service.</param>
        /// <param name="audioPlaybackService">Audio playback service.</param>
        /// <param name="processService">Process privilege service.</param>
        /// <param name="dialogService">Dialog prompt service.</param>
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

        /// <summary>
        /// Enqueues a batch transfer request targeting a specific drive.
        /// </summary>
        /// <param name="items">List of items to transfer.</param>
        /// <param name="targetDrive">Target drive details.</param>
        /// <param name="destinationPath">Root path on target drive.</param>
        /// <returns>The created <see cref="TransferQueueItem"/> instance.</returns>
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

        /// <summary>
        /// Gets total bytes allocated by active transfers targeting the specified drive letter.
        /// </summary>
        /// <param name="driveLetter">Target drive letter.</param>
        /// <returns>Total active reserved bytes.</returns>
        public long GetReservedBytes(string driveLetter)
        {
            return QueueItems
                .Where(i => i.IsActive && string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.TotalBytes);
        }

        /// <summary>
        /// Removes completed, failed, or cancelled items from the visible queue collection.
        /// </summary>
        public void ClearFinished()
        {
            List<TransferQueueItem> toRemove = [.. QueueItems.Where(i => !i.IsActive)];
            foreach (TransferQueueItem item in toRemove)
            {
                _ = QueueItems.Remove(item);
            }
        }

        /// <summary>
        /// Main background loop reading items from the primary channel and routing them to per-drive execution channels.
        /// </summary>
        /// <returns>A task representing the background queue processing operation.</returns>
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

        /// <summary>
        /// Per-drive loop processing queued transfers sequentially for a single drive letter.
        /// </summary>
        /// <param name="driveKey">Drive letter identifier key.</param>
        /// <param name="driveChannel">Drive-specific channel.</param>
        /// <returns>A task representing the processing loop.</returns>
        private async Task ProcessDriveQueueAsync(string driveKey, Channel<TransferQueueItem> driveChannel)
        {
            await foreach (TransferQueueItem item in driveChannel.Reader.ReadAllAsync())
            {
                await ProcessItemAsync(item);
            }

            _ = _driveChannels.TryRemove(driveKey, out _);
        }

        /// <summary>
        /// Executes an individual transfer item and updates its status and progress properties on the UI thread.
        /// </summary>
        /// <param name="item">The transfer queue item to process.</param>
        /// <returns>A task representing the execution.</returns>
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

        /// <summary>
        /// Checks if all queued items targeting a drive have finished and triggers completion sounds and toast notifications.
        /// </summary>
        /// <param name="driveLetter">Target drive letter.</param>
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

        /// <summary>
        /// Builds and displays a Windows AppNotification toast message summarizing batch transfer completion.
        /// </summary>
        /// <param name="driveLetter">Target drive letter.</param>
        /// <param name="batchItems">List of batch transfer queue items.</param>
        /// <param name="isSuccess"><c>true</c> if all batch items succeeded; otherwise, <c>false</c>.</param>
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

        /// <summary>
        /// Normalizes a drive letter string for dictionary keys.
        /// </summary>
        /// <param name="driveLetter">Input drive letter.</param>
        /// <returns>Normalized drive letter key.</returns>
        private static string NormalizeDriveKey(string driveLetter)
        {
            return (driveLetter ?? string.Empty).Trim().TrimEnd('\\').ToUpperInvariant();
        }

        /// <summary>
        /// Executes an action on the UI thread using <see cref="Infrastructure.IDispatcherService"/>.
        /// </summary>
        /// <param name="action">Action delegate to execute.</param>
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
