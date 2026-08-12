using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Easy_Copier.Services
{
    public interface IGameInfoDownloadService
    {
        Task DownloadGameInfoAsync(IEnumerable<string> sourceFolders, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    }

    public class GameInfoDownloadService : IGameInfoDownloadService
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
            foreach (var sourceFolder in sourceFolders)
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (!Directory.Exists(sourceFolder))
                {
                    _logger.LogWarning("Source folder does not exist: {Path}", sourceFolder);
                    progress?.Report($"Skipping inaccessible: {sourceFolder}");
                    continue;
                }

                try
                {
                    progress?.Report($"Scanning: {sourceFolder}");
                    var initialSubdirectories = await Task.Run(() => Directory.GetDirectories(sourceFolder, "*", SearchOption.TopDirectoryOnly), cancellationToken);

                    var foldersToProcess = new List<string>();
                    foreach (var subdir in initialSubdirectories)
                    {
                        var folderName = Path.GetFileName(subdir).TrimEnd();
                        if (folderName.EndsWith("collection", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var collectionSubdirectories = await Task.Run(() => Directory.GetDirectories(subdir, "*", SearchOption.TopDirectoryOnly), cancellationToken);
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
                    foreach (var gameFolder in foldersToProcess)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var gameName = Path.GetFileName(gameFolder);
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
            var reqFile = Path.Combine(gameFolder, "system_requirements.txt");
            if (File.Exists(reqFile))
            {
                var content = await File.ReadAllTextAsync(reqFile, cancellationToken);
                if (content.Contains("Steam Web API")) return; // Already fetched
            }

            try
            {
                var reqs = await FetchSteamRequirementsAsync(gameName, cancellationToken);
                if (reqs != null)
                {
                    var formatted = FormatRequirements(gameName, reqs);
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
            var coverPath = Path.Combine(gameFolder, "cover.jpg");
            if (File.Exists(coverPath)) return;

            try
            {
                var appId = await FetchSteamAppIdAsync(gameName, cancellationToken);
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

        private async Task<Dictionary<string, Dictionary<string, string>>?> FetchSteamRequirementsAsync(string gameName, CancellationToken cancellationToken)
        {
            var appId = await FetchSteamAppIdAsync(gameName, cancellationToken);
            if (appId == null) return null;

            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty(appId, out var appData) || !appData.GetProperty("success").GetBoolean()) return null;

            var data = appData.GetProperty("data");
            var requirements = new Dictionary<string, Dictionary<string, string>>();

            if (data.TryGetProperty("pc_requirements", out var pcReqs))
            {
                if (pcReqs.TryGetProperty("minimum", out var minProp))
                {
                    var parsed = ParseSteamRequirements(minProp.GetString());
                    if (parsed.Count > 0) requirements["minimum"] = parsed;
                }
                if (pcReqs.TryGetProperty("recommended", out var recProp))
                {
                    var parsed = ParseSteamRequirements(recProp.GetString());
                    if (parsed.Count > 0) requirements["recommended"] = parsed;
                }
            }

            return requirements.Count > 0 ? requirements : null;
        }

        private Dictionary<string, string> ParseSteamRequirements(string? html)
        {
            var specs = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(html)) return specs;

            var text = Regex.Replace(html, "<[^<]+?>", "");
            text = Regex.Replace(text, "\n+", "\n").Trim();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var t = line.Trim();
                if (string.IsNullOrEmpty(t)) continue;

                if (Regex.IsMatch(t, "cpu|processor", RegexOptions.IgnoreCase)) specs["CPU"] = t;
                else if (Regex.IsMatch(t, "gpu|graphics|video|directx", RegexOptions.IgnoreCase)) specs["GPU"] = t;
                else if (Regex.IsMatch(t, "memory|ram|gb", RegexOptions.IgnoreCase)) specs["RAM"] = t;
                else if (Regex.IsMatch(t, "storage|disk|space", RegexOptions.IgnoreCase)) specs["Storage"] = t;
            }
            return specs;
        }

        private string FormatRequirements(string gameName, Dictionary<string, Dictionary<string, string>> requirements)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"SYSTEM REQUIREMENTS FOR: {gameName}");
            sb.AppendLine(new string('=', 70));
            sb.AppendLine();

            sb.AppendLine("MINIMUM REQUIREMENTS:");
            sb.AppendLine(new string('-', 70));
            var minSpecs = requirements.ContainsKey("minimum") ? requirements["minimum"] : new Dictionary<string, string>();
            sb.AppendLine($"CPU: {(minSpecs.TryGetValue("CPU", out var cpu) ? cpu : "Not available")}");
            sb.AppendLine($"GPU: {(minSpecs.TryGetValue("GPU", out var gpu) ? gpu : "Not available")}");
            sb.AppendLine($"RAM: {(minSpecs.TryGetValue("RAM", out var ram) ? ram : "Not available")}");
            sb.AppendLine($"Storage: {(minSpecs.TryGetValue("Storage", out var storage) ? storage : "Not available")}");
            sb.AppendLine();

            sb.AppendLine("RECOMMENDED REQUIREMENTS:");
            sb.AppendLine(new string('-', 70));
            var recSpecs = requirements.ContainsKey("recommended") ? requirements["recommended"] : new Dictionary<string, string>();
            sb.AppendLine($"CPU: {(recSpecs.TryGetValue("CPU", out var rcpu) ? rcpu : "Not available")}");
            sb.AppendLine($"GPU: {(recSpecs.TryGetValue("GPU", out var rgpu) ? rgpu : "Not available")}");
            sb.AppendLine($"RAM: {(recSpecs.TryGetValue("RAM", out var rram) ? rram : "Not available")}");
            sb.AppendLine($"Storage: {(recSpecs.TryGetValue("Storage", out var rstorage) ? rstorage : "Not available")}");
            sb.AppendLine();

            sb.AppendLine(new string('-', 70));
            sb.AppendLine($"Created on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("Source: Fetched from Steam Web API");

            return sb.ToString();
        }

        // --- Cover Methods ---

        private async Task<string?> FetchSteamAppIdAsync(string gameName, CancellationToken cancellationToken)
        {
            var url = $"https://steamcommunity.com/actions/SearchApps/{Uri.EscapeDataString(gameName)}";
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode) return null;
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.GetArrayLength() > 0)
                {
                    return doc.RootElement[0].GetProperty("appid").GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Steam AppId fetch failed for {GameName}", gameName);
            }
            return null;
        }

        private async Task<bool> DownloadImageAsync(string url, string outputPath, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
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

        private async Task<bool> TryFetchSteamCoverAsync(string appId, string coverPath, CancellationToken cancellationToken)
        {
            var url = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/library_600x900.jpg";
            return await DownloadImageAsync(url, coverPath, cancellationToken);
        }

        private async Task<bool> TryFetchGogCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            var url = $"https://catalog.gog.com/v1/catalog?query=like:{Uri.EscapeDataString(gameName)}&limit=1";
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("products", out var products) && products.GetArrayLength() > 0)
                    {
                        var product = products[0];
                        if (product.TryGetProperty("coverVertical", out var cover))
                        {
                            var imgUrl = cover.GetString();
                            if (!string.IsNullOrEmpty(imgUrl))
                            {
                                imgUrl = imgUrl.Replace("{formatter}", "avif").Replace("{ext}", "webp");
                                return await DownloadImageAsync(imgUrl, coverPath, cancellationToken);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "GOG cover failed for {GameName}", gameName); }
            return false;
        }

        private async Task<bool> TryFetchGsrCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            var url = $"https://gamesystemrequirements.com/games.php?req={Uri.EscapeDataString(gameName)}";
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync(cancellationToken);
                    var match = Regex.Match(html, @"<div class=""game-item"">.*?<a href=""([^""]+)"">.*?<img src=""([^""]+)""", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        var imgUrl = match.Groups[2].Value;
                        if (imgUrl.StartsWith("/")) imgUrl = "https://gamesystemrequirements.com" + imgUrl;
                        return await DownloadImageAsync(imgUrl, coverPath, cancellationToken);
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "GSR cover failed for {GameName}", gameName); }
            return false;
        }

        private async Task<bool> TryFetchWikipediaCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            var url = $"https://en.wikipedia.org/wiki/Special:Search?search={Uri.EscapeDataString(gameName)}";
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync(cancellationToken);
                    var match = Regex.Match(html, @"<table class=""infobox[^""]*"">.*?<img[^>]+src=""([^""]+)""", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        var imgUrl = match.Groups[1].Value;
                        if (imgUrl.StartsWith("//")) imgUrl = "https:" + imgUrl;
                        else if (imgUrl.StartsWith("/")) imgUrl = "https://en.wikipedia.org" + imgUrl;

                        imgUrl = imgUrl.Replace("/220px-", "/500px-").Replace("/250px-", "/500px-");
                        return await DownloadImageAsync(imgUrl, coverPath, cancellationToken);
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Wikipedia cover failed for {GameName}", gameName); }
            return false;
        }

        private async Task<bool> TryFetchPcgwCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            var url = $"https://www.pcgamingwiki.com/w/api.php?action=query&prop=pageimages&titles={Uri.EscapeDataString(gameName)}&format=json&pithumbsize=800";
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("query", out var query) && query.TryGetProperty("pages", out var pages))
                    {
                        foreach (var pageProp in pages.EnumerateObject())
                        {
                            if (pageProp.Name != "-1" && pageProp.Value.TryGetProperty("thumbnail", out var thumb))
                            {
                                if (thumb.TryGetProperty("source", out var source))
                                {
                                    return await DownloadImageAsync(source.GetString() ?? "", coverPath, cancellationToken);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "PCGW cover failed for {GameName}", gameName); }
            return false;
        }

        private async Task<bool> TryFetchOpenCriticCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            var url = $"https://api.opencritic.com/api/game/search?criteria={Uri.EscapeDataString(gameName)}";
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.GetArrayLength() > 0)
                    {
                        var gameId = doc.RootElement[0].GetProperty("id").GetInt32();
                        var gameUrl = $"https://api.opencritic.com/api/game/{gameId}";
                        var gameResponse = await _httpClient.GetAsync(gameUrl, cancellationToken);
                        if (gameResponse.IsSuccessStatusCode)
                        {
                            var gameContent = await gameResponse.Content.ReadAsStringAsync(cancellationToken);
                            using var gameDoc = JsonDocument.Parse(gameContent);
                            var root = gameDoc.RootElement;

                            string? imgUrl = null;
                            if (root.TryGetProperty("images", out var images) && images.TryGetProperty("boxArt", out var boxArt) && boxArt.TryGetProperty("og", out var og))
                            {
                                imgUrl = $"https://img.opencritic.com/{og.GetString()}";
                            }
                            else if (root.TryGetProperty("bannerImageUrl", out var banner))
                            {
                                imgUrl = banner.GetString();
                                if (!string.IsNullOrEmpty(imgUrl) && !imgUrl.StartsWith("http"))
                                {
                                    imgUrl = $"https://img.opencritic.com/{imgUrl}";
                                }
                            }

                            if (!string.IsNullOrEmpty(imgUrl))
                            {
                                return await DownloadImageAsync(imgUrl, coverPath, cancellationToken);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "OpenCritic cover failed for {GameName}", gameName); }
            return false;
        }

        private async Task<bool> TryFetchLutrisCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            var url = $"https://lutris.net/games/?q={Uri.EscapeDataString(gameName)}";
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync(cancellationToken);
                    var match = Regex.Match(html, @"<a href=""/games/[^/]+/"".*?<img.*?src=""([^""]+)""", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        var imgUrl = match.Groups[1].Value;
                        if (imgUrl.StartsWith("/")) imgUrl = "https://lutris.net" + imgUrl;
                        return await DownloadImageAsync(imgUrl, coverPath, cancellationToken);
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Lutris cover failed for {GameName}", gameName); }
            return false;
        }
    }
}
