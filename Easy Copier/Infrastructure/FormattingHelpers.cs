namespace Easy_Copier.Infrastructure
{
    public static class FormattingHelpers
    {
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

        public static int CalculatePrice(long bytes, Models.AppSettings settings)
        {
            double gb = bytes / (1024.0 * 1024.0 * 1024.0);

            return gb <= 5.0
                ? settings.PriceTier1
                : gb <= 10.0 ? settings.PriceTier2 : gb < 16.0 ? settings.PriceTier3 : settings.PriceTier4;
        }
    }
}
