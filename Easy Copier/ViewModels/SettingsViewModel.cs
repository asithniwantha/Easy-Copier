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

        /// <summary>Gets or sets a value indicating whether library scanning automatically runs at application startup.</summary>
        [ObservableProperty]
        public partial bool AutoScanOnStartup { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether the application launches automatically at Windows logon.</summary>
        [ObservableProperty]
        public partial bool StartOnLogon { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether updates are downloaded automatically when detected.</summary>
        [ObservableProperty]
        public partial bool AutoDownloadUpdates { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether sound effects play for completion/failure events.</summary>
        [ObservableProperty]
        public partial bool PlayNotificationSounds { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether desktop toast notifications are enabled.</summary>
        [ObservableProperty]
        public partial bool ShowDesktopNotifications { get; set; } = true;

        /// <summary>Gets or sets the string representation of price tier 1 (&lt; 5 GB).</summary>
        [ObservableProperty]
        public partial string PriceTier1 { get; set; } = "100";

        /// <summary>Gets or sets the string representation of price tier 2 (5-10 GB).</summary>
        [ObservableProperty]
        public partial string PriceTier2 { get; set; } = "200";

        /// <summary>Gets or sets the string representation of price tier 3 (10-16 GB).</summary>
        [ObservableProperty]
        public partial string PriceTier3 { get; set; } = "300";

        /// <summary>Gets or sets the string representation of price tier 4 (&gt; 16 GB).</summary>
        [ObservableProperty]
        public partial string PriceTier4 { get; set; } = "400";

        /// <summary>Gets or sets the status message string displayed in the settings interface.</summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        /// <summary>Gets the collection of configured game source directory paths.</summary>
        public ObservableCollection<string> GameSourceFolders { get; } = [];

        /// <summary>Gets the collection of configured application source directory paths.</summary>
        public ObservableCollection<string> AppSourceFolders { get; } = [];

        /// <summary>Gets the collection of configured TV and film source directory paths.</summary>
        public ObservableCollection<string> TvAndFilmSourceFolders { get; } = [];

        /// <summary>Gets the collection of configured OS image source directory paths.</summary>
        public ObservableCollection<string> OsImageSourceFolders { get; } = [];

        /// <summary>Gets or sets the file path to the Rufus executable used for bootable USB creation.</summary>
        [ObservableProperty]
        public partial string RufusExecutablePath { get; set; } = @"%USERPROFILE%\Downloads\Programs\rufus.exe";

        /// <summary>Gets or sets the comma-separated list of video file extension filters.</summary>
        [ObservableProperty]
        public partial string VideoFileExtensions { get; set; } = ".mp4,.mkv,.avi";

        /// <summary>Gets or sets the active navigation tag string determining which settings sub-panel is displayed.</summary>
        [ObservableProperty]
        public partial string SelectedNavTag { get; set; } = "General";

        /// <summary>Gets or sets a value indicating whether the general settings panel is visible.</summary>
        [ObservableProperty]
        public partial bool IsGeneralPanelVisible { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether the games settings panel is visible.</summary>
        [ObservableProperty]
        public partial bool IsGamesPanelVisible { get; set; }

        /// <summary>Gets or sets a value indicating whether the apps settings panel is visible.</summary>
        [ObservableProperty]
        public partial bool IsAppsPanelVisible { get; set; }

        /// <summary>Gets or sets a value indicating whether the films &amp; TV settings panel is visible.</summary>
        [ObservableProperty]
        public partial bool IsFilmAndTvPanelVisible { get; set; }

        /// <summary>Gets or sets a value indicating whether the OS images settings panel is visible.</summary>
        [ObservableProperty]
        public partial bool IsOsImagesPanelVisible { get; set; }

        /// <summary>Gets or sets a value indicating whether the logs settings panel is visible.</summary>
        [ObservableProperty]
        public partial bool IsLogsPanelVisible { get; set; }

        /// <summary>Occurs when the view model requests the containing settings window or dialog to close.</summary>
        public event EventHandler? CloseRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class with required dependencies.
        /// </summary>
        /// <param name="logger">The logger instance for operational diagnostic output.</param>
        /// <param name="settingsService">The settings service for persisting and reading application settings.</param>
        /// <param name="folderPickerService">The folder picker service for selecting source directories.</param>
        /// <param name="filePickerService">The file picker service for selecting executable files.</param>
        /// <param name="sourceLibraryService">The source library service for validating configured source folders.</param>
        /// <param name="processService">The process service for opening external folders in File Explorer.</param>
        /// <param name="gameInfoDownloadService">The game info service for fetching cover art and requirements metadata.</param>
        /// <param name="startupService">The startup service for managing Windows logon auto-start registration.</param>
        /// <param name="dispatcherService">The dispatcher service for marshaling calls to the UI thread.</param>
        /// <param name="updateService">The update service for checking and downloading software updates.</param>
        /// <param name="dialogService">The dialog service for displaying modal message dialogs.</param>
        /// <param name="libraryScannerService">The library scanner service for duplicate content detection.</param>
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

        /// <summary>
        /// Opens the application logs folder in Windows File Explorer.
        /// </summary>
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

        /// <summary>
        /// Asynchronously prompts the user to select a Rufus executable file using a file picker.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Asynchronously scans active library folders and generates a duplicate item detection report.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous scan operation.</returns>
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

        /// <summary>
        /// Asynchronously checks for application updates and downloads any available update package.
        /// </summary>
        /// <returns>A task representing the asynchronous update check operation.</returns>
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

        /// <summary>
        /// Asynchronously downloads missing covers and system requirements for all games in the configured game source folders.
        /// </summary>
        /// <returns>A task representing the asynchronous download operation.</returns>
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

        /// <summary>
        /// Asynchronously prompts the user to select and append a new source folder for the specified library category type.
        /// </summary>
        /// <param name="folderType">The folder category string ("Game", "App", "TvAndFilm", or "OsImage").</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [RelayCommand]
        private async Task AddNewSourceFolderAsync(string folderType)
        {
            (ObservableCollection<string>? targetFolders, string categoryLabel) = folderType switch
            {
                "Game" => (GameSourceFolders, "game"),
                "App" => (AppSourceFolders, "app"),
                "TvAndFilm" => (TvAndFilmSourceFolders, "film/tv"),
                "OsImage" => (OsImageSourceFolders, "OS image"),
                _ => (null, string.Empty)
            };

            if (targetFolders != null)
            {
                await AddSourceFolderAsync(targetFolders, categoryLabel);
            }
        }

        /// <summary>
        /// Internal helper method to display a folder picker and add the chosen path to the specified collection.
        /// </summary>
        /// <param name="targetFolders">The collection to add the new folder path to.</param>
        /// <param name="categoryLabel">Display label describing the folder category.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Asynchronously removes a source folder from all configured source collections by its path.
        /// </summary>
        /// <param name="folderPath">The full path of the source directory to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Opens the application local AppData data folder in Windows File Explorer.
        /// </summary>
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

        /// <summary>
        /// Asynchronously persists current ViewModel field values to disk and updates logon auto-start status.
        /// </summary>
        /// <returns>A task representing the asynchronous save operation.</returns>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            StatusMessage = "Saving settings...";

            AppSettings settings = GetSettings();
            await _settingsService.SaveSettingsAsync(settings);

            _startupService.UpdateStartOnLogon(settings.StartOnLogon);

            StatusMessage = "Settings saved";
        }

        /// <summary>
        /// Asynchronously saves active settings to disk and triggers the <see cref="CloseRequested"/> event.
        /// </summary>
        /// <returns>A task representing the save and close operation.</returns>
        [RelayCommand]
        private async Task SaveAndCloseAsync()
        {
            await SaveSettingsAsync();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedNavTag"/> changes to toggle active panel visibility.
        /// </summary>
        /// <param name="value">The new navigation tag string.</param>
        partial void OnSelectedNavTagChanged(string value)
        {
            IsGeneralPanelVisible = value == "General";
            IsGamesPanelVisible = value == "Games";
            IsAppsPanelVisible = value == "Apps";
            IsFilmAndTvPanelVisible = value == "FilmAndTv";
            IsOsImagesPanelVisible = value == "OsImages";
            IsLogsPanelVisible = value == "Logs";
        }

        /// <summary>
        /// Partial notification handler invoked when <see cref="StartOnLogon"/> changes to validate application executable directory location.
        /// </summary>
        /// <param name="value">The new start on logon boolean flag.</param>
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
