using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Partial class implementation of <see cref="SettingsViewModel"/> providing settings persistence and initialization routines.
    /// </summary>
    public partial class SettingsViewModel
    {
        /// <summary>
        /// Asynchronously loads settings from persistent storage and validates configured source library folders.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadSettingsAsync()
        {
            AppSettings settings = await _settingsService.LoadSettingsAsync();
            LoadSettings(settings);

            // Auto-resolve Rufus executable path to the latest update in the same folder if available
            string latestRufusPath = RufusResolutionHelper.ResolveLatestRufusPath(RufusExecutablePath);
            if (!string.Equals(latestRufusPath, RufusExecutablePath, StringComparison.OrdinalIgnoreCase))
            {
                RufusExecutablePath = latestRufusPath;
                await SaveSettingsAsync();
            }

            IReadOnlyList<SourceFolder> validatedGameFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.GameSourceFolders);
            IReadOnlyList<SourceFolder> validatedAppFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.AppSourceFolders);
            int invalidCount = validatedGameFolders.Count(f => !f.IsValid) + validatedAppFolders.Count(f => !f.IsValid);

            if (invalidCount > 0)
            {
                StatusMessage = $"Warning: {invalidCount} source folder(s) are not accessible";
            }
        }

        /// <summary>
        /// Populates the view model properties and collections from the provided <see cref="AppSettings"/> instance.
        /// </summary>
        /// <param name="settings">The application settings instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <c>null</c>.</exception>
        public void LoadSettings(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            AutoScanOnStartup = settings.AutoScanOnStartup;
            StartOnLogon = settings.StartOnLogon;
            AutoDownloadUpdates = settings.AutoDownloadUpdates;
            PlayNotificationSounds = settings.PlayNotificationSounds;
            ShowDesktopNotifications = settings.ShowDesktopNotifications;
            RufusExecutablePath = settings.RufusExecutablePath ?? @"%USERPROFILE%\Downloads\Programs\rufus.exe";
            VideoFileExtensions = settings.VideoFileExtensions ?? ".mp4,.mkv,.avi";
            PriceTier1 = settings.PriceTier1.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PriceTier2 = settings.PriceTier2.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PriceTier3 = settings.PriceTier3.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PriceTier4 = settings.PriceTier4.ToString(System.Globalization.CultureInfo.InvariantCulture);

            GameSourceFolders.UpdateFrom(settings.GameSourceFolders);
            AppSourceFolders.UpdateFrom(settings.AppSourceFolders);
            TvAndFilmSourceFolders.UpdateFrom(settings.TvAndFilmSourceFolders ?? []);
            OsImageSourceFolders.UpdateFrom(settings.OsImageSourceFolders ?? []);
        }

        /// <summary>
        /// Constructs a new <see cref="AppSettings"/> snapshot from the current view model state.
        /// </summary>
        /// <returns>A new <see cref="AppSettings"/> populated with active view model field values.</returns>
        public AppSettings GetSettings()
        {
            return new AppSettings
            {
                AutoScanOnStartup = AutoScanOnStartup,
                StartOnLogon = StartOnLogon,
                AutoDownloadUpdates = AutoDownloadUpdates,
                PlayNotificationSounds = PlayNotificationSounds,
                ShowDesktopNotifications = ShowDesktopNotifications,
                RufusExecutablePath = RufusExecutablePath,
                VideoFileExtensions = VideoFileExtensions,
                PriceTier1 = int.TryParse(PriceTier1, out int p1) ? p1 : 100,
                PriceTier2 = int.TryParse(PriceTier2, out int p2) ? p2 : 200,
                PriceTier3 = int.TryParse(PriceTier3, out int p3) ? p3 : 300,
                PriceTier4 = int.TryParse(PriceTier4, out int p4) ? p4 : 400,
                GameSourceFolders = [.. GameSourceFolders],
                AppSourceFolders = [.. AppSourceFolders],
                TvAndFilmSourceFolders = [.. TvAndFilmSourceFolders],
                OsImageSourceFolders = [.. OsImageSourceFolders]
            };
        }
    }
}
