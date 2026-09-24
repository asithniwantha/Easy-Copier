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
    /// <summary>
    /// ViewModel managing application settings, source folder configurations, price tiers, and updates.
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IFolderPickerService _folderPickerService;
        private readonly IFilePickerService _filePickerService;
        private readonly ISourceLibraryService _sourceLibraryService;
        private readonly IGameInfoDownloadService _gameInfoDownloadService;
        private readonly IStartupService _startupService;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger<SettingsViewModel> _logger;
        private readonly IUpdateService _updateService;
        private readonly IDialogService _dialogService;
        private readonly ILibraryScannerService _libraryScannerService;
        private readonly IProcessService _processService;

        /// <summary>
        /// Gets or sets a value indicating whether library scanning executes automatically on startup.
        /// </summary>
        [ObservableProperty]
        public partial bool AutoScanOnStartup { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the application launches automatically on Windows logon.
        /// </summary>
        [ObservableProperty]
        public partial bool StartOnLogon { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether application updates are downloaded automatically in the background.
        /// </summary>
        [ObservableProperty]
        public partial bool AutoDownloadUpdates { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether sound effects play upon completion or failure of transfer operations.
        /// </summary>
        [ObservableProperty]
        public partial bool PlayNotificationSounds { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether Windows toast notifications are displayed.
        /// </summary>
        [ObservableProperty]
        public partial bool ShowDesktopNotifications { get; set; } = true;

        /// <summary>
        /// Gets or sets the price string for tier 1 (&lt; 10 GB).
        /// </summary>
        [ObservableProperty]
        public partial string PriceTier1 { get; set; } = "100";

        /// <summary>
        /// Gets or sets the price string for tier 2 (&lt; 30 GB).
        /// </summary>
        [ObservableProperty]
        public partial string PriceTier2 { get; set; } = "200";

        /// <summary>
        /// Gets or sets the price string for tier 3 (&lt; 60 GB).
        /// </summary>
        [ObservableProperty]
        public partial string PriceTier3 { get; set; } = "300";

        /// <summary>
        /// Gets or sets the price string for tier 4 (&gt;= 60 GB).
        /// </summary>
        [ObservableProperty]
        public partial string PriceTier4 { get; set; } = "400";

        /// <summary>
        /// Gets or sets the status or feedback message displayed to the user in the Settings window.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets the collection of configured game source folder paths.
        /// </summary>
        public ObservableCollection<string> GameSourceFolders { get; } = [];

        /// <summary>
        /// Gets the collection of configured application source folder paths.
        /// </summary>
        public ObservableCollection<string> AppSourceFolders { get; } = [];

        /// <summary>
        /// Gets the collection of configured film and TV series source folder paths.
        /// </summary>
        public ObservableCollection<string> TvAndFilmSourceFolders { get; } = [];

        /// <summary>
        /// Gets the collection of configured OS image source folder paths.
        /// </summary>
        public ObservableCollection<string> OsImageSourceFolders { get; } = [];

        /// <summary>
        /// Gets or sets the configured path to the Rufus executable file.
        /// </summary>
        [ObservableProperty]
        public partial string RufusExecutablePath { get; set; } = @"%USERPROFILE%\Downloads\Programs\rufus.exe";

        /// <summary>
        /// Gets or sets the comma-separated list of recognized video file extensions.
        /// </summary>
        [ObservableProperty]
        public partial string VideoFileExtensions { get; set; } = ".mp4,.mkv,.avi";

        /// <summary>
        /// Gets or sets the tag string identifying the active navigation section in the Settings window.
        /// </summary>
        [ObservableProperty]
        public partial string SelectedNavTag { get; set; } = "General";

        /// <summary>
        /// Gets or sets a value indicating whether the General settings panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsGeneralPanelVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the Games settings panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsGamesPanelVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Apps settings panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsAppsPanelVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Film &amp; TV settings panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsFilmAndTvPanelVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the OS Images settings panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsOsImagesPanelVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Logs panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsLogsPanelVisible { get; set; }

        /// <summary>
        /// Event raised when the Settings window requests to be closed.
        /// </summary>
        public event EventHandler? CloseRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="settingsService">The settings persistence service.</param>
        /// <param name="folderPickerService">The folder picker dialog service.</param>
        /// <param name="filePickerService">The file picker dialog service.</param>
        /// <param name="sourceLibraryService">The source library service.</param>
        /// <param name="processService">The process service.</param>
        /// <param name="gameInfoDownloadService">The game info download service.</param>
        /// <param name="startupService">The startup registration service.</param>
        /// <param name="dispatcherService">The dispatcher service.</param>
        /// <param name="updateService">The update service.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="libraryScannerService">The library scanner service.</param>
        public SettingsViewModel(
            ILogger<SettingsViewModel> logger,
            ISettingsService settingsService,
            IFolderPickerService folderPickerService,
            IFilePickerService filePickerService,
            ISourceLibraryService sourceLibraryService,
            IProcessService processService,
            IGameInfoDownloadService gameInfoDownloadService,
            IStartupService startupService,
            IDispatcherService dispatcherService,
            IUpdateService updateService,
            IDialogService dialogService,
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

        [RelayCommand]
        private async Task BrowseRufusExecutableAsync()
        {
            StatusMessage = "Opening file picker...";

            string? selectedFile = await _filePickerService.PickOpenFileAsync([".exe"]);

            if (!string.IsNullOrEmpty(selectedFile))
            {
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
            Task addFolderTask = folderType switch
            {
                "Game" => AddSourceFolderAsync(GameSourceFolders, "game"),
                "App" => AddSourceFolderAsync(AppSourceFolders, "app"),
                "TvAndFilm" => AddSourceFolderAsync(TvAndFilmSourceFolders, "film/tv"),
                "OsImage" => AddSourceFolderAsync(OsImageSourceFolders, "OS image"),
                _ => Task.CompletedTask
            };

            await addFolderTask;
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

            foreach (ObservableCollection<string> collection in allFolderCollections)
            {
                if (collection.Remove(folderPath))
                {
                    StatusMessage = $"Removed: {folderPath}";
                    await SaveSettingsAsync();
                    return;
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
            IsGeneralPanelVisible = value is "General";
            IsGamesPanelVisible = value is "Games";
            IsAppsPanelVisible = value is "Apps";
            IsFilmAndTvPanelVisible = value is "FilmAndTv";
            IsOsImagesPanelVisible = value is "OsImages";
            IsLogsPanelVisible = value is "Logs";
        }

        partial void OnStartOnLogonChanged(bool value)
        {
            if (value)
            {
                string? executablePath = Environment.ProcessPath;
                string expectedFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyCopier");

                if (string.IsNullOrEmpty(executablePath) || !executablePath.StartsWith(expectedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Cannot enable start on logon: Application is not running from {ExpectedFolder}. Current path: {ExecutablePath}", expectedFolder, executablePath);
                    StartOnLogon = false;
                    StatusMessage = "Cannot enable startup. App is not in AppData\\Local\\EasyCopier.";
                }
            }
        }
    }
}
