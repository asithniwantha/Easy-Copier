using Easy_Copier.Models;
using Easy_Copier.Services;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Easy_Copier.Tests;

public class LibraryScannerServiceTests
{
    private readonly Mock<IGameScannerService> _mockGameScannerService;
    private readonly LibraryScannerService _libraryScannerService;

    public LibraryScannerServiceTests()
    {
        _mockGameScannerService = new Mock<IGameScannerService>();
        _libraryScannerService = new LibraryScannerService(_mockGameScannerService.Object);
    }

    [Fact]
    public async Task FindDuplicatesReportAsync_WhenNoDuplicates_ReturnsNoDuplicatesMessage()
    {
        // Arrange
        var settings = new AppSettings
        {
            GameSourceFolders = new List<string> { @"C:\Games" },
            AppSourceFolders = new List<string> { @"C:\Apps" }
        };

        var games = new List<GameEntry>
        {
            new GameEntry("Game1", @"C:\Games\Game1", 100, null, System.DateTime.Now, false, LibraryCategory.Game)
        };
        var apps = new List<GameEntry>
        {
            new GameEntry("App1", @"C:\Apps\App1", 200, null, System.DateTime.Now, false, LibraryCategory.App)
        };

        _mockGameScannerService
            .Setup(s => s.ScanLibraryAsync(settings.GameSourceFolders, LibraryCategory.Game, null, null, CancellationToken.None))
            .ReturnsAsync(games);

        _mockGameScannerService
            .Setup(s => s.ScanLibraryAsync(settings.AppSourceFolders, LibraryCategory.App, null, null, CancellationToken.None))
            .ReturnsAsync(apps);

        // Act
        string report = await _libraryScannerService.FindDuplicatesReportAsync(settings);

        // Assert
        Assert.Equal("No duplicates found across source libraries.", report);
    }

    [Fact]
    public async Task FindDuplicatesReportAsync_WhenDuplicatesExist_ReturnsFormattedReport()
    {
        // Arrange
        var settings = new AppSettings
        {
            GameSourceFolders = new List<string> { @"C:\Games" },
            AppSourceFolders = new List<string> { @"D:\BackupGames" }
        };

        var games = new List<GameEntry>
        {
            new GameEntry("Game1", @"C:\Games\Game1", 100, null, System.DateTime.Now, false, LibraryCategory.Game)
        };
        var backupGames = new List<GameEntry>
        {
            new GameEntry("Game1", @"D:\BackupGames\Game1", 100, null, System.DateTime.Now, false, LibraryCategory.App)
        };

        _mockGameScannerService
            .Setup(s => s.ScanLibraryAsync(settings.GameSourceFolders, LibraryCategory.Game, null, null, CancellationToken.None))
            .ReturnsAsync(games);

        _mockGameScannerService
            .Setup(s => s.ScanLibraryAsync(settings.AppSourceFolders, LibraryCategory.App, null, null, CancellationToken.None))
            .ReturnsAsync(backupGames);

        // Act
        string report = await _libraryScannerService.FindDuplicatesReportAsync(settings);

        // Assert
        Assert.Contains("Found 1 duplicated items:", report, System.StringComparison.Ordinal);
        Assert.Contains("- Game1 (2 copies):", report, System.StringComparison.Ordinal);
        Assert.Contains("  • [Game] C:\\Games\\Game1", report, System.StringComparison.Ordinal);
        Assert.Contains("  • [App] D:\\BackupGames\\Game1", report, System.StringComparison.Ordinal);
        Assert.Contains("Cleanup Recommendation:", report, System.StringComparison.Ordinal);
    }
}
