using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private readonly IFilePickerService _filePickerService;
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
        public partial bool PlayNotificationSounds { get; set; } = true;

        [ObservableProperty]
        public partial bool ShowDesktopNotifications { get; set; } = true;

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
        public ObservableCollection<string> OsImageSourceFolders { get; } = [];

        [ObservableProperty]
        public partial string RufusExecutablePath { get; set; } = @"%USERPROFILE%\Downloads\Programs\rufus.exe";

        [ObservableProperty]
        public partial string VideoFileExtensions { get; set; } = ".mp4,.mkv,.avi";

        [ObservableProperty]
        public partial string SelectedNavTag { get; set; } = "General";

        [ObservableProperty]
        public partial bool IsGeneralPanelVisible { get; set; } = true;

        [ObservableProperty]
        public partial bool IsGamesPanelVisible { get; set; }

        [ObservableProperty]
        public partial bool IsAppsPanelVisible { get; set; }

        [ObservableProperty]
        public partial bool IsFilmAndTvPanelVisible { get; set; }

        [ObservableProperty]
        public partial bool IsOsImagesPanelVisible { get; set; }

        [ObservableProperty]
        public partial bool IsLogsPanelVisible { get; set; }

        public event EventHandler? CloseRequested;

        private readonly Infrastructure.IProcessService _processService;

        public SettingsViewModel(
            ILogger<SettingsViewModel> logger,
            ISettingsService settingsService,
            IFolderPickerService folderPickerService,
            IFilePickerService filePickerService,
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
            _filePickerService = filePickerService;
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
        private async Task BrowseRufusExecutableAsync()
        {
            StatusMessage = "Opening file picker...";

            string? selectedFile = await _filePickerService.PickOpenFileAsync([".exe"]);

            if (!string.IsNullOrEmpty(selectedFile))
            {
                // Resolve latest version in that same folder if one exists
                string resolvedPath = RufusResolutionHelper.ResolveLatestRufusPath(selectedFile);
                RufusExecutablePath = resolvedPath;
                StatusMessage = $"Selected Rufus executable: {System.IO.Path.GetFileName(resolvedPath)}";
                await SaveSettingsAsync();
            }
            else
            {
                StatusMessage = "No file selected";
            }
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
        private async Task AddNewSourceFolderAsync(string folderType)
        {
            switch (folderType)
            {
                case "Game":
                    await AddSourceFolderAsync(GameSourceFolders, "game");
                    break;
                case "App":
                    await AddSourceFolderAsync(AppSourceFolders, "app");
                    break;
                case "TvAndFilm":
                    await AddSourceFolderAsync(TvAndFilmSourceFolders, "film/tv");
                    break;
                case "OsImage":
                    await AddSourceFolderAsync(OsImageSourceFolders, "OS image");
                    break;
            }
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
        private async Task RemoveSourceFolderByPathAsync(string folderPath)
        {
            ObservableCollection<string>[] allFolderCollections = [GameSourceFolders, AppSourceFolders, TvAndFilmSourceFolders, OsImageSourceFolders];

            foreach (var collection in allFolderCollections)
            {
                if (collection.Remove(folderPath))
                {
                    StatusMessage = $"Removed: {folderPath}";
                    await SaveSettingsAsync();
                    return; // Assuming a folder path is unique across all lists and we only need to remove it once
                }
            }
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

        [RelayCommand]
        private async Task SaveAndCloseAsync()
        {
            await SaveSettingsAsync();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        partial void OnSelectedNavTagChanged(string value)
        {
            IsGeneralPanelVisible = value == "General";
            IsGamesPanelVisible = value == "Games";
            IsAppsPanelVisible = value == "Apps";
            IsFilmAndTvPanelVisible = value == "FilmAndTv";
            IsOsImagesPanelVisible = value == "OsImages";
            IsLogsPanelVisible = value == "Logs";
        }
    }
}
