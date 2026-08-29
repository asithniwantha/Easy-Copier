using System;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides formatting and calculation helper utilities.
    /// </summary>
    public static class FormattingHelpers
    {
        /// <summary>
        /// Formats a byte size into a human-readable string with units (e.g. KB, MB, GB).
        /// </summary>
        /// <param name="bytes">The size in bytes.</param>
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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
        public static int CalculatePrice(long bytes, Models.AppSettings settings)
        {
            // Validate non-null settings parameter to satisfy Roslyn CA1062 and prevent NullReferenceException
            ArgumentNullException.ThrowIfNull(settings);

            // Convert total bytes to gigabytes for tier evaluation
            double gb = bytes / (1024.0 * 1024.0 * 1024.0);

            return gb <= 5.0
                ? settings.PriceTier1
                : gb <= 10.0 ? settings.PriceTier2 : gb < 16.0 ? settings.PriceTier3 : settings.PriceTier4;
        }
    }
}
