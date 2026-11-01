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
        /// <param name="overwriteExistingCovers">Whether to force re-download and replace existing cover art.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DownloadGameInfoAsync(IEnumerable<string> sourceFolders, IProgress<string>? progress = null, bool overwriteExistingCovers = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads or refreshes cover art for a single game directory.
        /// </summary>
        /// <param name="gameFolder">The directory path of the game.</param>
        /// <param name="overwrite">Whether to overwrite any existing valid cover image.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning <c>true</c> if a valid cover exists or was successfully downloaded; otherwise, <c>false</c>.</returns>
        Task<bool> DownloadCoverForGameAsync(string gameFolder, bool overwrite = true, CancellationToken cancellationToken = default);
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
        /// Indicates whether this instance owns and must dispose the HttpClient.
        /// </summary>
        private readonly bool _ownsClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameInfoDownloadService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging operation details.</param>
        /// <param name="httpClient">The HTTP client instance, or <c>null</c> to initialize an optimized pooled client.</param>
        public GameInfoDownloadService(ILogger<GameInfoDownloadService> logger, HttpClient? httpClient = null)
        {
            _logger = logger;
            if (httpClient != null)
            {
                _httpClient = httpClient;
                _ownsClient = false;
            }
            else
            {
                SocketsHttpHandler? handler = null;
                try
                {
                    handler = new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                        AutomaticDecompression = System.Net.DecompressionMethods.All
                    };
                    _httpClient = new HttpClient(handler, disposeHandler: true);
                    handler = null; // Ownership transferred to HttpClient
                }
                finally
                {
                    handler?.Dispose();
                }

                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                _ownsClient = true;
            }
        }

        /// <summary>
        /// Asynchronously scans the specified source folders and downloads system requirements, covers, and categories for each game directory.
        /// </summary>
        /// <param name="sourceFolders">The root directory paths containing game folders or collections.</param>
        /// <param name="progress">Optional status progress reporter.</param>
        /// <param name="overwriteExistingCovers">Whether to overwrite existing cover files.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DownloadGameInfoAsync(
            IEnumerable<string> sourceFolders,
            IProgress<string>? progress = null,
            bool overwriteExistingCovers = false,
            CancellationToken cancellationToken = default)
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
                    string[] initialSubdirectories = await Task.Run(() => Directory.GetDirectories(sourceFolder, "*", SearchOption.TopDirectoryOnly), cancellationToken).ConfigureAwait(false);

                    List<string> foldersToProcess = [];
                    foreach (string? subdir in initialSubdirectories)
                    {
                        string folderName = Path.GetFileName(subdir).TrimEnd();
                        if (folderName.EndsWith("collection", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                string[] collectionSubdirectories = await Task.Run(() => Directory.GetDirectories(subdir, "*", SearchOption.TopDirectoryOnly), cancellationToken).ConfigureAwait(false);
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

                        await ProcessGameAsync(gameName, gameFolder, overwriteExistingCovers, cancellationToken).ConfigureAwait(false);
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
        /// Downloads or refreshes cover art for a single game directory.
        /// </summary>
        /// <param name="gameFolder">The directory path of the game.</param>
        /// <param name="overwrite">Whether to overwrite any existing valid cover image.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning <c>true</c> if a valid cover exists or was successfully downloaded; otherwise, <c>false</c>.</returns>
        public async Task<bool> DownloadCoverForGameAsync(string gameFolder, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(gameFolder) || !Directory.Exists(gameFolder))
            {
                return false;
            }

            string gameName = Path.GetFileName(gameFolder);
            await DownloadCoverAsync(gameName, gameFolder, overwrite, cancellationToken).ConfigureAwait(false);

            string coverPath = Path.Combine(gameFolder, "cover.jpg");
            // Minimum payload threshold of 1,500 bytes ensures that empty spacer GIFs or small HTML 404 error pages
            // are treated as failed downloads rather than valid cover artwork.
            return File.Exists(coverPath) && new FileInfo(coverPath).Length >= 1500;
        }

        /// <summary>
        /// Processes an individual game directory by fetching its requirements, cover image, and categories.
        /// </summary>
        /// <param name="gameName">The title of the game.</param>
        /// <param name="gameFolder">The local folder path of the game.</param>
        /// <param name="overwriteExistingCovers">Whether to force re-download of existing covers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessGameAsync(string gameName, string gameFolder, bool overwriteExistingCovers, CancellationToken cancellationToken)
        {
            await DownloadRequirementsAsync(gameName, gameFolder, cancellationToken).ConfigureAwait(false);
            await DownloadCoverAsync(gameName, gameFolder, overwriteExistingCovers, cancellationToken).ConfigureAwait(false);
            await DownloadCategoriesAsync(gameName, gameFolder, overwriteExistingCovers, cancellationToken).ConfigureAwait(false);
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
                string content = await File.ReadAllTextAsync(reqFile, cancellationToken).ConfigureAwait(false);
                if (content.Contains("Steam Web API", StringComparison.Ordinal))
                {
                    return; // Already fetched
                }
            }

            try
            {
                Dictionary<string, Dictionary<string, string>>? reqs = await FetchSteamRequirementsAsync(gameName, cancellationToken).ConfigureAwait(false);
                if (reqs != null)
                {
                    string formatted = FormatRequirements(gameName, reqs);
                    await File.WriteAllTextAsync(reqFile, formatted, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(300, cancellationToken).ConfigureAwait(false); // Rate limiting
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download requirements for {GameName}", gameName);
            }
        }

        /// <summary>
        /// Downloads cover image for a game from various online services (Steam, CheapShark, GOG, PCGW, Wikipedia, OpenCritic, Lutris, GSR)
        /// and saves it to <c>cover.jpg</c>. Automatically heals corrupted/empty covers and uses candidate title queries.
        /// </summary>
        /// <param name="gameName">The raw or cleaned title of the game.</param>
        /// <param name="gameFolder">The directory path of the game.</param>
        /// <param name="overwriteExisting">Whether to force re-download when a valid cover is present.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DownloadCoverAsync(string gameName, string gameFolder, bool overwriteExisting, CancellationToken cancellationToken)
        {
            string coverPath = Path.Combine(gameFolder, "cover.jpg");
            if (File.Exists(coverPath))
            {
                FileInfo fi = new(coverPath);
                // Self-healing: if existing cover is valid and overwrite was not requested, keep it.
                // The 1,500-byte threshold distinguishes genuine cover art (typically 10KB-500KB) from truncated 0-byte files,
                // 1x1 transparent tracking pixels, or small textual HTML error responses (usually < 1.5KB).
                if (fi.Length >= 1500 && !overwriteExisting)
                {
                    return;
                }

                // If corrupted (0-byte or < 1500 bytes) or overwrite is true, delete existing file before fetching
                try
                {
                    File.Delete(coverPath);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to delete old/corrupted cover at {Path}", coverPath);
                }
            }

            try
            {
                // Generate prioritized query candidates (cleaned, edition-stripped, year-stripped)
                IReadOnlyList<string> candidates = GameTitleSanitizer.GenerateSearchCandidates(gameName);

                foreach (string candidate in candidates)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // 1. Steam CDN via App ID
                    string? appId = await FetchSteamAppIdAsync(candidate, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(appId) && await TryFetchSteamCoverAsync(appId, coverPath, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // 2. CheapShark (direct Steam App ID resolution or game thumbnail)
                    if (await TryFetchCheapSharkCoverAsync(candidate, coverPath, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // 3. GOG Catalog API
                    if (await TryFetchGogCoverAsync(candidate, coverPath, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // 4. PCGamingWiki API
                    if (await TryFetchPcgwCoverAsync(candidate, coverPath, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // 5. Wikipedia MediaWiki page images / summary
                    if (await TryFetchWikipediaCoverAsync(candidate, coverPath, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // 6. OpenCritic API
                    if (await TryFetchOpenCriticCoverAsync(candidate, coverPath, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // 7. Lutris REST API & scrape
                    if (await TryFetchLutrisCoverAsync(candidate, coverPath, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // 8. GameSystemRequirements
                    if (await TryFetchGsrCoverAsync(candidate, coverPath, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download cover for {GameName}", gameName);
            }
        }

        /// <summary>
        /// Downloads an image from the specified URL and saves it to a local file path after verifying image data integrity.
        /// </summary>
        /// <param name="url">The remote URL of the image.</param>
        /// <param name="outputPath">The local destination file path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning <c>true</c> if the image was valid and downloaded successfully; otherwise, <c>false</c>.</returns>
        private async Task<bool> DownloadImageAsync(string url, string outputPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

                    // Ensure payload is a genuine image with valid magic bytes and sufficient size
                    if (!GameTitleSanitizer.IsValidImage(bytes))
                    {
                        _logger.LogDebug("Image payload from {Url} failed validation (length: {Length})", url, bytes.Length);
                        return false;
                    }

                    string? parentDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    await File.WriteAllBytesAsync(outputPath, bytes, cancellationToken).ConfigureAwait(false);
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
            if (_ownsClient)
            {
                _httpClient?.Dispose();
            }
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
