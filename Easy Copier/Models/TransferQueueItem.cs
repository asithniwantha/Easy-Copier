using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Models
{
    public enum TransferQueueItemStatus
    {
        Queued,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    public partial class TransferQueueItem(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath, int totalPrice) : ObservableObject
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public IReadOnlyList<TransferItem> Items { get; } = items;
        public RemovableDrive TargetDrive { get; } = targetDrive;
        public string DestinationPath { get; } = destinationPath;
        public DateTime EnqueuedAt { get; } = DateTime.Now;
        public long TotalBytes { get; } = items.Sum(i => i.Game.TotalBytes);
        public int TotalPrice { get; } = totalPrice;

        [ObservableProperty]
        private TransferQueueItemStatus _status = TransferQueueItemStatus.Queued;

        [ObservableProperty]
        private string _statusMessage = "Waiting in queue...";

        [ObservableProperty]
        private DateTime? _completedAt;

        public string ItemsSummary => Items.Count == 1
            ? Items[0].Game.Name
            : $"{Items.Count} items";

        public string StatusGlyph => Status switch
        {
            TransferQueueItemStatus.Queued => "\uE823",
            TransferQueueItemStatus.InProgress => "\uE895",
            TransferQueueItemStatus.Completed => "\uE73E",
            TransferQueueItemStatus.Failed => "\uE783",
            TransferQueueItemStatus.Cancelled => "\uE711",
            _ => "\uE9CE"
        };

        public bool IsActive => Status is TransferQueueItemStatus.Queued or TransferQueueItemStatus.InProgress;

        partial void OnStatusChanged(TransferQueueItemStatus value)
        {
            OnPropertyChanged(nameof(StatusGlyph));
            OnPropertyChanged(nameof(IsActive));
        }
    }
}
