using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [RelayCommand]
        private void OpenLogsFolder()
        {
            try
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logFolder = System.IO.Path.Combine(appDataFolder, "EasyCopier", "Logs");
                if (System.IO.Directory.Exists(logFolder))
                {
                    _processService.OpenInExplorer(logFolder);
                    _logger.LogInformation("Logs folder opened successfully.");
                }
                else
                {
                    _logger.LogWarning("Logs folder not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open logs folder.");
            }
        }

        private readonly ISettingsService _settingsService;
        private readonly IFolderPickerService _folderPickerService;
        private readonly ISourceLibraryService _sourceLibraryService;
        private readonly IGameInfoDownloadService _gameInfoDownloadService;
        private readonly IStartupService _startupService;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger<SettingsViewModel> _logger;
        private readonly IUpdateService _updateService;
        private readonly Infrastructure.IDialogService _dialogService;
        private readonly ILibraryScannerService _libraryScannerService;

        [ObservableProperty]
        public partial bool AutoScanOnStartup { get; set; } = true;

        [ObservableProperty]
        public partial bool StartOnLogon { get; set; } = false;

        [ObservableProperty]
        public partial bool AutoDownloadUpdates { get; set; } = true;

        [ObservableProperty]
        public partial string PriceTier1 { get; set; } = "100";

        [ObservableProperty]
        public partial string PriceTier2 { get; set; } = "200";

        [ObservableProperty]
        public partial string PriceTier3 { get; set; } = "300";

        [ObservableProperty]
        public partial string PriceTier4 { get; set; } = "400";

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;
        public ObservableCollection<string> GameSourceFolders { get; } = [];
        public ObservableCollection<string> AppSourceFolders { get; } = [];
        public ObservableCollection<string> TvAndFilmSourceFolders { get; } = [];

        [ObservableProperty]
        public partial string VideoFileExtensions { get; set; } = ".mp4,.mkv,.avi";

        private readonly Infrastructure.IProcessService _processService;

        public SettingsViewModel(
            ILogger<SettingsViewModel> logger,
            ISettingsService settingsService,
            IFolderPickerService folderPickerService,
            ISourceLibraryService sourceLibraryService,
            Infrastructure.IProcessService processService,
            IGameInfoDownloadService gameInfoDownloadService,
            IStartupService startupService,
            IDispatcherService dispatcherService,
            IUpdateService updateService,
            Infrastructure.IDialogService dialogService,
            ILibraryScannerService libraryScannerService)
        {
            _settingsService = settingsService;
            _folderPickerService = folderPickerService;
            _sourceLibraryService = sourceLibraryService;
            _processService = processService;
            _gameInfoDownloadService = gameInfoDownloadService;
            _startupService = startupService;
            _dispatcherService = dispatcherService;
            _logger = logger;
            _updateService = updateService;
            _dialogService = dialogService;
            _libraryScannerService = libraryScannerService;
        }

        [RelayCommand]
        private async Task FindDuplicatesAsync(CancellationToken cancellationToken)
        {
            StatusMessage = "Scanning libraries for duplicates...";

            try
            {
                Progress<string> progress = new(msg =>
                {
                    _ = _dispatcherService.TryEnqueue(() => StatusMessage = msg);
                });

                AppSettings settings = GetSettings();
                string report = await _libraryScannerService.FindDuplicatesReportAsync(settings, progress, cancellationToken);

                await _dialogService.ShowMessageDialogAsync("Duplicate Detection Report", report);
                StatusMessage = "Duplicate detection complete";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Duplicate scan canceled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan for duplicates.");
                StatusMessage = "Error scanning for duplicates.";
            }
        }

        [RelayCommand]
        private async Task CheckForUpdatesNowAsync()
        {
            StatusMessage = "Checking for updates...";
            _logger.LogInformation("Manual update check triggered from Settings.");

            try
            {
                bool hasUpdate = await _updateService.CheckForUpdatesAsync();

                if (hasUpdate)
                {
                    _logger.LogInformation("Manual update check found an update. Starting download...");
                    StatusMessage = "Downloading update...";

                    await _updateService.DownloadUpdateAsync();

                    _logger.LogInformation("Manual update download completed.");
                    StatusMessage = "Update ready! Please restart the app to apply.";

                    await _dialogService.ShowMessageDialogAsync(
                        "Update Ready",
                        "The update has been downloaded successfully. Please close and restart the application to apply the update.",
                        "OK");
                }
                else
                {
                    _logger.LogInformation("Manual update check found no updates.");
                    StatusMessage = "App is up to date.";

                    await _dialogService.ShowMessageDialogAsync(
                        "No Updates",
                        "The application is up to date.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual update check");
                StatusMessage = "Failed to check for updates.";

                await _dialogService.ShowMessageDialogAsync(
                    "Error",
                    "Failed to check for updates. Please try again later.",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task DownloadGameInfoAsync()
        {
            if (GameSourceFolders.Count == 0)
            {
                StatusMessage = "No game folders configured";
                return;
            }

            StatusMessage = "Downloading game covers and requirements...";

            try
            {
                Progress<string> progress = new(msg =>
                {
                    _ = _dispatcherService.TryEnqueue(() => StatusMessage = msg);
                });

                await _gameInfoDownloadService.DownloadGameInfoAsync(GameSourceFolders, progress, CancellationToken.None);

                StatusMessage = "Game info download complete";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error downloading game info: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddGameSourceFolderAsync()
        {
            await AddSourceFolderAsync(GameSourceFolders, "game");
        }

        [RelayCommand]
        private async Task AddAppSourceFolderAsync()
        {
            await AddSourceFolderAsync(AppSourceFolders, "app");
        }

        [RelayCommand]
        private async Task AddTvAndFilmSourceFolderAsync()
        {
            await AddSourceFolderAsync(TvAndFilmSourceFolders, "film/tv");
        }

        private async Task AddSourceFolderAsync(ObservableCollection<string> targetFolders, string categoryLabel)
        {
            StatusMessage = "Opening folder picker...";

            string? folderPath = await _folderPickerService.PickFolderAsync();

            if (!string.IsNullOrEmpty(folderPath))
            {
                if (!targetFolders.Contains(folderPath))
                {
                    targetFolders.Add(folderPath);
                    StatusMessage = $"Added {categoryLabel} folder: {folderPath}";
                    await SaveSettingsAsync();
                }
                else
                {
                    StatusMessage = "Folder already exists in the list";
                }
            }
            else
            {
                StatusMessage = "No folder selected";
            }
        }

        [RelayCommand]
        private async Task RemoveGameSourceFolderAsync(string folderPath)
        {
            await RemoveSourceFolderInternalAsync(GameSourceFolders, folderPath);
        }

        [RelayCommand]
        private async Task RemoveAppSourceFolderAsync(string folderPath)
        {
            await RemoveSourceFolderInternalAsync(AppSourceFolders, folderPath);
        }

        [RelayCommand]
        private async Task RemoveTvAndFilmSourceFolderAsync(string folderPath)
        {
            await RemoveSourceFolderInternalAsync(TvAndFilmSourceFolders, folderPath);
        }

        private async Task RemoveSourceFolderInternalAsync(ObservableCollection<string> targetFolders, string folderPath)
        {
            _ = targetFolders.Remove(folderPath);
            StatusMessage = $"Removed: {folderPath}";
            await SaveSettingsAsync();
        }

        [RelayCommand]
        private void OpenDataFolder()
        {
            string? folderPath = System.IO.Path.GetDirectoryName(_settingsService.GetSettingsFilePath());

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                StatusMessage = "Unable to resolve the data folder";
                return;
            }

            try
            {
                _processService.OpenInExplorer(folderPath);
                StatusMessage = $"Opened data folder: {folderPath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to open data folder: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            StatusMessage = "Saving settings...";

            AppSettings settings = GetSettings();
            await _settingsService.SaveSettingsAsync(settings);

            _startupService.UpdateStartOnLogon(settings.StartOnLogon);

            StatusMessage = "Settings saved";
        }

    }
}
