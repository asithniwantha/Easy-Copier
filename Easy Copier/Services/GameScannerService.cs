using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for scanning game and content directories, computing folder sizes, and locating metadata assets.
    /// </summary>
    public interface IGameScannerService
    {
        /// <summary>
        /// Asynchronously scans specified source folders for items belonging to the given library category.
        /// </summary>
        /// <param name="sourceFolders">Collection of source folder directory paths.</param>
        /// <param name="category">The content library category being scanned.</param>
        /// <param name="progress">Optional progress reporter for status updates.</param>
        /// <param name="videoExtensions">Optional video file extension filter string (e.g., ".mp4, .mkv") for video libraries.</param>
        /// <param name="cancellationToken">Cancellation token to observe while scanning.</param>
        /// <returns>A task returning a read-only list of discovered <see cref="GameEntry"/> objects.</returns>
        Task<IReadOnlyList<GameEntry>> ScanLibraryAsync(
            IEnumerable<string> sourceFolders,
            LibraryCategory category,
            IProgress<string>? progress = null,
            string? videoExtensions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the total byte size and determines if any contained file exceeds the specified size limit.
        /// </summary>
        /// <param name="folderPath">The file or directory path to inspect.</param>
        /// <param name="sizeLimit">The maximum single file size threshold (e.g., 4GB for FAT32).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning a tuple with total byte size and a boolean flag indicating whether large files exist.</returns>
        Task<(long TotalSize, bool HasLargeFiles)> GetFolderStatsAsync(string folderPath, long sizeLimit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Locates a cover image file within a game folder if available.
        /// </summary>
        /// <param name="gameFolderPath">The local game folder path.</param>
        /// <returns>The full path to the cover image file if found; otherwise, <c>null</c>.</returns>
        string? FindCoverImage(string gameFolderPath);

        /// <summary>
        /// Reads genre categories for a game from its local <c>categories.txt</c> file.
        /// </summary>
        /// <param name="gameFolderPath">The game folder path.</param>
        /// <returns>A read-only list of parsed <see cref="GameCategory"/> values.</returns>
        IReadOnlyList<GameCategory> GetCategories(string gameFolderPath);
    }

    /// <summary>
    /// Service that scans local drives and network shares for games, media files, and application directories.
    /// </summary>
    public class GameScannerService : IGameScannerService
    {
        /// <summary>
        /// Logger instance used for recording library scanning operations.
        /// </summary>
        private readonly ILogger<GameScannerService> _logger;
        private readonly IThumbnailService _thumbnailService;

        /// <summary>
        /// Array of standard cover image file names checked when locating artwork.
        /// </summary>
        private static readonly string[] CoverImageFileNames = { "cover.jpg", "cover.png", "cover.jpeg", "folder.jpg", "folder.png" };

        /// <summary>
        /// Separators used to split video file extension configuration strings.
        /// </summary>
        private static readonly char[] VideoExtensionSeparators = [',', ';', ' '];

        /// <summary>
        /// Initializes a new instance of the <see cref="GameScannerService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic logging.</param>
        public GameScannerService(ILogger<GameScannerService> logger, IThumbnailService thumbnailService)
        {
            _logger = logger;
            _thumbnailService = thumbnailService;
        }

        /// <summary>
        /// Determines whether a folder should be excluded from scanning (e.g., system directories, Recycle Bin, hidden folders).
        /// </summary>
        /// <param name="folderPath">The folder path to evaluate.</param>
        /// <returns><c>true</c> if the folder should be skipped; otherwise, <c>false</c>.</returns>
        private static bool IsExcludedFolder(string folderPath)
        {
            try
            {
                string folderName = Path.GetFileName(folderPath).TrimEnd();

                if (folderName.StartsWith('$'))
                {
                    return true;
                }

                if (string.Equals(folderName, "recyclebin", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(folderName, "System Volume Information", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                DirectoryInfo dirInfo = new(folderPath);
                return dirInfo.Exists && (dirInfo.Attributes & FileAttributes.System) == FileAttributes.System;
            }
            catch
            {
                // If we can't access it to check attributes, it's safer to exclude it.
                return true;
            }
        }

        /// <summary>
        /// Asynchronously scans the specified source directories for game, application, video, or OS image entries.
        /// </summary>
        /// <param name="sourceFolders">Root folder paths to scan.</param>
        /// <param name="category">The content category expected in the source folders.</param>
        /// <param name="progress">Optional progress reporter for status updates.</param>
        /// <param name="videoExtensions">Optional delimiter-separated string of video file extensions.</param>
        /// <param name="cancellationToken">Cancellation token to stop scanning.</param>
        /// <returns>A task returning a read-only list of discovered <see cref="GameEntry"/> objects.</returns>
        public async Task<IReadOnlyList<GameEntry>> ScanLibraryAsync(
            IEnumerable<string> sourceFolders,
            LibraryCategory category,
            IProgress<string>? progress = null,
            string? videoExtensions = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sourceFolders);

            List<GameEntry> games = [];
            HashSet<string> processedPaths = [with(StringComparer.OrdinalIgnoreCase)];

            string categoryLabel = category == LibraryCategory.App ? "app" :
                                   (category == LibraryCategory.TvAndFilm ? "film/tv" :
                                   (category == LibraryCategory.OsImage ? "OS image" : "game"));

            foreach (string sourceFolder in sourceFolders)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!Directory.Exists(sourceFolder))
                {
                    _logger.LogWarning("Source folder does not exist: {Path}", sourceFolder);
                    progress?.Report($"Skipping inaccessible: {sourceFolder}");
                    continue;
                }

                try
                {
                    progress?.Report($"Scanning: {sourceFolder}");

                    string[] initialSubdirectories = await Task.Run(() =>
                        Directory.GetDirectories(sourceFolder, "*", SearchOption.TopDirectoryOnly),
                        cancellationToken).ConfigureAwait(false);

                    if ((category == LibraryCategory.TvAndFilm && !string.IsNullOrWhiteSpace(videoExtensions)) || category == LibraryCategory.OsImage)
                    {
                        List<string> extList = [];
                        if (category == LibraryCategory.TvAndFilm)
                        {
                            ArgumentNullException.ThrowIfNull(videoExtensions);
                            extList = [.. videoExtensions.Split(VideoExtensionSeparators, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim().StartsWith('.') ? e.Trim() : "." + e.Trim())];
                        }
                        else
                        {
                            extList.Add(".iso");
                        }

                        try
                        {
                            string[] files = await Task.Run(() => Directory.GetFiles(sourceFolder, "*.*", SearchOption.TopDirectoryOnly), cancellationToken).ConfigureAwait(false);
                            foreach (string? file in files)
                            {
                                if (extList.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                                {
                                    if (processedPaths.Contains(file))
                                    {
                                        continue;
                                    }

                                    string fileName = Path.GetFileName(file);
                                    progress?.Report($"Processing file: {fileName}");
                                    FileInfo fi = new(file);
                                    long totalSize = fi.Length;
                                    bool hasLargeFiles = totalSize > RemovableDrive.Fat32MaxFileSize;

                                    string? coverImage = null;
                                    if (category == LibraryCategory.TvAndFilm)
                                    {
                                        string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyCopier", "Thumbnails");
                                        coverImage = await _thumbnailService.ExtractThumbnailAsync(file, cacheDir, cancellationToken).ConfigureAwait(false);
                                    }

                                    GameEntry entry = new(
                                        Path.GetFileNameWithoutExtension(fileName),
                                        file,
                                        totalSize,
                                        coverImage,
                                        fi.LastWriteTime < fi.CreationTime && fi.LastWriteTime != DateTime.MinValue ? fi.LastWriteTime : fi.CreationTime,
                                        hasLargeFiles,
                                        category);

                                    games.Add(entry);
                                    _ = processedPaths.Add(file);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error scanning root files in {Path}", sourceFolder);
                        }
                    }

                    if (category != LibraryCategory.OsImage)
                    {
                        List<string> foldersToProcess = [];
                        foreach (string subdir in initialSubdirectories)
                        {
                            if (IsExcludedFolder(subdir))
                            {
                                _logger.LogDebug("Excluded folder: {Path}", subdir);
                                continue;
                            }

                            string folderName = Path.GetFileName(subdir).TrimEnd();
                            if (folderName.EndsWith("collection", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    string[] collectionSubdirectories = await Task.Run(() =>
                                        Directory.GetDirectories(subdir, "*", SearchOption.TopDirectoryOnly),
                                        cancellationToken).ConfigureAwait(false);
                                    foreach (string collSubdir in collectionSubdirectories)
                                    {
                                        if (!IsExcludedFolder(collSubdir))
                                        {
                                            foldersToProcess.Add(collSubdir);
                                        }
                                        else
                                        {
                                            _logger.LogDebug("Excluded collection subfolder: {Path}", collSubdir);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error reading collection folder: {Path}", subdir);
                                }
                            }
                            else
                            {
                                foldersToProcess.Add(subdir);
                            }
                        }

                        foreach (string gameFolder in foldersToProcess)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            if (processedPaths.Contains(gameFolder))
                            {
                                _logger.LogDebug("Duplicate {Category} folder ignored: {Path}", categoryLabel, gameFolder);
                                continue;
                            }

                            try
                            {
                                string gameName = Path.GetFileName(gameFolder);
                                progress?.Report($"Processing: {gameName}");

                                (long TotalSize, bool HasLargeFiles) = await GetFolderStatsAsync(gameFolder, RemovableDrive.Fat32MaxFileSize, cancellationToken).ConfigureAwait(false);

                                string? coverImage = FindCoverImage(gameFolder);
                                IReadOnlyList<GameCategory> categoriesList = GetCategories(gameFolder);

                                GameEntry game = new(
                                    gameName,
                                    gameFolder,
                                    TotalSize,
                                    coverImage,
                                    new DirectoryInfo(gameFolder).LastWriteTime < new DirectoryInfo(gameFolder).CreationTime && new DirectoryInfo(gameFolder).LastWriteTime != DateTime.MinValue ? new DirectoryInfo(gameFolder).LastWriteTime : new DirectoryInfo(gameFolder).CreationTime,
                                    HasLargeFiles,
                                    category,
                                    categoriesList);

                                games.Add(game);
                                _ = processedPaths.Add(gameFolder);

                                _logger.LogInformation("Scanned {Category}: {Name}, Size: {Size} bytes", categoryLabel, gameName, TotalSize);
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                _logger.LogWarning(ex, "Access denied to {Category} folder: {Path}", categoryLabel, gameFolder);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error scanning {Category} folder: {Path}", categoryLabel, gameFolder);
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Access denied to source folder: {Path}", sourceFolder);
                    progress?.Report($"Access denied: {sourceFolder}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scanning source folder: {Path}", sourceFolder);
                    progress?.Report($"Error scanning: {sourceFolder}");
                }
            }

            progress?.Report($"Scan complete: {games.Count} {categoryLabel}(s) found");
            return games;
        }

        /// <summary>
        /// Asynchronously calculates file size statistics for a folder or file path.
        /// </summary>
        /// <param name="folderPath">The directory or file path.</param>
        /// <param name="sizeLimit">The single-file byte limit to check against.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning total bytes and whether any file exceeds the size threshold.</returns>
        public async Task<(long TotalSize, bool HasLargeFiles)> GetFolderStatsAsync(string folderPath, long sizeLimit, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(folderPath))
                    {
                        long size = new FileInfo(folderPath).Length;
                        return (size, size > sizeLimit);
                    }

                    DirectoryInfo dirInfo = new(folderPath);
                    long totalSize = 0;
                    bool hasLargeFiles = false;

                    foreach (FileInfo file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        long fileLength = file.Length;
                        totalSize += fileLength;
                        if (fileLength > sizeLimit)
                        {
                            hasLargeFiles = true;
                        }
                    }

                    return (totalSize, hasLargeFiles);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting folder stats: {Path}", folderPath);
                    return (0L, false);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Parses genre category information from <c>categories.txt</c> inside a game directory.
        /// </summary>
        /// <param name="gameFolderPath">The local game folder path.</param>
        /// <returns>A read-only list of parsed categories, defaulting to <see cref="GameCategory.Uncategorized"/> if absent.</returns>
        public IReadOnlyList<GameCategory> GetCategories(string gameFolderPath)
        {
            if (File.Exists(gameFolderPath))
            {
                return [];
            }
            try
            {
                string catPath = Path.Combine(gameFolderPath, "categories.txt");
                if (File.Exists(catPath))
                {
                    string[] lines = File.ReadAllLines(catPath);
                    List<GameCategory> categories = [];
                    foreach (string line in lines)
                    {
                        if (Enum.TryParse<GameCategory>(line.Trim(), true, out GameCategory cat))
                        {
                            categories.Add(cat);
                        }
                    }
                    if (categories.Count > 0)
                    {
                        return categories;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading categories for: {Path}", gameFolderPath);
            }
            return [GameCategory.Uncategorized];
        }

        /// <summary>
        /// Searches a game folder for standard cover image files (<c>cover.jpg</c>, <c>cover.png</c>, etc.).
        /// </summary>
        /// <param name="gameFolderPath">The local game folder path.</param>
        /// <returns>The full file path to the cover image if found; otherwise, <c>null</c>.</returns>
        public string? FindCoverImage(string gameFolderPath)
        {
            if (File.Exists(gameFolderPath))
            {
                return null;
            }

            try
            {
                foreach (string fileName in CoverImageFileNames)
                {
                    string fullPath = Path.Combine(gameFolderPath, fileName);
                    if (File.Exists(fullPath))
                    {
                        _logger.LogDebug("Found cover image: {Path}", fullPath);
                        return fullPath;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching for cover image in: {Path}", gameFolderPath);
            }

            return null;
        }
    }
}
