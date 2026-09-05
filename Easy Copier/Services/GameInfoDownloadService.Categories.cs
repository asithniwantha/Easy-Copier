using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public sealed partial class GameInfoDownloadService
    {
        private static readonly Dictionary<string, GameCategory> KeywordCategoryMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "shoot", GameCategory.Shooter },
            { "fps", GameCategory.Shooter },
            { "gun", GameCategory.Shooter },
            { "race", GameCategory.Racing },
            { "racing", GameCategory.Racing },
            { "car", GameCategory.Racing },
            { "driving", GameCategory.Racing },
            { "rpg", GameCategory.RPG },
            { "role-playing", GameCategory.RPG },
            { "roleplaying", GameCategory.RPG },
            { "strategy", GameCategory.Strategy },
            { "rts", GameCategory.Strategy },
            { "tactics", GameCategory.Strategy },
            { "adventure", GameCategory.Adventure },
            { "sim", GameCategory.Simulation },
            { "simulation", GameCategory.Simulation },
            { "sport", GameCategory.Sports },
            { "football", GameCategory.Sports },
            { "basketball", GameCategory.Sports },
            { "soccer", GameCategory.Sports },
            { "puzzle", GameCategory.Puzzle },
            { "logic", GameCategory.Puzzle },
            { "horror", GameCategory.Horror },
            { "scary", GameCategory.Horror },
            { "zombie", GameCategory.Horror },
            { "platformer", GameCategory.Platformer },
            { "platform", GameCategory.Platformer },
            { "jump", GameCategory.Platformer }
        };

        private async Task DownloadCategoriesAsync(string gameName, string gameFolder, CancellationToken cancellationToken)
        {
            string catFile = Path.Combine(gameFolder, "categories.txt");
            if (File.Exists(catFile))
            {
                return;
            }

            try
            {
                List<GameCategory> categories = [];
                string? appId = await FetchSteamAppIdAsync(gameName, cancellationToken);

                if (appId != null)
                {
                    categories = await FetchSteamCategoriesAsync(appId, cancellationToken);
                }

                if (categories.Count == 0)
                {
                    categories = FallbackExtractCategories(gameName, gameFolder);
                }

                if (categories.Count == 0)
                {
                    categories.Add(GameCategory.Uncategorized);
                }

                categories = categories.Distinct().ToList();
                await File.WriteAllLinesAsync(catFile, categories.Select(c => c.ToString()), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download categories for {GameName}", gameName);
            }
        }

        private async Task<List<GameCategory>> FetchSteamCategoriesAsync(string appId, CancellationToken cancellationToken)
        {
            List<GameCategory> categories = [];
            Uri url = new($"https://store.steampowered.com/api/appdetails?appids={appId}");
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken);
                    using JsonDocument doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty(appId, out JsonElement appData) && appData.GetProperty("success").GetBoolean())
                    {
                        JsonElement data = appData.GetProperty("data");
                        if (data.TryGetProperty("genres", out JsonElement genresArray))
                        {
                            foreach (JsonElement genre in genresArray.EnumerateArray())
                            {
                                string? genreDesc = genre.GetProperty("description").GetString();
                                if (genreDesc != null)
                                {
                                    GameCategory mapped = MapSteamGenreToCore(genreDesc);
                                    if (mapped != GameCategory.Uncategorized)
                                    {
                                        categories.Add(mapped);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Steam Categories fetch failed for appId {AppId}", appId);
            }
            return categories;
        }

        private static GameCategory MapSteamGenreToCore(string steamGenre)
        {
            return steamGenre.ToUpperInvariant() switch
            {
                "ACTION" => GameCategory.Shooter,
                "RPG" => GameCategory.RPG,
                "STRATEGY" => GameCategory.Strategy,
                "ADVENTURE" => GameCategory.Adventure,
                "SIMULATION" => GameCategory.Simulation,
                "RACING" => GameCategory.Racing,
                "SPORTS" => GameCategory.Sports,
                var s when s.Contains("PUZZLE", StringComparison.OrdinalIgnoreCase) => GameCategory.Puzzle,
                var s when s.Contains("HORROR", StringComparison.OrdinalIgnoreCase) => GameCategory.Horror,
                var s when s.Contains("PLATFORMER", StringComparison.OrdinalIgnoreCase) => GameCategory.Platformer,
                var s when KeywordCategoryMapping.TryGetValue(s, out GameCategory mappedCategory) => mappedCategory,
                _ => GameCategory.Uncategorized
            };
        }

        private static List<GameCategory> FallbackExtractCategories(string gameName, string gameFolder)
        {
            List<GameCategory> categories = [];
            foreach (KeyValuePair<string, GameCategory> kvp in KeywordCategoryMapping)
            {
                if (gameName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    categories.Add(kvp.Value);
                }
            }
            string folderName = Path.GetFileName(gameFolder);
            if (!folderName.Equals(gameName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (KeyValuePair<string, GameCategory> kvp in KeywordCategoryMapping)
                {
                    if (folderName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        categories.Add(kvp.Value);
                    }
                }
            }
            return categories.Distinct().ToList();
        }
    }
}
