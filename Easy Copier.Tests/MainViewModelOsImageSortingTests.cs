using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Easy_Copier.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Tests;

public class MainViewModelOsImageSortingTests
{
    [Fact]
    public void OsImages_SortsByName_AscendingAndDescending()
    {
        GameEntry imageA = new("Ubuntu 22.04", @"C:\OS\ubuntu.iso", 3_000_000_000L, null, new DateTime(2023, 1, 1), false, LibraryCategory.OsImage);
        GameEntry imageB = new("Windows 11", @"C:\OS\win11.iso", 5_000_000_000L, null, new DateTime(2024, 1, 1), false, LibraryCategory.OsImage);
        GameEntry imageC = new("Debian 12", @"C:\OS\debian.iso", 1_000_000_000L, null, new DateTime(2022, 1, 1), false, LibraryCategory.OsImage);

        MainViewModel viewModel = CreateViewModelWithOsImages([imageA, imageB, imageC]);

        // Default: Sort by Name, Ascending (A-Z)
        viewModel.SelectedOsImageSortOption = OsImageSortOption.Name;
        Assert.True(viewModel.IsOsImageSortAscending);
        Assert.Equal(["Debian 12", "Ubuntu 22.04", "Windows 11"], viewModel.OsImages.Select(i => i.Name));

        // Toggle direction to Descending
        viewModel.ToggleOsImageSortDirectionCommand.Execute(null);
        Assert.False(viewModel.IsOsImageSortAscending);
        Assert.Equal(["Windows 11", "Ubuntu 22.04", "Debian 12"], viewModel.OsImages.Select(i => i.Name));
    }

    [Fact]
    public void OsImages_SortsByDateAdded_DefaultsToDescending()
    {
        GameEntry imageOld = new("Old OS", @"C:\OS\old.iso", 2_000_000_000L, null, new DateTime(2020, 1, 1), false, LibraryCategory.OsImage);
        GameEntry imageNew = new("New OS", @"C:\OS\new.iso", 2_000_000_000L, null, new DateTime(2025, 1, 1), false, LibraryCategory.OsImage);
        GameEntry imageMid = new("Mid OS", @"C:\OS\mid.iso", 2_000_000_000L, null, new DateTime(2023, 1, 1), false, LibraryCategory.OsImage);

        MainViewModel viewModel = CreateViewModelWithOsImages([imageOld, imageNew, imageMid]);

        // Selecting DateAdded defaults IsOsImageSortAscending to false (Newest First)
        viewModel.SelectedOsImageSortOption = OsImageSortOption.DateAdded;
        Assert.False(viewModel.IsOsImageSortAscending);
        Assert.Equal(["New OS", "Mid OS", "Old OS"], viewModel.OsImages.Select(i => i.Name));

        // Toggle direction to Ascending (Oldest First)
        viewModel.ToggleOsImageSortDirectionCommand.Execute(null);
        Assert.True(viewModel.IsOsImageSortAscending);
        Assert.Equal(["Old OS", "Mid OS", "New OS"], viewModel.OsImages.Select(i => i.Name));
    }

    [Fact]
    public void OsImages_SortsBySize_DefaultsToAscending()
    {
        GameEntry imageSmall = new("Small OS", @"C:\OS\small.iso", 500_000_000L, null, new DateTime(2023, 1, 1), false, LibraryCategory.OsImage);
        GameEntry imageLarge = new("Large OS", @"C:\OS\large.iso", 10_000_000_000L, null, new DateTime(2023, 1, 1), false, LibraryCategory.OsImage);
        GameEntry imageMedium = new("Medium OS", @"C:\OS\medium.iso", 4_000_000_000L, null, new DateTime(2023, 1, 1), false, LibraryCategory.OsImage);

        MainViewModel viewModel = CreateViewModelWithOsImages([imageSmall, imageLarge, imageMedium]);

        // Selecting Size defaults IsOsImageSortAscending to true (Smallest First)
        viewModel.SelectedOsImageSortOption = OsImageSortOption.Size;
        Assert.True(viewModel.IsOsImageSortAscending);
        Assert.Equal(["Small OS", "Medium OS", "Large OS"], viewModel.OsImages.Select(i => i.Name));

        // Toggle direction to Descending (Largest First)
        viewModel.ToggleOsImageSortDirectionCommand.Execute(null);
        Assert.False(viewModel.IsOsImageSortAscending);
        Assert.Equal(["Large OS", "Medium OS", "Small OS"], viewModel.OsImages.Select(i => i.Name));
    }

    private static MainViewModel CreateViewModelWithOsImages(List<GameEntry> osImages)
    {
        Mock<ISettingsService> settingsServiceMock = new();
        settingsServiceMock.Setup(s => s.LoadSettingsAsync())
            .ReturnsAsync(new AppSettings { OsImageSourceFolders = [@"C:\OS"] });

        Mock<ILibraryCacheService> cacheServiceMock = new();
        cacheServiceMock.Setup(c => c.LoadCacheAsync())
            .ReturnsAsync(new LibraryCacheSnapshot(
                LibraryCacheSnapshot.CurrentSchemaVersion,
                [], [], [], osImages,
                [], [], [], [@"C:\OS"],
                DateTime.Now,
                []));

        SmartAdderViewModel smartAdderViewModel = new(
            Mock.Of<IWindowService>(),
            Mock.Of<ISmartAdderHistoryService>(),
            Mock.Of<ILogger<SmartAdderViewModel>>());

        MainViewModel viewModel = new(
            Mock.Of<ILogger<MainViewModel>>(),
            Mock.Of<IUpdateService>(),
            settingsServiceMock.Object,
            cacheServiceMock.Object,
            Mock.Of<ILibraryScannerService>(),
            Mock.Of<IDriveDiscoveryService>(),
            Mock.Of<IDriveValidationService>(),
            Mock.Of<IFileTransferService>(),
            Mock.Of<ITransferQueueService>(),
            Mock.Of<IWindowService>(),
            Mock.Of<IProcessService>(),
            Mock.Of<IDispatcherService>(),
            Mock.Of<ISourceLibraryService>(),
            Mock.Of<IDialogService>(),
            smartAdderViewModel,
            () => Mock.Of<GameDetailsViewModel>());

        viewModel.InitializeAsync().GetAwaiter().GetResult();
        return viewModel;
    }
}
