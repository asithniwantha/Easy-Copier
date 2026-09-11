using Easy_Copier.Models;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface ITransferQueueService
    {
        ObservableCollection<TransferQueueItem> QueueItems { get; }

        TransferQueueItem Enqueue(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath);

        long GetReservedBytes(string driveLetter);

        void ClearFinished();

        event EventHandler<TransferQueueItem>? ItemCompleted;
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

        private readonly ISettingsService _settingsService;
        private readonly IAudioPlaybackService _audioPlaybackService;

        public TransferQueueService(IFileTransferService fileTransferService, ILogger<TransferQueueService> logger, Infrastructure.IDispatcherService dispatcherService, ISettingsService settingsService, IAudioPlaybackService audioPlaybackService)
        {
            _fileTransferService = fileTransferService;
            _logger = logger;
            _dispatcherService = dispatcherService;
            _settingsService = settingsService;
            _audioPlaybackService = audioPlaybackService;

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

                CheckAndPlayCompletionSound(item.TargetDrive.DriveLetter);
            });
        }

        private void CheckAndPlayCompletionSound(string driveLetter)
        {
            AppSettings settings = _settingsService.LoadSettingsSync();
            if (!settings.PlayNotificationSounds)
            {
                return;
            }

            // Check if there are any active items left for this specific drive
            int activeItemsForDrive = QueueItems.Count(i =>
                i.IsActive && string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase));

            if (activeItemsForDrive == 0)
            {
                // We consider it a "failure" if ANY item in the queue for this drive has a Failed or Cancelled status.
                // NOTE: Once a user hits 'Clear Finished', those items are gone, so this evaluates only the currently visible batch.
                bool anyFailedOrCancelled = QueueItems.Any(i =>
                    !i.IsActive &&
                    string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase) &&
                    (i.Status == TransferQueueItemStatus.Failed || i.Status == TransferQueueItemStatus.Cancelled));

                if (anyFailedOrCancelled)
                {
                    _audioPlaybackService.PlayFailureSound();
                }
                else
                {
                    _audioPlaybackService.PlaySuccessSound();
                }
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
