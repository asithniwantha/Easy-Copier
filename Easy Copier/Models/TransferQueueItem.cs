using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Models
{
    /// <summary>
    /// Specifies the execution status of a item or batch in the transfer queue.
    /// </summary>
    public enum TransferQueueItemStatus
    {
        /// <summary>The transfer item is waiting in the queue.</summary>
        Queued,

        /// <summary>The transfer operation is currently actively copying data.</summary>
        InProgress,

        /// <summary>The transfer operation completed successfully.</summary>
        Completed,

        /// <summary>The transfer operation failed due to an error.</summary>
        Failed,

        /// <summary>The transfer operation was cancelled by the user.</summary>
        Cancelled
    }

    /// <summary>
    /// Represents a queued transfer job batch in the queue UI with progress monitoring and status tracking.
    /// </summary>
    /// <param name="items">The items included in this transfer batch.</param>
    /// <param name="targetDrive">The target removable drive destination.</param>
    /// <param name="destinationPath">The destination root path on the target drive.</param>
    /// <param name="totalPrice">The total calculated price for all items in the batch.</param>
    public partial class TransferQueueItem(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath, int totalPrice) : ObservableObject
    {
        /// <summary>
        /// Gets the unique identifier for this queue item job.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the list of transfer items included in this batch.
        /// </summary>
        public IReadOnlyList<TransferItem> Items { get; } = items;

        /// <summary>
        /// Gets the target drive destination details.
        /// </summary>
        public RemovableDrive TargetDrive { get; } = targetDrive;

        /// <summary>
        /// Gets the destination directory path on the target volume.
        /// </summary>
        public string DestinationPath { get; } = destinationPath;

        /// <summary>
        /// Gets the timestamp when this batch was added to the transfer queue.
        /// </summary>
        public DateTime EnqueuedAt { get; } = DateTime.Now;

        /// <summary>
        /// Gets the total byte size of all items in this transfer batch.
        /// </summary>
        public long TotalBytes { get; } = items.Sum(i => i.Game.TotalBytes);

        /// <summary>
        /// Gets the total calculated price for this batch transfer.
        /// </summary>
        public int TotalPrice { get; } = totalPrice;

        /// <summary>
        /// Gets or sets the current execution status of the queue item.
        /// </summary>
        [ObservableProperty]
        public partial TransferQueueItemStatus Status { get; set; } = TransferQueueItemStatus.Queued;

        /// <summary>
        /// Gets or sets a user-visible message describing the current queue status or error reason.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = "Waiting in queue...";

        /// <summary>
        /// Gets or sets the overall completion percentage of the active transfer (0 to 100).
        /// </summary>
        [ObservableProperty]
        public partial double ProgressPercentage { get; set; }

        /// <summary>
        /// Gets or sets the formatted real-time transfer speed string (e.g. "25 MB/s").
        /// </summary>
        [ObservableProperty]
        public partial string SpeedText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the formatted estimated remaining transfer time string.
        /// </summary>
        [ObservableProperty]
        public partial string RemainingTimeText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the transfer completed or failed.
        /// </summary>
        [ObservableProperty]
        public partial DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets a summary text describing the items in the batch (either single item name or count).
        /// </summary>
        public string ItemsSummary => Items.Count == 1
            ? Items[0].Game.Name
            : $"{Items.Count} items";

        /// <summary>
        /// Gets the Segoe MDL2 Assets icon glyph matching the current transfer status.
        /// </summary>
        public string StatusGlyph => Status switch
        {
            TransferQueueItemStatus.Queued => "\uE823",
            TransferQueueItemStatus.InProgress => "\uE895",
            TransferQueueItemStatus.Completed => "\uE73E",
            TransferQueueItemStatus.Failed => "\uE783",
            TransferQueueItemStatus.Cancelled => "\uE711",
            _ => "\uE9CE"
        };

        /// <summary>
        /// Gets a value indicating whether this item is currently queued or in progress.
        /// </summary>
        public bool IsActive => Status is TransferQueueItemStatus.Queued or TransferQueueItemStatus.InProgress;

        /// <summary>
        /// Partial method invoked when <see cref="Status"/> changes to notify UI of dependent property changes.
        /// </summary>
        /// <param name="value">The new status value.</param>
        partial void OnStatusChanged(TransferQueueItemStatus value)
        {
            OnPropertyChanged(nameof(StatusGlyph));
            OnPropertyChanged(nameof(IsActive));
        }
    }
}
