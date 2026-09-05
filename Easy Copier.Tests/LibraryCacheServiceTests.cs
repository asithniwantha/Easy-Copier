using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Easy_Copier.Tests;

public class LibraryCacheServiceTests : IDisposable
{
    private readonly string _testCacheDirectory;
    private readonly string _testCachePath;
    private readonly LibraryCacheService _cacheService;
    private readonly Mock<ILogger<LibraryCacheService>> _mockLogger;

    public LibraryCacheServiceTests()
    {
        _testCacheDirectory = Path.Combine(Path.GetTempPath(), $"EasyCopierTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testCacheDirectory);

        _testCachePath = Path.Combine(_testCacheDirectory, "library_cache.json");

        _mockLogger = new Mock<ILogger<LibraryCacheService>>();
        _cacheService = new LibraryCacheService(_mockLogger.Object);

        Environment.SetEnvironmentVariable("LOCALAPPDATA", _testCacheDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testCacheDirectory))
        {
            Directory.Delete(_testCacheDirectory, true);
        }
    }

    [Fact]
    public async Task LoadCacheAsync_WhenCacheDoesNotExist_ReturnsNull()
    {
        var result = await _cacheService.LoadCacheAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndLoadCache_RoundTrip_Success()
    {
        var snapshot = CreateTestSnapshot();

        await _cacheService.SaveCacheAsync(snapshot);
        var loaded = await _cacheService.LoadCacheAsync();

        Assert.NotNull(loaded);
        Assert.Equal(snapshot.SchemaVersion, loaded.SchemaVersion);
        Assert.Equal(snapshot.Games.Count, loaded.Games.Count);
        Assert.Equal(snapshot.Apps.Count, loaded.Apps.Count);
        Assert.Equal(snapshot.Games[0].Name, loaded.Games[0].Name);
        Assert.Equal(snapshot.ItemFingerprints.Count, loaded.ItemFingerprints.Count);
    }

    [Fact]
    public async Task LoadCacheAsync_WithCorruptJson_ReturnsNull()
    {
        var cachePath = _cacheService.GetType()
            .GetMethod("GetCacheFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_cacheService, null) as string;

        if (cachePath != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            await File.WriteAllTextAsync(cachePath, "{ corrupt json }}");

            var result = await _cacheService.LoadCacheAsync();

            Assert.Null(result);
        }
    }

    [Fact]
    public async Task LoadCacheAsync_WithUnsupportedSchemaVersion_ReturnsNull()
    {
        var snapshot = new LibraryCacheSnapshot(
            0,
            new List<GameEntry>(),
            new List<GameEntry>(),
            new List<string>(),
            new List<string>(),
            DateTime.Now,
            new Dictionary<string, ItemFingerprint>());

        await _cacheService.SaveCacheAsync(snapshot);
        var loaded = await _cacheService.LoadCacheAsync();

        Assert.Null(loaded);
    }

    [Fact]
    public async Task InvalidateCacheAsync_RemovesCacheFile()
    {
        var snapshot = CreateTestSnapshot();
        await _cacheService.SaveCacheAsync(snapshot);

        await _cacheService.InvalidateCacheAsync();

        var loaded = await _cacheService.LoadCacheAsync();
        Assert.Null(loaded);
    }

    [Fact]
    public async Task ValidateCacheAsync_WithConfigurationMismatch_ReturnsConfigurationMismatch()
    {
        var cache = CreateTestSnapshot();
        var settings = new AppSettings
        {
            GameSourceFolders = new List<string> { @"C:\DifferentGames" },
            AppSourceFolders = new List<string>()
        };

        var result = await _cacheService.ValidateCacheAsync(cache, settings);

        Assert.Equal(CacheValidationResult.ConfigurationMismatch, result.Result);
    }

    [Fact]
    public async Task ValidateCacheAsync_WithUnavailableSource_ReturnsSourcesUnavailable()
    {
        var cache = CreateTestSnapshot();
        var settings = new AppSettings
        {
            GameSourceFolders = cache.GameSourceFolders,
            AppSourceFolders = cache.AppSourceFolders
        };

        var result = await _cacheService.ValidateCacheAsync(cache, settings);

        Assert.Equal(CacheValidationResult.SourcesUnavailable, result.Result);
    }

    private LibraryCacheSnapshot CreateTestSnapshot()
    {
        var games = new List<GameEntry>
        {
            new GameEntry(
                "TestGame1",
                @"C:\Games\TestGame1",
                1024000,
                null,
                DateTime.Now,
                false,
                LibraryCategory.Game)
        };

        var apps = new List<GameEntry>
        {
            new GameEntry(
                "TestApp1",
                @"C:\Apps\TestApp1",
                512000,
                null,
                DateTime.Now,
                false,
                LibraryCategory.App)
        };

        var fingerprints = new Dictionary<string, ItemFingerprint>
        {
            [@"C:\Games\TestGame1"] = new ItemFingerprint(@"C:\Games\TestGame1", 1024000, DateTime.UtcNow),
            [@"C:\Apps\TestApp1"] = new ItemFingerprint(@"C:\Apps\TestApp1", 512000, DateTime.UtcNow)
        };

        return new LibraryCacheSnapshot(
            LibraryCacheSnapshot.CurrentSchemaVersion,
            games,
            apps,
            new List<string> { @"C:\Games" },
            new List<string> { @"C:\Apps" },
            DateTime.Now,
            fingerprints);
    }
}

public class CacheValidationIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LibraryCacheService _cacheService;

    public CacheValidationIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CacheValidationTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        var mockLogger = new Mock<ILogger<LibraryCacheService>>();
        _cacheService = new LibraryCacheService(mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ValidateCacheAsync_WithUnchangedFiles_ReturnsValid()
    {
        var gameFolder = Path.Combine(_testDirectory, "Games");
        var game1Folder = Path.Combine(gameFolder, "Game1");
        Directory.CreateDirectory(game1Folder);
        File.WriteAllText(Path.Combine(game1Folder, "test.txt"), "content");

        var fingerprint = await ComputeFingerprintAsync(game1Folder);

        var cache = new LibraryCacheSnapshot(
            LibraryCacheSnapshot.CurrentSchemaVersion,
            new List<GameEntry>
            {
                new GameEntry("Game1", game1Folder, 100, null, DateTime.Now, false, LibraryCategory.Game)
            },
            new List<GameEntry>(),
            new List<string> { gameFolder },
            new List<string>(),
            DateTime.Now,
            new Dictionary<string, ItemFingerprint>
            {
                [Path.GetFullPath(game1Folder).TrimEnd(Path.DirectorySeparatorChar)] = fingerprint
            });

        var settings = new AppSettings
        {
            GameSourceFolders = new List<string> { gameFolder },
            AppSourceFolders = new List<string>()
        };

        var result = await _cacheService.ValidateCacheAsync(cache, settings);

        Assert.Equal(CacheValidationResult.Valid, result.Result);
        Assert.Empty(result.ChangedItems);
    }

    [Fact]
    public async Task ValidateCacheAsync_WithChangedFiles_ReturnsItemsChanged()
    {
        var gameFolder = Path.Combine(_testDirectory, "Games");
        var game1Folder = Path.Combine(gameFolder, "Game1");
        Directory.CreateDirectory(game1Folder);
        File.WriteAllText(Path.Combine(game1Folder, "test.txt"), "original");

        var originalFingerprint = await ComputeFingerprintAsync(game1Folder);

        var cache = new LibraryCacheSnapshot(
            LibraryCacheSnapshot.CurrentSchemaVersion,
            new List<GameEntry>
            {
                new GameEntry("Game1", game1Folder, 100, null, DateTime.Now, false, LibraryCategory.Game)
            },
            new List<GameEntry>(),
            new List<string> { gameFolder },
            new List<string>(),
            DateTime.Now,
            new Dictionary<string, ItemFingerprint>
            {
                [Path.GetFullPath(game1Folder).TrimEnd(Path.DirectorySeparatorChar)] = originalFingerprint
            });

        await Task.Delay(100);
        File.WriteAllText(Path.Combine(game1Folder, "test.txt"), "modified content");

        var settings = new AppSettings
        {
            GameSourceFolders = new List<string> { gameFolder },
            AppSourceFolders = new List<string>()
        };

        var result = await _cacheService.ValidateCacheAsync(cache, settings);

        Assert.Equal(CacheValidationResult.ItemsChanged, result.Result);
        Assert.NotEmpty(result.ChangedItems);
    }

    [Fact]
    public async Task ValidateCacheAsync_WithNewItem_ReturnsItemsChanged()
    {
        var gameFolder = Path.Combine(_testDirectory, "Games");
        var game1Folder = Path.Combine(gameFolder, "Game1");
        var game2Folder = Path.Combine(gameFolder, "Game2");

        Directory.CreateDirectory(game1Folder);
        File.WriteAllText(Path.Combine(game1Folder, "test.txt"), "content");

        var fingerprint = await ComputeFingerprintAsync(game1Folder);

        var cache = new LibraryCacheSnapshot(
            LibraryCacheSnapshot.CurrentSchemaVersion,
            new List<GameEntry>
            {
                new GameEntry("Game1", game1Folder, 100, null, DateTime.Now, false, LibraryCategory.Game)
            },
            new List<GameEntry>(),
            new List<string> { gameFolder },
            new List<string>(),
            DateTime.Now,
            new Dictionary<string, ItemFingerprint>
            {
                [Path.GetFullPath(game1Folder).TrimEnd(Path.DirectorySeparatorChar)] = fingerprint
            });

        Directory.CreateDirectory(game2Folder);
        File.WriteAllText(Path.Combine(game2Folder, "test.txt"), "new game");

        var settings = new AppSettings
        {
            GameSourceFolders = new List<string> { gameFolder },
            AppSourceFolders = new List<string>()
        };

        var result = await _cacheService.ValidateCacheAsync(cache, settings);

        Assert.Equal(CacheValidationResult.ItemsChanged, result.Result);
        Assert.Contains(result.ChangedItems, item => item.Contains("Game2", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateCacheAsync_WithRemovedItem_ReturnsItemsChanged()
    {
        var gameFolder = Path.Combine(_testDirectory, "Games");
        var game1Folder = Path.Combine(gameFolder, "Game1");
        var game2Folder = Path.Combine(gameFolder, "Game2");

        Directory.CreateDirectory(game1Folder);
        File.WriteAllText(Path.Combine(game1Folder, "test.txt"), "content");

        var fingerprint = await ComputeFingerprintAsync(game1Folder);

        var cache = new LibraryCacheSnapshot(
            LibraryCacheSnapshot.CurrentSchemaVersion,
            new List<GameEntry>
            {
                new GameEntry("Game1", game1Folder, 100, null, DateTime.Now, false, LibraryCategory.Game),
                new GameEntry("Game2", game2Folder, 100, null, DateTime.Now, false, LibraryCategory.Game)
            },
            new List<GameEntry>(),
            new List<string> { gameFolder },
            new List<string>(),
            DateTime.Now,
            new Dictionary<string, ItemFingerprint>
            {
                [Path.GetFullPath(game1Folder).TrimEnd(Path.DirectorySeparatorChar)] = fingerprint,
                [Path.GetFullPath(game2Folder).TrimEnd(Path.DirectorySeparatorChar)] = new ItemFingerprint(game2Folder, 100, DateTime.UtcNow)
            });

        var settings = new AppSettings
        {
            GameSourceFolders = new List<string> { gameFolder },
            AppSourceFolders = new List<string>()
        };

        var result = await _cacheService.ValidateCacheAsync(cache, settings);

        Assert.Equal(CacheValidationResult.ItemsChanged, result.Result);
        Assert.Contains(result.ChangedItems, item => item.Contains("Game2", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ItemFingerprint> ComputeFingerprintAsync(string folderPath)
    {
        return await Task.Run(() =>
        {
            var dirInfo = new DirectoryInfo(folderPath);
            var files = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories);

            long totalBytes = 0;
            var latestWriteTime = DateTime.MinValue;

            foreach (var file in files)
            {
                totalBytes += file.Length;
                if (file.LastWriteTimeUtc > latestWriteTime)
                {
                    latestWriteTime = file.LastWriteTimeUtc;
                }
            }

            var normalizedPath = Path.GetFullPath(folderPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return new ItemFingerprint(normalizedPath, totalBytes, latestWriteTime);
        });
    }
}
