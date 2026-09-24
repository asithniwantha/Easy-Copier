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
    /// <summary>
    /// Partial class implementation of <see cref="GameInfoDownloadService"/> handling game category downloading and mapping.
    /// </summary>
    public sealed partial class GameInfoDownloadService
    {
        /// <summary>
        /// Mapping dictionary that associates common text keywords with <see cref="GameCategory"/> enum values.
        /// </summary>
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

        /// <summary>
        /// Downloads category information for a game and writes it to a <c>categories.txt</c> file within the game folder.
        /// </summary>
        /// <param name="gameName">The title of the game.</param>
        /// <param name="gameFolder">The local folder path for the game.</param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Fetches game genres from the Steam Web API for the specified app ID and maps them to <see cref="GameCategory"/> enum values.
        /// </summary>
        /// <param name="appId">The Steam App ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task returning a list of matched <see cref="GameCategory"/> values.</returns>
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

        /// <summary>
        /// Maps a Steam genre description string to a corresponding <see cref="GameCategory"/>.
        /// </summary>
        /// <param name="steamGenre">The genre string retrieved from Steam API.</param>
        /// <returns>The mapped <see cref="GameCategory"/> value.</returns>
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

        /// <summary>
        /// Extracts categories from the game name or folder name using keyword matching as a fallback when online APIs return no categories.
        /// </summary>
        /// <param name="gameName">The title of the game.</param>
        /// <param name="gameFolder">The local game folder path.</param>
        /// <returns>A list of inferred <see cref="GameCategory"/> values.</returns>
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
