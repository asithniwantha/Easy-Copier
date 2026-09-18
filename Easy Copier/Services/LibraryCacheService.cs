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
    /// <summary>
    /// Defines operations for managing persistent JSON library caching and checking cache validity against directory fingerprints.
    /// </summary>
    public interface ILibraryCacheService
    {
        /// <summary>
        /// Asynchronously loads the cached library snapshot from local disk storage if available and schema compatible.
        /// </summary>
        /// <returns>A task returning the loaded <see cref="LibraryCacheSnapshot"/>, or <c>null</c> if not found or invalid.</returns>
        Task<LibraryCacheSnapshot?> LoadCacheAsync();

        /// <summary>
        /// Asynchronously saves the library cache snapshot to local disk storage using atomic write patterns.
        /// </summary>
        /// <param name="snapshot">The snapshot object containing scanned library state and fingerprints.</param>
        /// <returns>A task representing the save operation.</returns>
        Task SaveCacheAsync(LibraryCacheSnapshot snapshot);

        /// <summary>
        /// Asynchronously deletes the local library cache file from disk.
        /// </summary>
        /// <returns>A task representing the deletion operation.</returns>
        Task InvalidateCacheAsync();

        /// <summary>
        /// Validates whether the cached snapshot matches current application settings source folders and directory fingerprints.
        /// </summary>
        /// <param name="cache">The cached snapshot to validate.</param>
        /// <param name="currentSettings">Current application settings containing source folder lists.</param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>A task returning a <see cref="CacheValidationOutcome"/> indicating validation results and changed items.</returns>
        Task<CacheValidationOutcome> ValidateCacheAsync(
            LibraryCacheSnapshot cache,
            AppSettings currentSettings,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Computes a lightweight size and modification fingerprint for a given file or folder path.
        /// </summary>
        /// <param name="folderPath">The folder path to inspect.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning the computed <see cref="ItemFingerprint"/>.</returns>
        Task<ItemFingerprint> ComputeItemFingerprintAsync(
            string folderPath,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service for serializing, deserializing, and validating library scan results stored in <c>library_cache.json</c>.
    /// </summary>
    public class LibraryCacheService : ILibraryCacheService
    {
        /// <summary>
        /// Indented JSON serializer options for formatting cached files.
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Logger instance used for diagnostic logging.
        /// </summary>
        private readonly ILogger<LibraryCacheService> _logger;

        /// <summary>
        /// Name of the cache file stored in AppData.
        /// </summary>
        private const string CacheFileName = "library_cache.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryCacheService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public LibraryCacheService(ILogger<LibraryCacheService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Resolves the absolute path to the local library cache file in AppData.
        /// </summary>
        /// <returns>The full string path to the cache file.</returns>
        private static string GetCacheFilePath()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolder, "EasyCopier");
            _ = Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, CacheFileName);
        }

        /// <summary>
        /// Asynchronously deserializes and verifies the cached library snapshot file.
        /// </summary>
        /// <returns>The cached snapshot if valid and matching schema version; otherwise, <c>null</c>.</returns>
        public async Task<LibraryCacheSnapshot?> LoadCacheAsync()
        {
            string cachePath = GetCacheFilePath();

            try
            {
                if (!File.Exists(cachePath))
                {
                    _logger.LogInformation("Cache file not found at {Path}", cachePath);
                    return null;
                }

                string json = await File.ReadAllTextAsync(cachePath);
                LibraryCacheSnapshot? cache = JsonSerializer.Deserialize<LibraryCacheSnapshot>(json);

                if (cache != null && cache.SchemaVersion < LibraryCacheSnapshot.CurrentSchemaVersion)
                {
                    _logger.LogInformation("Cache schema version {CacheVersion} is older than current {CurrentVersion}. Invalidating cache.", cache.SchemaVersion, LibraryCacheSnapshot.CurrentSchemaVersion);
                    return null;
                }

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

        /// <summary>
        /// Asynchronously writes the library cache snapshot to disk using a temporary file and atomic swap.
        /// </summary>
        /// <param name="snapshot">The snapshot instance to write.</param>
        /// <returns>A task representing the save operation.</returns>
        public async Task SaveCacheAsync(LibraryCacheSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            string cachePath = GetCacheFilePath();
            string tempPath = cachePath + ".tmp";

            try
            {
                string json = JsonSerializer.Serialize(snapshot, _jsonOptions);
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

        /// <summary>
        /// Deletes the cached snapshot file if present on disk.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvalidateCacheAsync()
        {
            string cachePath = GetCacheFilePath();

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

        /// <summary>
        /// Compares the cached snapshot against active source folder settings and inspects modified files to evaluate cache validity.
        /// </summary>
        /// <param name="cache">The cached snapshot to evaluate.</param>
        /// <param name="currentSettings">The active application settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning a <see cref="CacheValidationOutcome"/>.</returns>
        public async Task<CacheValidationOutcome> ValidateCacheAsync(
            LibraryCacheSnapshot cache,
            AppSettings currentSettings,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(currentSettings);

            try
            {
                List<string> normalizedCacheGameFolders = [.. cache.GameSourceFolders
                    .Select(NormalizePath)
                    .OrderBy(p => p)];

                List<string> normalizedCacheAppFolders = [.. cache.AppSourceFolders
                    .Select(NormalizePath)
                    .OrderBy(p => p)];

                List<string> normalizedCacheTvAndFilmFolders = [.. (cache.TvAndFilmSourceFolders ?? [])
                    .Select(NormalizePath)
                    .OrderBy(p => p)];

                List<string> normalizedCacheOsImageFolders = [.. (cache.OsImageSourceFolders ?? [])
                    .Select(NormalizePath)
                    .OrderBy(p => p)];

                List<string> normalizedCurrentGameFolders = [.. currentSettings.GameSourceFolders
                    .Select(NormalizePath)
                    .OrderBy(p => p)];

                List<string> normalizedCurrentAppFolders = [.. currentSettings.AppSourceFolders
                    .Select(NormalizePath)
                    .OrderBy(p => p)];

                List<string> normalizedCurrentTvAndFilmFolders = [.. (currentSettings.TvAndFilmSourceFolders ?? [])
                    .Select(NormalizePath)
                    .OrderBy(p => p)];

                List<string> normalizedCurrentOsImageFolders = [.. (currentSettings.OsImageSourceFolders ?? [])
                    .Select(NormalizePath)
                    .OrderBy(p => p)];

                if (!normalizedCacheGameFolders.SequenceEqual(normalizedCurrentGameFolders) ||
                    !normalizedCacheAppFolders.SequenceEqual(normalizedCurrentAppFolders) ||
                    !normalizedCacheTvAndFilmFolders.SequenceEqual(normalizedCurrentTvAndFilmFolders) ||
                    !normalizedCacheOsImageFolders.SequenceEqual(normalizedCurrentOsImageFolders))
                {
                    _logger.LogInformation("Cache configuration mismatch: source folders have changed");
                    return new CacheValidationOutcome(CacheValidationResult.ConfigurationMismatch, []);
                }

                List<string> allSourceFolders = normalizedCurrentGameFolders.Concat(normalizedCurrentAppFolders).Concat(normalizedCurrentTvAndFilmFolders).Concat(normalizedCurrentOsImageFolders).Distinct().ToList();
                foreach (string? sourceFolder in allSourceFolders)
                {
                    if (!Directory.Exists(sourceFolder))
                    {
                        _logger.LogInformation("Cache invalid: source folder no longer exists: {Path}", sourceFolder);
                        return new CacheValidationOutcome(CacheValidationResult.SourcesUnavailable, []);
                    }
                }

                List<string> changedItems = [];

                List<GameEntry> allCachedItems = cache.Games.Concat(cache.Apps).Concat(cache.TvAndFilms ?? []).Concat(cache.OsImages ?? []).ToList();

                foreach (GameEntry? item in allCachedItems)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    string normalizedPath = NormalizePath(item.FolderPath);

                    if (!cache.ItemFingerprints.TryGetValue(normalizedPath, out ItemFingerprint? cachedFingerprint))
                    {
                        _logger.LogDebug("Item missing fingerprint: {Path}", item.FolderPath);
                        changedItems.Add(normalizedPath);
                        continue;
                    }

                    if (!Directory.Exists(item.FolderPath) && !File.Exists(item.FolderPath))
                    {
                        _logger.LogDebug("Item removed: {Path}", item.FolderPath);
                        changedItems.Add(normalizedPath);
                        continue;
                    }

                    try
                    {
                        ItemFingerprint currentFingerprint = await ComputeItemFingerprintAsync(item.FolderPath, cancellationToken);

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

                HashSet<string> currentTopLevelItems = [with(StringComparer.OrdinalIgnoreCase)];

                foreach (string? sourceFolder in allSourceFolders)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        string[] subdirectories = await Task.Run(
                            () => Directory.GetDirectories(sourceFolder, "*", SearchOption.TopDirectoryOnly),
                            cancellationToken);

                        foreach (string? subdir in subdirectories)
                        {
                            _ = currentTopLevelItems.Add(NormalizePath(subdir));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error enumerating source folder (invalidating cache): {Path}", sourceFolder);
                        return new CacheValidationOutcome(CacheValidationResult.ItemsChanged, []);
                    }
                }

                HashSet<string> cachedTopLevelItems = allCachedItems
                    .Select(item => NormalizePath(item.FolderPath))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                List<string> newItems = currentTopLevelItems.Except(cachedTopLevelItems).ToList();
                List<string> removedItems = cachedTopLevelItems.Except(currentTopLevelItems).ToList();

                if (newItems.Count > 0)
                {
                    _logger.LogInformation("New items detected: {Count}", newItems.Count);
                    changedItems.AddRange(newItems);
                }

                if (removedItems.Count > 0)
                {
                    _logger.LogInformation("Removed items detected: {Count}", removedItems.Count);
                    changedItems.AddRange(removedItems);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cache validation cancelled");
                    return new CacheValidationOutcome(CacheValidationResult.Valid, []);
                }

                if (changedItems.Count > 0)
                {
                    _logger.LogInformation("Cache validation: {Count} items changed", changedItems.Count);
                    return new CacheValidationOutcome(CacheValidationResult.ItemsChanged, changedItems);
                }

                _logger.LogInformation("Cache validation successful: no changes detected");
                return new CacheValidationOutcome(CacheValidationResult.Valid, []);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache validation");
                return new CacheValidationOutcome(CacheValidationResult.CorruptOrInvalid, []);
            }
        }

        /// <summary>
        /// Calculates total size and latest modified timestamp for a directory or file.
        /// </summary>
        /// <param name="folderPath">The path to inspect.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning the calculated <see cref="ItemFingerprint"/>.</returns>
        public async Task<ItemFingerprint> ComputeItemFingerprintAsync(
            string folderPath,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                long totalBytes = 0;
                DateTime latestWriteTime = DateTime.MinValue;

                if (File.Exists(folderPath))
                {
                    FileInfo fileInfo = new(folderPath);
                    totalBytes = fileInfo.Length;
                    latestWriteTime = fileInfo.LastWriteTimeUtc;
                }
                else
                {
                    DirectoryInfo dirInfo = new(folderPath);
                    IEnumerable<FileInfo> files = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories);

                    foreach (FileInfo file in files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        totalBytes += file.Length;

                        if (file.LastWriteTimeUtc > latestWriteTime)
                        {
                            latestWriteTime = file.LastWriteTimeUtc;
                        }
                    }
                }

                return new ItemFingerprint(
                    NormalizePath(folderPath),
                    totalBytes,
                    latestWriteTime);
            }, cancellationToken);
        }

        /// <summary>
        /// Compares two fingerprints to check if total byte size and write timestamp match.
        /// </summary>
        /// <param name="cached">The original cached fingerprint.</param>
        /// <param name="current">The newly computed fingerprint.</param>
        /// <returns><c>true</c> if both fingerprints match; otherwise, <c>false</c>.</returns>
        private static bool FingerprintsMatch(ItemFingerprint cached, ItemFingerprint current)
        {
            return cached.TotalBytes == current.TotalBytes &&
                   cached.LastWriteTimeUtc == current.LastWriteTimeUtc;
        }

        /// <summary>
        /// Normalizes file and directory paths by resolving full paths and trimming trailing directory separators.
        /// </summary>
        /// <param name="path">The input path string.</param>
        /// <returns>The normalized path string.</returns>
        private string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
