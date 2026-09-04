using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Easy_Copier.Services
{
    public sealed partial class GameInfoDownloadService
    {
        private async Task<Dictionary<string, Dictionary<string, string>>?> FetchSteamRequirementsAsync(string gameName, CancellationToken cancellationToken)
        {
            string? appId = await FetchSteamAppIdAsync(gameName, cancellationToken);
            if (appId == null)
            {
                return null;
            }

            Uri url = new($"https://store.steampowered.com/api/appdetails?appids={appId}");
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
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
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                using JsonDocument doc = JsonDocument.Parse(content);
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
    }
}
