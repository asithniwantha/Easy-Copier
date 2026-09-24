using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Partial class implementation of <see cref="GameInfoDownloadService"/> handling PC system requirements lookup and formatting.
    /// </summary>
    public sealed partial class GameInfoDownloadService
    {
        /// <summary>
        /// Fetches minimum and recommended PC system requirements for a game from the Steam Store API.
        /// </summary>
        /// <param name="gameName">The title of the game to query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning a nested dictionary of requirements (minimum and recommended), or <c>null</c> if not found.</returns>
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

        /// <summary>
        /// Parses HTML string returned by Steam requirement properties to extract spec categories (CPU, GPU, RAM, Storage).
        /// </summary>
        /// <param name="html">The raw HTML system requirements string.</param>
        /// <returns>A dictionary containing spec component names and values.</returns>
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

        /// <summary>
        /// Formats parsed requirements into a clean, human-readable text document format.
        /// </summary>
        /// <param name="gameName">The title of the game.</param>
        /// <param name="requirements">The parsed dictionary of requirements.</param>
        /// <returns>A formatted string containing system requirements.</returns>
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

        /// <summary>
        /// Queries the Steam community app search endpoint to find the Steam App ID for a game title.
        /// </summary>
        /// <param name="gameName">The title of the game.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning the Steam App ID string if found; otherwise, <c>null</c>.</returns>
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
