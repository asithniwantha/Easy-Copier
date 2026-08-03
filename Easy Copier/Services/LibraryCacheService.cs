using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface ILibraryCacheService
    {
        Task<LibraryCacheSnapshot?> LoadCacheAsync();
        Task SaveCacheAsync(LibraryCacheSnapshot snapshot);
        Task InvalidateCacheAsync();
        Task<CacheValidationOutcome> ValidateCacheAsync(
            LibraryCacheSnapshot cache,
            AppSettings currentSettings,
            CancellationToken cancellationToken = default);
    }

    public class LibraryCacheService : ILibraryCacheService
    {
        private readonly ILogger<LibraryCacheService> _logger;
        private const string CacheFileName = "library_cache.json";

        public LibraryCacheService(ILogger<LibraryCacheService> logger)
        {
            _logger = logger;
        }

        private string GetCacheFilePath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataFolder, "EasyCopier");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, CacheFileName);
        }

        public async Task<LibraryCacheSnapshot?> LoadCacheAsync()
        {
            var cachePath = GetCacheFilePath();

            try
            {
                if (!File.Exists(cachePath))
                {
                    _logger.LogInformation("Cache file not found at {Path}", cachePath);
                    return null;
                }

                var json = await File.ReadAllTextAsync(cachePath);
                var cache = JsonSerializer.Deserialize<LibraryCacheSnapshot>(json);

                if (cache == null)
                {
                    _logger.LogWarning("Failed to deserialize cache from {Path}", cachePath);
                    return null;
                }

                if (cache.SchemaVersion != LibraryCacheSnapshot.CurrentSchemaVersion)
                {
                    _logger.LogWarning(
                        "Cache schema version {CacheVersion} does not match current version {CurrentVersion}",
                        cache.SchemaVersion,
                        LibraryCacheSnapshot.CurrentSchemaVersion);
                    return null;
                }

                _logger.LogInformation(
                    "Cache loaded successfully: {GameCount} games, {AppCount} apps from {CachedAt}",
                    cache.Games.Count,
                    cache.Apps.Count,
                    cache.CachedAt);

                return cache;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Cache file is corrupt or malformed at {Path}", cachePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cache from {Path}", cachePath);
                return null;
            }
        }

        public async Task SaveCacheAsync(LibraryCacheSnapshot snapshot)
        {
            var cachePath = GetCacheFilePath();
            var tempPath = cachePath + ".tmp";

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(snapshot, options);
                await File.WriteAllTextAsync(tempPath, json);

                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }

                File.Move(tempPath, cachePath);

                _logger.LogInformation(
                    "Cache saved successfully: {GameCount} games, {AppCount} apps, {FingerprintCount} fingerprints",
                    snapshot.Games.Count,
                    snapshot.Apps.Count,
                    snapshot.ItemFingerprints.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving cache to {Path}", cachePath);

                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }

                throw;
            }
        }

        public async Task InvalidateCacheAsync()
        {
            var cachePath = GetCacheFilePath();

            try
            {
                if (File.Exists(cachePath))
                {
                    await Task.Run(() => File.Delete(cachePath));
                    _logger.LogInformation("Cache invalidated at {Path}", cachePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache at {Path}", cachePath);
            }
        }

        public async Task<CacheValidationOutcome> ValidateCacheAsync(
            LibraryCacheSnapshot cache,
            AppSettings currentSettings,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedCacheGameFolders = cache.GameSourceFolders
                    .Select(NormalizePath)
                    .OrderBy(p => p)
                    .ToList();

                var normalizedCacheAppFolders = cache.AppSourceFolders
                    .Select(NormalizePath)
                    .OrderBy(p => p)
                    .ToList();

                var normalizedCurrentGameFolders = currentSettings.GameSourceFolders
                    .Select(NormalizePath)
                    .OrderBy(p => p)
                    .ToList();

                var normalizedCurrentAppFolders = currentSettings.AppSourceFolders
                    .Select(NormalizePath)
                    .OrderBy(p => p)
                    .ToList();

                if (!normalizedCacheGameFolders.SequenceEqual(normalizedCurrentGameFolders) ||
                    !normalizedCacheAppFolders.SequenceEqual(normalizedCurrentAppFolders))
                {
                    _logger.LogInformation("Cache configuration mismatch: source folders have changed");
                    return new CacheValidationOutcome(CacheValidationResult.ConfigurationMismatch, new List<string>());
                }

                var allSourceFolders = normalizedCurrentGameFolders.Concat(normalizedCurrentAppFolders).Distinct();
                foreach (var sourceFolder in allSourceFolders)
                {
                    if (!Directory.Exists(sourceFolder))
                    {
                        _logger.LogInformation("Cache invalid: source folder no longer exists: {Path}", sourceFolder);
                        return new CacheValidationOutcome(CacheValidationResult.SourcesUnavailable, new List<string>());
                    }
                }

                var changedItems = new List<string>();

                var allCachedItems = cache.Games.Concat(cache.Apps).ToList();

                foreach (var item in allCachedItems)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var normalizedPath = NormalizePath(item.FolderPath);

                    if (!cache.ItemFingerprints.TryGetValue(normalizedPath, out var cachedFingerprint))
                    {
                        _logger.LogDebug("Item missing fingerprint: {Path}", item.FolderPath);
                        changedItems.Add(normalizedPath);
                        continue;
                    }

                    if (!Directory.Exists(item.FolderPath))
                    {
                        _logger.LogDebug("Item removed: {Path}", item.FolderPath);
                        changedItems.Add(normalizedPath);
                        continue;
                    }

                    try
                    {
                        var currentFingerprint = await ComputeItemFingerprintAsync(item.FolderPath, cancellationToken);

                        if (!FingerprintsMatch(cachedFingerprint, currentFingerprint))
                        {
                            _logger.LogDebug("Item changed: {Path}", item.FolderPath);
                            changedItems.Add(normalizedPath);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _logger.LogDebug("Item inaccessible (treating as changed): {Path}", item.FolderPath);
                        changedItems.Add(normalizedPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error validating item (treating as changed): {Path}", item.FolderPath);
                        changedItems.Add(normalizedPath);
                    }
                }

                var currentTopLevelItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var sourceFolder in allSourceFolders)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var subdirectories = await Task.Run(
                            () => Directory.GetDirectories(sourceFolder, "*", SearchOption.TopDirectoryOnly),
                            cancellationToken);

                        foreach (var subdir in subdirectories)
                        {
                            currentTopLevelItems.Add(NormalizePath(subdir));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error enumerating source folder (invalidating cache): {Path}", sourceFolder);
                        return new CacheValidationOutcome(CacheValidationResult.ItemsChanged, new List<string>());
                    }
                }

                var cachedTopLevelItems = allCachedItems
                    .Select(item => NormalizePath(item.FolderPath))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var newItems = currentTopLevelItems.Except(cachedTopLevelItems).ToList();
                var removedItems = cachedTopLevelItems.Except(currentTopLevelItems).ToList();

                if (newItems.Any())
                {
                    _logger.LogInformation("New items detected: {Count}", newItems.Count);
                    changedItems.AddRange(newItems);
                }

                if (removedItems.Any())
                {
                    _logger.LogInformation("Removed items detected: {Count}", removedItems.Count);
                    changedItems.AddRange(removedItems);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cache validation cancelled");
                    return new CacheValidationOutcome(CacheValidationResult.Valid, new List<string>());
                }

                if (changedItems.Any())
                {
                    _logger.LogInformation("Cache validation: {Count} items changed", changedItems.Count);
                    return new CacheValidationOutcome(CacheValidationResult.ItemsChanged, changedItems);
                }

                _logger.LogInformation("Cache validation successful: no changes detected");
                return new CacheValidationOutcome(CacheValidationResult.Valid, new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache validation");
                return new CacheValidationOutcome(CacheValidationResult.CorruptOrInvalid, new List<string>());
            }
        }

        private async Task<ItemFingerprint> ComputeItemFingerprintAsync(
            string folderPath,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var dirInfo = new DirectoryInfo(folderPath);
                var files = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories);

                long totalBytes = 0;
                var latestWriteTime = DateTime.MinValue;

                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    totalBytes += file.Length;

                    if (file.LastWriteTimeUtc > latestWriteTime)
                    {
                        latestWriteTime = file.LastWriteTimeUtc;
                    }
                }

                return new ItemFingerprint(
                    NormalizePath(folderPath),
                    totalBytes,
                    latestWriteTime);
            }, cancellationToken);
        }

        private bool FingerprintsMatch(ItemFingerprint cached, ItemFingerprint current)
        {
            return cached.TotalBytes == current.TotalBytes &&
                   cached.LastWriteTimeUtc == current.LastWriteTimeUtc;
        }

        private string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
