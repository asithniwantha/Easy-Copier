using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Easy_Copier.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Easy_Copier.Tests;

public class MainViewModelLifecycleTests
{
    [Fact]
    public void Dispose_StopsDriveWatching_AndIgnoresLateDriveNotifications()
    {
        TestDriveDiscoveryService driveDiscoveryService = new();
        TestDispatcherService dispatcherService = new();
        TestTransferQueueService transferQueueService = new();

        MainViewModel viewModel = CreateViewModel(driveDiscoveryService, dispatcherService, transferQueueService);

        viewModel.Dispose();
        driveDiscoveryService.RaiseDrivesChanged();

        Assert.Equal(1, driveDiscoveryService.StopWatchingCallCount);
        Assert.Equal(0, dispatcherService.EnqueuedActionCount);
        Assert.Equal(0, dispatcherService.EnqueuedTaskCount);
    }

    [Fact]
    public void Dispose_IgnoresLateQueueCompletionCallbacks()
    {
        TestDriveDiscoveryService driveDiscoveryService = new();
        TestDispatcherService dispatcherService = new();
        TestTransferQueueService transferQueueService = new();

        MainViewModel viewModel = CreateViewModel(driveDiscoveryService, dispatcherService, transferQueueService);
        viewModel.StatusMessage = "Ready";

        viewModel.Dispose();
        transferQueueService.RaiseItemCompleted(CreateCompletedQueueItem());

        Assert.Equal("Ready", viewModel.StatusMessage);
    }

    private static MainViewModel CreateViewModel(
        IDriveDiscoveryService driveDiscoveryService,
        IDispatcherService dispatcherService,
        ITransferQueueService transferQueueService)
    {
        SmartAdderViewModel smartAdderViewModel = new(
            Mock.Of<IWindowService>(),
            Mock.Of<ISmartAdderHistoryService>(),
            Mock.Of<ILogger<SmartAdderViewModel>>());

        return new MainViewModel(
            Mock.Of<ILogger<MainViewModel>>(),
            Mock.Of<IUpdateService>(),
            Mock.Of<ISettingsService>(),
            Mock.Of<ILibraryCacheService>(),
            Mock.Of<ILibraryScannerService>(),
            driveDiscoveryService,
            Mock.Of<IDriveValidationService>(),
            Mock.Of<IFileTransferService>(),
            transferQueueService,
            Mock.Of<IWindowService>(),
            Mock.Of<IProcessService>(),
            dispatcherService,
            Mock.Of<ISourceLibraryService>(),
            Mock.Of<IDialogService>(),
            smartAdderViewModel);
    }

    private static TransferQueueItem CreateCompletedQueueItem()
    {
        GameEntry game = new("Test Game", @"C:\Games\Test Game", 1024, null, DateTime.Now, false);
        TransferItem transferItem = new(game);
        RemovableDrive drive = new("E:", "USB", "exFAT", 1024 * 1024, 512 * 1024, 50, "Test Drive");
        TransferQueueItem queueItem = new([transferItem], drive, @"E:\", 100)
        {
            Status = TransferQueueItemStatus.Completed,
            StatusMessage = "Done"
        };

        return queueItem;
    }

    private sealed class TestDriveDiscoveryService : IDriveDiscoveryService
    {
        public int StopWatchingCallCount { get; private set; }

        public event EventHandler? DrivesChanged;

        public Task<IReadOnlyList<RemovableDrive>> GetRemovableDrivesAsync()
        {
            return Task.FromResult<IReadOnlyList<RemovableDrive>>([]);
        }

        public void StartWatching()
        {
        }

        public void StopWatching()
        {
            StopWatchingCallCount++;
        }

        public void RaiseDrivesChanged()
        {
            DrivesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
        }
    }

    private sealed class TestTransferQueueService : ITransferQueueService
    {
        public ObservableCollection<TransferQueueItem> QueueItems { get; } = [];

        public event EventHandler<TransferQueueItem>? ItemCompleted;

        public TransferQueueItem Enqueue(IReadOnlyList<TransferItem> items, RemovableDrive targetDrive, string destinationPath)
        {
            throw new NotSupportedException();
        }

        public long GetReservedBytes(string driveLetter)
        {
            return 0;
        }

        public void ClearFinished()
        {
        }

        public void RaiseItemCompleted(TransferQueueItem item)
        {
            ItemCompleted?.Invoke(this, item);
        }
    }

    private sealed class TestDispatcherService : IDispatcherService
    {
        public bool HasThreadAccess => false;

        public int EnqueuedActionCount { get; private set; }

        public int EnqueuedTaskCount { get; private set; }

        public bool TryEnqueue(Action action)
        {
            EnqueuedActionCount++;
            action();
            return true;
        }

        public bool TryEnqueue(Func<Task> action)
        {
            EnqueuedTaskCount++;
            action().GetAwaiter().GetResult();
            return true;
        }
    }
}
