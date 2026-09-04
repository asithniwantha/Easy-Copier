using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    public partial class SettingsViewModel
    {
        public async Task LoadSettingsAsync()
        {
            AppSettings settings = await _settingsService.LoadSettingsAsync();
            LoadSettings(settings);

            IReadOnlyList<SourceFolder> validatedGameFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.GameSourceFolders);
            IReadOnlyList<SourceFolder> validatedAppFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.AppSourceFolders);
            int invalidCount = validatedGameFolders.Count(f => !f.IsValid) + validatedAppFolders.Count(f => !f.IsValid);

            if (invalidCount > 0)
            {
                StatusMessage = $"Warning: {invalidCount} source folder(s) are not accessible";
            }
        }

        public void LoadSettings(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            AutoScanOnStartup = settings.AutoScanOnStartup;
            StartOnLogon = settings.StartOnLogon;
            AutoDownloadUpdates = settings.AutoDownloadUpdates;
            VideoFileExtensions = settings.VideoFileExtensions ?? ".mp4,.mkv,.avi";
            PriceTier1 = settings.PriceTier1.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PriceTier2 = settings.PriceTier2.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PriceTier3 = settings.PriceTier3.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PriceTier4 = settings.PriceTier4.ToString(System.Globalization.CultureInfo.InvariantCulture);

            GameSourceFolders.UpdateFrom(settings.GameSourceFolders);
            AppSourceFolders.UpdateFrom(settings.AppSourceFolders);
            TvAndFilmSourceFolders.UpdateFrom(settings.TvAndFilmSourceFolders ?? []);
        }

        public AppSettings GetSettings()
        {
            return new AppSettings
            {
                AutoScanOnStartup = AutoScanOnStartup,
                StartOnLogon = StartOnLogon,
                AutoDownloadUpdates = AutoDownloadUpdates,
                VideoFileExtensions = VideoFileExtensions,
                PriceTier1 = int.TryParse(PriceTier1, out int p1) ? p1 : 100,
                PriceTier2 = int.TryParse(PriceTier2, out int p2) ? p2 : 200,
                PriceTier3 = int.TryParse(PriceTier3, out int p3) ? p3 : 300,
                PriceTier4 = int.TryParse(PriceTier4, out int p4) ? p4 : 400,
                GameSourceFolders = [.. GameSourceFolders],
                AppSourceFolders = [.. AppSourceFolders],
                TvAndFilmSourceFolders = [.. TvAndFilmSourceFolders]
            };
        }
    }
}
