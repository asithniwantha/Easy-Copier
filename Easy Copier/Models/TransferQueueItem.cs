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

    public partial class TransferQueueItem : ObservableObject
    {
        public string Id { get; }
        public IReadOnlyList<TransferItem> Items { get; }
        public RemovableDrive TargetDrive { get; }
        public string DestinationPath { get; }
        public DateTime EnqueuedAt { get; }
        public long TotalBytes { get; }
        public int TotalPrice { get; }

        [ObservableProperty]
        private TransferQueueItemStatus _status= TransferQueueItemStatus.Queued;

        [ObservableProperty]
        private string _statusMessage = "Waiting in queue...";

        [ObservableProperty]
        private DateTime? _completedAt;

        public TransferQueueItem(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath, int totalPrice)
        {
            Id = Guid.NewGuid().ToString();
            Items = items;
            TargetDrive = targetDrive;
            DestinationPath = destinationPath;
            TotalPrice = totalPrice;
            EnqueuedAt = DateTime.Now;
            TotalBytes = items.Sum(i => i.Game.TotalBytes);
        }

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
