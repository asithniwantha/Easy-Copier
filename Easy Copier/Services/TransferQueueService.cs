using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
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
        private readonly DispatcherQueue? _dispatcherQueue;

        public ObservableCollection<TransferQueueItem> QueueItems { get; } = [];

        public event EventHandler<TransferQueueItem>? ItemCompleted;

        public TransferQueueService(IFileTransferService fileTransferService, ILogger<TransferQueueService> logger)
        {
            _fileTransferService = fileTransferService;
            _logger = logger;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _ = Task.Run(ProcessQueueAsync);
        }

        public TransferQueueItem Enqueue(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(targetDrive);

            TransferQueueItem item = new(items, targetDrive, destinationPath);

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
                TransferRequest request = new(item.Items, item.TargetDrive, item.DestinationPath);
                outcome = await _fileTransferService.TransferGamesAsync(request);
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
                }
                else
                {
                    item.Status = TransferQueueItemStatus.Failed;
                    item.StatusMessage = "Transfer failed unexpectedly";
                }

                item.CompletedAt = DateTime.Now;
                ItemCompleted?.Invoke(this, item);
            });
        }

        private static string NormalizeDriveKey(string driveLetter)
        {
            return (driveLetter ?? string.Empty).Trim().TrimEnd('\\').ToUpperInvariant();
        }

        private void RunOnUiThread(Action action)
        {
            if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
            {
                _ = _dispatcherQueue.TryEnqueue(() => action());
            }
            else
            {
                action();
            }
        }
    }
}
