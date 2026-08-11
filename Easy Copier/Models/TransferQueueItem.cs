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
        public IReadOnlyList<GameEntry> Games { get; }
        public RemovableDrive TargetDrive { get; }
        public string DestinationPath { get; }
        public DateTime EnqueuedAt { get; }
        public long TotalBytes { get; }

        [ObservableProperty]
        public partial TransferQueueItemStatus Status { get; set; } = TransferQueueItemStatus.Queued;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = "Waiting in queue...";

        [ObservableProperty]
        public partial DateTime? CompletedAt { get; set; }

        public TransferQueueItem(IReadOnlyList<GameEntry> games, RemovableDrive targetDrive, string destinationPath)
        {
            Id = Guid.NewGuid().ToString();
            Games = games;
            TargetDrive = targetDrive;
            DestinationPath = destinationPath;
            EnqueuedAt = DateTime.Now;
            TotalBytes = games.Sum(g => g.TotalBytes);
        }

        public string ItemsSummary => Games.Count == 1
            ? Games[0].Name
            : $"{Games.Count} items";

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
