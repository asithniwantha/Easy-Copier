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
    public interface IGameScannerService
    {
        Task<IReadOnlyList<GameEntry>> ScanLibraryAsync(
            IEnumerable<string> sourceFolders,
            LibraryCategory category,
            IProgress<string>? progress = null,
            string? videoExtensions = null,
            CancellationToken cancellationToken = default);

        Task<long> CalculateFolderSizeAsync(string folderPath, CancellationToken cancellationToken = default);
        string? FindCoverImage(string gameFolderPath);
    }

    public class GameScannerService : IGameScannerService
    {
        private readonly ILogger<GameScannerService> _logger;
        private static readonly string[] CoverImageFileNames = { "cover.jpg", "cover.png", "cover.jpeg", "folder.jpg", "folder.png" };
        private static readonly char[] VideoExtensionSeparators = [',', ';', ' '];

        public GameScannerService(ILogger<GameScannerService> logger)
        {
            _logger = logger;
        }

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

        public async Task<IReadOnlyList<GameEntry>> ScanLibraryAsync(
            IEnumerable<string> sourceFolders,
            LibraryCategory category,
            IProgress<string>? progress = null,
            string? videoExtensions = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sourceFolders);

            List<GameEntry> games = [];
            HashSet<string> processedPaths = new(StringComparer.OrdinalIgnoreCase);

            string categoryLabel = category == LibraryCategory.App ? "app" :
                                   (category == LibraryCategory.TvAndFilm ? "film/tv" : "game");

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

                    if (category == LibraryCategory.TvAndFilm && !string.IsNullOrWhiteSpace(videoExtensions))
                    {
                        List<string> extList = videoExtensions.Split(VideoExtensionSeparators, StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(e => e.Trim().StartsWith('.') ? e.Trim() : "." + e.Trim())
                                                     .ToList();

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

                                    GameEntry entry = new(
                                        Path.GetFileNameWithoutExtension(fileName),
                                        file,
                                        totalSize,
                                        null, // Standalone files covers to be implemented later
                                        DateTime.Now,
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

                            long totalSize = await CalculateFolderSizeAsync(gameFolder, cancellationToken).ConfigureAwait(false);
                            string? coverImage = FindCoverImage(gameFolder);
                            bool hasLargeFiles = await HasFilesExceedingLimitAsync(gameFolder, RemovableDrive.Fat32MaxFileSize, cancellationToken).ConfigureAwait(false);

                            GameEntry game = new(
                                gameName,
                                gameFolder,
                                totalSize,
                                coverImage,
                                DateTime.Now,
                                hasLargeFiles,
                                category);

                            games.Add(game);
                            _ = processedPaths.Add(gameFolder);

                            _logger.LogInformation("Scanned {Category}: {Name}, Size: {Size} bytes", categoryLabel, gameName, totalSize);
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

        public async Task<long> CalculateFolderSizeAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(folderPath))
                    {
                        return new FileInfo(folderPath).Length;
                    }
                    DirectoryInfo dirInfo = new(folderPath);
                    return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(file => file.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error calculating folder size: {Path}", folderPath);
                    return 0L;
                }
            }, cancellationToken);
        }

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

        private async Task<bool> HasFilesExceedingLimitAsync(string folderPath, long sizeLimit, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(folderPath))
                    {
                        return new FileInfo(folderPath).Length > sizeLimit;
                    }

                    DirectoryInfo dirInfo = new(folderPath);
                    return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Any(file => file.Length > sizeLimit);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking file sizes in: {Path}", folderPath);
                    return false;
                }
            }, cancellationToken);
        }
    }
}
