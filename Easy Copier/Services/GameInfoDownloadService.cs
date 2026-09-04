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
    public interface IGameInfoDownloadService
    {
        Task DownloadGameInfoAsync(IEnumerable<string> sourceFolders, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    }

    public sealed partial class GameInfoDownloadService : IGameInfoDownloadService, IDisposable
    {
        private readonly ILogger<GameInfoDownloadService> _logger;
        private readonly HttpClient _httpClient;

        public GameInfoDownloadService(ILogger<GameInfoDownloadService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

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

        private async Task ProcessGameAsync(string gameName, string gameFolder, CancellationToken cancellationToken)
        {
            await DownloadRequirementsAsync(gameName, gameFolder, cancellationToken);
            await DownloadCoverAsync(gameName, gameFolder, cancellationToken);
        }

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

        // --- Requirements Methods ---

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

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }

        [GeneratedRegex("<[^<]+?>")]
        private static partial Regex MyRegex();
    }
}
