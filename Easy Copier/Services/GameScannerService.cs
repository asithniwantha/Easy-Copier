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
            CancellationToken cancellationToken = default);

        Task<long> CalculateFolderSizeAsync(string folderPath, CancellationToken cancellationToken = default);
        string? FindCoverImage(string gameFolderPath);
    }

    public class GameScannerService : IGameScannerService
    {
        private readonly ILogger<GameScannerService> _logger;
        private static readonly string[] CoverImageFileNames = { "cover.jpg", "cover.png", "cover.jpeg", "folder.jpg", "folder.png" };

        public GameScannerService(ILogger<GameScannerService> logger)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyList<GameEntry>> ScanLibraryAsync(
            IEnumerable<string> sourceFolders,
            LibraryCategory category,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var games = new List<GameEntry>();
            var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var categoryLabel = category == LibraryCategory.App ? "app" : "game";

            foreach (var sourceFolder in sourceFolders)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!Directory.Exists(sourceFolder))
                {
                    _logger.LogWarning("Source folder does not exist: {Path}", sourceFolder);
                    progress?.Report($"Skipping inaccessible: {sourceFolder}");
                    continue;
                }

                try
                {
                    progress?.Report($"Scanning: {sourceFolder}");

                    var subdirectories = await Task.Run(() =>
                        Directory.GetDirectories(sourceFolder, "*", SearchOption.TopDirectoryOnly),
                        cancellationToken);

                    foreach (var gameFolder in subdirectories)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if (processedPaths.Contains(gameFolder))
                        {
                            _logger.LogDebug("Duplicate {Category} folder ignored: {Path}", categoryLabel, gameFolder);
                            continue;
                        }

                        try
                        {
                            var gameName = Path.GetFileName(gameFolder);
                            progress?.Report($"Processing: {gameName}");

                            var totalSize = await CalculateFolderSizeAsync(gameFolder, cancellationToken);
                            var coverImage = FindCoverImage(gameFolder);
                            var hasLargeFiles = await HasFilesExceedingLimitAsync(gameFolder, RemovableDrive.Fat32MaxFileSize, cancellationToken);

                            var game = new GameEntry(
                                gameName,
                                gameFolder,
                                totalSize,
                                coverImage,
                                DateTime.Now,
                                hasLargeFiles,
                                category);

                            games.Add(game);
                            processedPaths.Add(gameFolder);

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
                    var dirInfo = new DirectoryInfo(folderPath);
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
            try
            {
                foreach (var fileName in CoverImageFileNames)
                {
                    var fullPath = Path.Combine(gameFolderPath, fileName);
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
                    var dirInfo = new DirectoryInfo(folderPath);
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
