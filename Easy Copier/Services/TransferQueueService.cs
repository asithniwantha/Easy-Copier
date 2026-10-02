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
        event EventHandler<(string Title, string Message, bool IsSuccess)>? BatchCompleted;
    }

    /// <summary>
    /// Service managing background asynchronous file transfer queues with per-drive channels, delegating notifications to <see cref="IBatchNotificationService"/>.
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
        /// Settings service for loading notification and pricing settings.
        /// </summary>
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// Batch notification service for audio and desktop notification dispatching.
        /// </summary>
        private readonly IBatchNotificationService _batchNotificationService;

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
        public event EventHandler<(string Title, string Message, bool IsSuccess)>? BatchCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransferQueueService"/> class and starts background consumer tasks.
        /// </summary>
        /// <param name="fileTransferService">File transfer service implementation.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="dispatcherService">UI dispatcher service.</param>
        /// <param name="settingsService">Settings service.</param>
        /// <param name="batchNotificationService">Batch notification service.</param>
        public TransferQueueService(
            IFileTransferService fileTransferService,
            ILogger<TransferQueueService> logger,
            Infrastructure.IDispatcherService dispatcherService,
            ISettingsService settingsService,
            IBatchNotificationService batchNotificationService)
        {
            _fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _batchNotificationService = batchNotificationService ?? throw new ArgumentNullException(nameof(batchNotificationService));

            _batchNotificationService.BatchCompleted += (sender, args) => BatchCompleted?.Invoke(sender, args);

            _ = Task.Run(ProcessQueueAsync);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public long GetReservedBytes(string driveLetter)
        {
            return QueueItems
                .Where(i => i.IsActive && string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.TotalBytes);
        }

        /// <inheritdoc />
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

                _batchNotificationService.EvaluateAndNotifyBatchCompletion(item.TargetDrive.DriveLetter, QueueItems);
            });
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
