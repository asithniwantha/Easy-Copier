using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        private static readonly HttpClient _httpClient = new();

        static GameInfoDownloadService()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public GameInfoDownloadService(ILogger<GameInfoDownloadService> logger)
        {
            _logger = logger;
        }

        private async Task<string?> FetchStringAsync(Uri url, CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching data from {Url}", url);
            }
            return null;
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

        private async Task<Dictionary<string, Dictionary<string, string>>?> FetchSteamRequirementsAsync(string gameName, CancellationToken cancellationToken)
        {
            string? appId = await FetchSteamAppIdAsync(gameName, cancellationToken);
            if (appId == null)
            {
                return null;
            }

            Uri url = new($"https://store.steampowered.com/api/appdetails?appids={appId}");
            string? content = await FetchStringAsync(url, cancellationToken);
            if (content == null)
            {
                return null;
            }
            using JsonDocument doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty(appId, out JsonElement appData) || !appData.GetProperty("success").GetBoolean())
            {
                return null;
            }

            JsonElement data = appData.GetProperty("data");
            Dictionary<string, Dictionary<string, string>> requirements = [];

            if (data.TryGetProperty("pc_requirements", out JsonElement pcReqs))
            {
                if (pcReqs.TryGetProperty("minimum", out JsonElement minProp))
                {
                    Dictionary<string, string> parsed = ParseSteamRequirements(minProp.GetString());
                    if (parsed.Count > 0)
                    {
                        requirements["minimum"] = parsed;
                    }
                }
                if (pcReqs.TryGetProperty("recommended", out JsonElement recProp))
                {
                    Dictionary<string, string> parsed = ParseSteamRequirements(recProp.GetString());
                    if (parsed.Count > 0)
                    {
                        requirements["recommended"] = parsed;
                    }
                }
            }

            return requirements.Count > 0 ? requirements : null;
        }

        private static Dictionary<string, string> ParseSteamRequirements(string? html)
        {
            Dictionary<string, string> specs = [];
            if (string.IsNullOrWhiteSpace(html))
            {
                return specs;
            }

            string text = MyRegex().Replace(html, "");
            text = Regex.Replace(text, "\n+", "\n").Trim();
            string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string t = line.Trim();
                if (string.IsNullOrEmpty(t))
                {
                    continue;
                }

                if (Regex.IsMatch(t, "cpu|processor", RegexOptions.IgnoreCase))
                {
                    specs["CPU"] = t;
                }
                else if (Regex.IsMatch(t, "gpu|graphics|video|directx", RegexOptions.IgnoreCase))
                {
                    specs["GPU"] = t;
                }
                else if (Regex.IsMatch(t, "memory|ram|gb", RegexOptions.IgnoreCase))
                {
                    specs["RAM"] = t;
                }
                else if (Regex.IsMatch(t, "storage|disk|space", RegexOptions.IgnoreCase))
                {
                    specs["Storage"] = t;
                }
            }
            return specs;
        }

        private static string FormatRequirements(string gameName, Dictionary<string, Dictionary<string, string>> requirements)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"SYSTEM REQUIREMENTS FOR: {gameName}");
            _ = sb.AppendLine(new string('=', 70));
            _ = sb.AppendLine();

            _ = sb.AppendLine("MINIMUM REQUIREMENTS:");
            _ = sb.AppendLine(new string('-', 70));
            if (!requirements.TryGetValue("minimum", out Dictionary<string, string>? minSpecs))
            {
                minSpecs = [];
            }
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"CPU: {(minSpecs.TryGetValue("CPU", out string? cpu) ? cpu : "Not available")}");
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"GPU: {(minSpecs.TryGetValue("GPU", out string? gpu) ? gpu : "Not available")}");
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"RAM: {(minSpecs.TryGetValue("RAM", out string? ram) ? ram : "Not available")}");
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Storage: {(minSpecs.TryGetValue("Storage", out string? storage) ? storage : "Not available")}");
            _ = sb.AppendLine();

            _ = sb.AppendLine("RECOMMENDED REQUIREMENTS:");
            _ = sb.AppendLine(new string('-', 70));
            if (!requirements.TryGetValue("recommended", out Dictionary<string, string>? recSpecs))
            {
                recSpecs = [];
            }
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"CPU: {(recSpecs.TryGetValue("CPU", out string? rcpu) ? rcpu : "Not available")}");
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"GPU: {(recSpecs.TryGetValue("GPU", out string? rgpu) ? rgpu : "Not available")}");
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"RAM: {(recSpecs.TryGetValue("RAM", out string? rram) ? rram : "Not available")}");
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Storage: {(recSpecs.TryGetValue("Storage", out string? rstorage) ? rstorage : "Not available")}");
            _ = sb.AppendLine();

            _ = sb.AppendLine(new string('-', 70));
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Created on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _ = sb.AppendLine("Source: Fetched from Steam Web API");

            return sb.ToString();
        }

        // --- Cover Methods ---

        private async Task<string?> FetchSteamAppIdAsync(string gameName, CancellationToken cancellationToken)
        {
            Uri url = new($"https://steamcommunity.com/actions/SearchApps/{Uri.EscapeDataString(gameName)}");
            try
            {
                string? json = await FetchStringAsync(url, cancellationToken);
                if (json == null)
                {
                    return null;
                }
                using JsonDocument doc = JsonDocument.Parse(json);
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

        private async Task<bool> TryFetchSteamCoverAsync(string appId, string coverPath, CancellationToken cancellationToken)
        {
            string url = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/library_600x900.jpg";
            return await DownloadImageAsync(url, coverPath, cancellationToken);
        }

        private async Task<bool> TryFetchGogCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            Uri url = new($"https://catalog.gog.com/v1/catalog?query=like:{Uri.EscapeDataString(gameName)}&limit=1");
            try
            {
                string? json = await FetchStringAsync(url, cancellationToken);
                if (json != null)
                {
                    using JsonDocument doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("products", out JsonElement products) && products.GetArrayLength() > 0)
                    {
                        JsonElement product = products[0];
                        if (product.TryGetProperty("coverVertical", out JsonElement cover))
                        {
                            string? imgUrl = cover.GetString();
                            if (!string.IsNullOrEmpty(imgUrl))
                            {
                                imgUrl = imgUrl.Replace("{formatter}", "avif", StringComparison.Ordinal).Replace("{ext}", "webp", StringComparison.Ordinal);
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
            Uri url = new($"https://gamesystemrequirements.com/games.php?req={Uri.EscapeDataString(gameName)}");
            try
            {
                string? html = await FetchStringAsync(url, cancellationToken);
                if (html != null)
                {
                    Match match = Regex.Match(html, @"<div class=""game-item"">.*?<a href=""([^""]+)"">.*?<img src=""([^""]+)""", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        string imgUrl = match.Groups[2].Value;
                        if (imgUrl.StartsWith('/'))
                        {
                            imgUrl = "https://gamesystemrequirements.com" + imgUrl;
                        }

                        return await DownloadImageAsync(imgUrl, coverPath, cancellationToken);
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "GSR cover failed for {GameName}", gameName); }
            return false;
        }

        private async Task<bool> TryFetchWikipediaCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            Uri url = new($"https://en.wikipedia.org/wiki/Special:Search?search={Uri.EscapeDataString(gameName)}");
            try
            {
                string? html = await FetchStringAsync(url, cancellationToken);
                if (html != null)
                {
                    Match match = Regex.Match(html, @"<table class=""infobox[^""]*"">.*?<img[^>]+src=""([^""]+)""", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        string imgUrl = match.Groups[1].Value;
                        if (imgUrl.StartsWith("//", StringComparison.Ordinal))
                        {
                            imgUrl = "https:" + imgUrl;
                        }
                        else if (imgUrl.StartsWith('/'))
                        {
                            imgUrl = "https://en.wikipedia.org" + imgUrl;
                        }

                        imgUrl = imgUrl.Replace("/220px-", "/500px-", StringComparison.Ordinal).Replace("/250px-", "/500px-", StringComparison.Ordinal);
                        return await DownloadImageAsync(imgUrl, coverPath, cancellationToken);
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Wikipedia cover failed for {GameName}", gameName); }
            return false;
        }

        private async Task<bool> TryFetchPcgwCoverAsync(string gameName, string coverPath, CancellationToken cancellationToken)
        {
            Uri url = new($"https://www.pcgamingwiki.com/w/api.php?action=query&prop=pageimages&titles={Uri.EscapeDataString(gameName)}&format=json&pithumbsize=800");
            try
            {
                string? json = await FetchStringAsync(url, cancellationToken);
                if (json != null)
                {
                    using JsonDocument doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("query", out JsonElement query) && query.TryGetProperty("pages", out JsonElement pages))
                    {
                        foreach (JsonProperty pageProp in pages.EnumerateObject())
                        {
                            if (pageProp.Name != "-1" && pageProp.Value.TryGetProperty("thumbnail", out JsonElement thumb))
                            {
                                if (thumb.TryGetProperty("source", out JsonElement source))
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
            Uri url = new($"https://api.opencritic.com/api/game/search?criteria={Uri.EscapeDataString(gameName)}");
            try
            {
                string? content = await FetchStringAsync(url, cancellationToken);
                if (content != null)
                {
                    using JsonDocument doc = JsonDocument.Parse(content);
                    if (doc.RootElement.GetArrayLength() > 0)
                    {
                        int gameId = doc.RootElement[0].GetProperty("id").GetInt32();
                        Uri gameUrl = new($"https://api.opencritic.com/api/game/{gameId}");
                        string? gameContent = await FetchStringAsync(gameUrl, cancellationToken);
                        if (gameContent != null)
                        {
                            using JsonDocument gameDoc = JsonDocument.Parse(gameContent);
                            JsonElement root = gameDoc.RootElement;

                            string? imgUrl = null;
                            if (root.TryGetProperty("images", out JsonElement images) && images.TryGetProperty("boxArt", out JsonElement boxArt) && boxArt.TryGetProperty("og", out JsonElement og))
                            {
                                imgUrl = $"https://img.opencritic.com/{og.GetString()}";
                            }
                            else if (root.TryGetProperty("bannerImageUrl", out JsonElement banner))
                            {
                                imgUrl = banner.GetString();
                                if (!string.IsNullOrEmpty(imgUrl) && !imgUrl.StartsWith("http", StringComparison.Ordinal))
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
            Uri url = new($"https://lutris.net/games/?q={Uri.EscapeDataString(gameName)}");
            try
            {
                string? html = await FetchStringAsync(url, cancellationToken);
                if (html != null)
                {
                    Match match = Regex.Match(html, @"<a href=""/games/[^/]+/"".*?<img.*?src=""([^""]+)""", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        string imgUrl = match.Groups[1].Value;
                        if (imgUrl.StartsWith('/'))
                        {
                            imgUrl = "https://lutris.net" + imgUrl;
                        }

                        return await DownloadImageAsync(imgUrl, coverPath, cancellationToken);
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Lutris cover failed for {GameName}", gameName); }
            return false;
        }

        public void Dispose()
        {
            // _httpClient is static, do not dispose here.
            GC.SuppressFinalize(this);
        }

        [GeneratedRegex("<[^<]+?>")]
        private static partial Regex MyRegex();
    }
}
