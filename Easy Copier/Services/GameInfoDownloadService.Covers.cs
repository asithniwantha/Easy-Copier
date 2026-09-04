using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public sealed partial class GameInfoDownloadService
    {
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
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken);
                    using JsonDocument doc = JsonDocument.Parse(content);
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
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string html = await response.Content.ReadAsStringAsync(cancellationToken);
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
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string html = await response.Content.ReadAsStringAsync(cancellationToken);
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
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken);
                    using JsonDocument doc = JsonDocument.Parse(content);
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
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken);
                    using JsonDocument doc = JsonDocument.Parse(content);
                    if (doc.RootElement.GetArrayLength() > 0)
                    {
                        int gameId = doc.RootElement[0].GetProperty("id").GetInt32();
                        Uri gameUrl = new($"https://api.opencritic.com/api/game/{gameId}");
                        HttpResponseMessage gameResponse = await _httpClient.GetAsync(gameUrl, cancellationToken);
                        if (gameResponse.IsSuccessStatusCode)
                        {
                            string gameContent = await gameResponse.Content.ReadAsStringAsync(cancellationToken);
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
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string html = await response.Content.ReadAsStringAsync(cancellationToken);
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
    }
}
