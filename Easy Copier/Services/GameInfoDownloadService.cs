using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for downloading game metadata, system requirements, covers, and category information for game directories.
    /// </summary>
    public interface IGameInfoDownloadService
    {
        /// <summary>
        /// Asynchronously scans game source directories and downloads missing metadata (system requirements, cover art, genres/categories).
        /// </summary>
        /// <param name="sourceFolders">Collection of source folder directory paths to scan and process.</param>
        /// <param name="progress">An optional progress reporter for reporting status messages.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DownloadGameInfoAsync(IEnumerable<string> sourceFolders, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service that downloads game system requirements, cover images, and category metadata from various web APIs and web pages.
    /// </summary>
    public sealed partial class GameInfoDownloadService : IGameInfoDownloadService, IDisposable
    {
        /// <summary>
        /// Logger instance used for diagnostic logging.
        /// </summary>
        private readonly ILogger<GameInfoDownloadService> _logger;

        /// <summary>
        /// HTTP client instance used for web scraping and API requests.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameInfoDownloadService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging operation details.</param>
        public GameInfoDownloadService(ILogger<GameInfoDownloadService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// Asynchronously scans the specified source folders and downloads system requirements, covers, and categories for each game directory.
        /// </summary>
        /// <param name="sourceFolders">The root directory paths containing game folders or collections.</param>
        /// <param name="progress">Optional status progress reporter.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DownloadGameInfoAsync(IEnumerable<string> sourceFolders, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sourceFolders);
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
                    string[] initialSubdirectories = await Task.Run(() => Directory.GetDirectories(sourceFolder, "*", SearchOption.TopDirectoryOnly), cancellationToken);

                    List<string> foldersToProcess = [];
                    foreach (string? subdir in initialSubdirectories)
                    {
                        string folderName = Path.GetFileName(subdir).TrimEnd();
                        if (folderName.EndsWith("collection", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                string[] collectionSubdirectories = await Task.Run(() => Directory.GetDirectories(subdir, "*", SearchOption.TopDirectoryOnly), cancellationToken);
                                foldersToProcess.AddRange(collectionSubdirectories);
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

                    int idx = 1;
                    foreach (string gameFolder in foldersToProcess)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        string gameName = Path.GetFileName(gameFolder);
                        progress?.Report($"[{idx}/{foldersToProcess.Count}] Processing: {gameName}");

                        await ProcessGameAsync(gameName, gameFolder, cancellationToken);
                        idx++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing source folder: {Path}", sourceFolder);
                    progress?.Report($"Error processing: {sourceFolder}");
                }
            }
            progress?.Report("Download complete");
        }

        /// <summary>
        /// Processes an individual game directory by fetching its requirements, cover image, and categories.
        /// </summary>
        /// <param name="gameName">The title of the game.</param>
        /// <param name="gameFolder">The local folder path of the game.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessGameAsync(string gameName, string gameFolder, CancellationToken cancellationToken)
        {
            await DownloadRequirementsAsync(gameName, gameFolder, cancellationToken);
            await DownloadCoverAsync(gameName, gameFolder, cancellationToken);
            await DownloadCategoriesAsync(gameName, gameFolder, cancellationToken);
        }

        /// <summary>
        /// Downloads system requirements for a game from Steam Web API and saves them to <c>system_requirements.txt</c>.
        /// </summary>
        /// <param name="gameName">The title of the game.</param>
        /// <param name="gameFolder">The directory path of the game.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DownloadRequirementsAsync(string gameName, string gameFolder, CancellationToken cancellationToken)
        {
            string reqFile = Path.Combine(gameFolder, "system_requirements.txt");
            if (File.Exists(reqFile))
            {
                string content = await File.ReadAllTextAsync(reqFile, cancellationToken);
                if (content.Contains("Steam Web API", StringComparison.Ordinal))
                {
                    return; // Already fetched
                }
            }

            try
            {
                Dictionary<string, Dictionary<string, string>>? reqs = await FetchSteamRequirementsAsync(gameName, cancellationToken);
                if (reqs != null)
                {
                    string formatted = FormatRequirements(gameName, reqs);
                    await File.WriteAllTextAsync(reqFile, formatted, cancellationToken);
                    await Task.Delay(300, cancellationToken); // Rate limiting
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download requirements for {GameName}", gameName);
            }
        }

        /// <summary>
        /// Downloads cover image for a game from various online services (Steam, GOG, PCGW, Lutris, Wikipedia, OpenCritic) and saves it to <c>cover.jpg</c>.
        /// </summary>
        /// <param name="gameName">The title of the game.</param>
        /// <param name="gameFolder">The directory path of the game.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DownloadCoverAsync(string gameName, string gameFolder, CancellationToken cancellationToken)
        {
            string coverPath = Path.Combine(gameFolder, "cover.jpg");
            if (File.Exists(coverPath))
            {
                return;
            }

            try
            {
                string? appId = await FetchSteamAppIdAsync(gameName, cancellationToken);
                if (appId != null && await TryFetchSteamCoverAsync(appId, coverPath, cancellationToken))
                {
                    await Task.Delay(200, cancellationToken);
                    return;
                }

                if (await TryFetchGogCoverAsync(gameName, coverPath, cancellationToken)) { await Task.Delay(200, cancellationToken); return; }
                if (await TryFetchGsrCoverAsync(gameName, coverPath, cancellationToken)) { await Task.Delay(200, cancellationToken); return; }
                if (await TryFetchWikipediaCoverAsync(gameName, coverPath, cancellationToken)) { await Task.Delay(200, cancellationToken); return; }
                if (await TryFetchPcgwCoverAsync(gameName, coverPath, cancellationToken)) { await Task.Delay(200, cancellationToken); return; }
                if (await TryFetchOpenCriticCoverAsync(gameName, coverPath, cancellationToken)) { await Task.Delay(200, cancellationToken); return; }
                if (await TryFetchLutrisCoverAsync(gameName, coverPath, cancellationToken)) { await Task.Delay(200, cancellationToken); return; }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download cover for {GameName}", gameName);
            }
        }

        /// <summary>
        /// Downloads an image from the specified URL and saves it to a local file path.
        /// </summary>
        /// <param name="url">The remote URL of the image.</param>
        /// <param name="outputPath">The local destination file path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning <c>true</c> if the image was downloaded successfully; otherwise, <c>false</c>.</returns>
        private async Task<bool> DownloadImageAsync(string url, string outputPath, CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(new Uri(url), cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    await File.WriteAllBytesAsync(outputPath, bytes, cancellationToken);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DownloadImage failed for {Url}", url);
            }
            return false;
        }

        /// <summary>
        /// Disposes internal resources used by <see cref="GameInfoDownloadService"/>.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Generates a regular expression for stripping HTML tags.
        /// </summary>
        /// <returns>A <see cref="Regex"/> instance matching HTML tags.</returns>
        [GeneratedRegex("<[^<]+?>")]
        private static partial Regex MyRegex();
    }
}
