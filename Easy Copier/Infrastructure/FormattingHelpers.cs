using System;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides formatting and calculation helper utilities.
    /// </summary>
    public static class FormattingHelpers
    {
        /// <summary>
        /// Formats a byte size into a human-readable string with units (e.g., KB, MB, GB, TB).
        /// </summary>
        /// <param name="bytes">The size in bytes to format.</param>
        /// <returns>A formatted string representation of the byte size.</returns>
        public static string FormatBytes(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Calculates the price tier based on size in gigabytes and configured pricing tiers.
        /// </summary>
        /// <param name="bytes">The size of the game or file in bytes.</param>
        /// <param name="settings">The application settings containing pricing tier values.</param>
        /// <returns>The calculated price tier value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <c>null</c>.</exception>
        public static int CalculatePrice(long bytes, Models.AppSettings settings)
        {
            // Validate non-null settings parameter to satisfy Roslyn CA1062 and prevent NullReferenceException
            ArgumentNullException.ThrowIfNull(settings);

            // Convert total bytes to gigabytes for tier evaluation
            double gb = bytes / (1024.0 * 1024.0 * 1024.0);

            return gb switch
            {
                <= 5.0 => settings.PriceTier1,
                <= 10.0 => settings.PriceTier2,
                < 16.0 => settings.PriceTier3,
                _ => settings.PriceTier4
            };
        }

        /// <summary>
        /// Formats an empty state message for a library tab, reflecting whether a search query is active.
        /// </summary>
        /// <param name="entityPlural">The plural display name of the entity (e.g. "games", "apps", "films/TV series", "OS images").</param>
        /// <param name="folderType">The singular display name of the folder type (e.g. "game", "app", "film/TV", "OS image").</param>
        /// <param name="searchText">The active search query string.</param>
        /// <returns>A formatted message describing the empty or filtered state.</returns>
        public static string FormatEmptyLibraryMessage(string entityPlural, string folderType, string? searchText)
        {
            return string.IsNullOrWhiteSpace(searchText)
                ? $"No {entityPlural} found. Add a {folderType} folder in Settings and scan your library."
                : $"No {entityPlural} match \"{searchText}\".";
        }
    }
}
