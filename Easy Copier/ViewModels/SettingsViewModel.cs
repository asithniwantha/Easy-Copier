using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel managing application settings, source folder configurations, price tiers, and updates.
    /// </summary>
    public sealed partial class SettingsViewModel : ObservableObject, ICloseableViewModel, IDisposable
    {
        /// <summary>The settings service used to load and persist configuration.</summary>
        private readonly ISettingsService _settingsService;

        /// <summary>The folder picker service used to prompt the user for directory paths.</summary>
        private readonly IFolderPickerService _folderPickerService;

        /// <summary>The file picker service used to select custom executable paths.</summary>
        private readonly IFilePickerService _filePickerService;

        /// <summary>The source library service used to inspect and validate source folders.</summary>
        private readonly ISourceLibraryService _sourceLibraryService;

        /// <summary>The game info download service used to fetch cover art and genres.</summary>
        private readonly IGameInfoDownloadService _gameInfoDownloadService;

        /// <summary>The startup service configuring automatic launch with Windows.</summary>
        private readonly IStartupService _startupService;

        /// <summary>The dispatcher service for UI thread marshaling.</summary>
        private readonly IDispatcherService _dispatcherService;

        /// <summary>The logger instance for recording settings events and diagnostics.</summary>
        private readonly ILogger<SettingsViewModel> _logger;

        /// <summary>The update service providing application update checks.</summary>
        private readonly IUpdateService _updateService;

        /// <summary>The dialog service for displaying confirmation prompts.</summary>
        private readonly IDialogService _dialogService;

        /// <summary>The library scanner service for scanning folders and duplicates.</summary>
        private readonly ILibraryScannerService _libraryScannerService;

        /// <summary>The window service for launching windows and modal dialogs.</summary>
        private readonly IWindowService _windowService;

        /// <summary>The process service for interacting with external executables.</summary>
        private readonly IProcessService _processService;

        /// <summary>Array indexing all folder collections corresponding to category indices.</summary>
        private readonly ObservableCollection<string>[] _allFolderCollections;

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

        /// <summary>Gets or sets a value indicating whether closing the main window minimizes to the system tray when transfers are running.</summary>
        [ObservableProperty]
        public partial bool MinimizeToTrayOnClose { get; set; } = true;

        /// <summary>
        /// Gets or sets the data integrity verification strategy executed after file transfers.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VerificationModeIndex))]
        public partial CopyVerificationMode VerificationMode { get; set; } = CopyVerificationMode.QuickSizeAndCount;

        /// <summary>
        /// Gets or sets the zero-based index representing <see cref="VerificationMode"/> for XAML ComboBox selection.
        /// </summary>
        public int VerificationModeIndex
        {
            get => (int)VerificationMode;
            set
            {
                if (value >= 0 && value <= 3 && (CopyVerificationMode)value != VerificationMode)
                {
                    VerificationMode = (CopyVerificationMode)value;
                    OnPropertyChanged();
                }
            }
        }

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

        /// <summary>Gets or sets a value indicating whether game cover downloads should force re-download and overwrite existing covers.</summary>
        [ObservableProperty]
        public partial bool OverwriteExistingCovers { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether cover art and requirements downloading is actively running.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDownloadCovers))]
        public partial bool IsDownloadingCovers { get; set; } = false;

        /// <summary>Gets a value indicating whether cover downloading can currently be initiated.</summary>
        public bool CanDownloadCovers => !IsDownloadingCovers;

        /// <summary>Cancellation token source managing active game cover download tasks.</summary>
        private CancellationTokenSource? _downloadGameInfoCts;

        /// <summary>Gets the collection of configured game source directory paths.</summary>
        public ObservableCollection<string> GameSourceFolders { get; } = [];

        /// <summary>Gets a value indicating whether no game source folders are configured.</summary>
        public bool HasNoGameSourceFolders => GameSourceFolders.Count == 0;

        /// <summary>Gets the collection of configured application source directory paths.</summary>
        public ObservableCollection<string> AppSourceFolders { get; } = [];

        /// <summary>Gets a value indicating whether no app source folders are configured.</summary>
        public bool HasNoAppSourceFolders => AppSourceFolders.Count == 0;

        /// <summary>Gets the collection of configured TV and film source directory paths.</summary>
        public ObservableCollection<string> TvAndFilmSourceFolders { get; } = [];

        /// <summary>Gets a value indicating whether no film/TV source folders are configured.</summary>
        public bool HasNoTvAndFilmSourceFolders => TvAndFilmSourceFolders.Count == 0;

        /// <summary>Gets the collection of configured OS image source directory paths.</summary>
        public ObservableCollection<string> OsImageSourceFolders { get; } = [];

        /// <summary>Gets a value indicating whether no OS image source folders are configured.</summary>
        public bool HasNoOsImageSourceFolders => OsImageSourceFolders.Count == 0;

        /// <summary>Gets or sets the file path to the Rufus executable used for bootable USB creation.</summary>
        [ObservableProperty]
        public partial string RufusExecutablePath { get; set; } = @"%USERPROFILE%\Downloads\Programs\rufus.exe";

        /// <summary>Gets or sets the comma-separated list of video file extension filters.</summary>
        [ObservableProperty]
        public partial string VideoFileExtensions { get; set; } = ".mp4,.mkv,.avi";

        /// <summary>Gets or sets the comma-separated list of app file extension filters.</summary>
        [ObservableProperty]
        public partial string AppFileExtensions { get; set; } = ".exe,.msi,.rar,.zip";

        /// <summary>Gets or sets the active navigation tag string determining which settings sub-panel is displayed.</summary>
        [ObservableProperty]
        public partial string SelectedNavTag { get; set; } = "General";

        /// <summary>Occurs when the view model requests the containing settings window or dialog to close.</summary>
        public event EventHandler? CloseRequested;

        /// <inheritdoc />
        public void RequestClose()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

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
        /// <param name="windowService">The window service for activating child and modal dialog windows.</param>
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
            ILibraryScannerService libraryScannerService,
            IWindowService windowService)
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
            _windowService = windowService;

            _allFolderCollections = [GameSourceFolders, AppSourceFolders, TvAndFilmSourceFolders, OsImageSourceFolders];

            // Notify UI when folder collections change so empty states update dynamically
            GameSourceFolders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoGameSourceFolders));
            AppSourceFolders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoAppSourceFolders));
            TvAndFilmSourceFolders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoTvAndFilmSourceFolders));
            OsImageSourceFolders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoOsImageSourceFolders));
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
        /// Opens the interactive Duplicate Library Manager dialog to scan and manage duplicate items across libraries.
        /// </summary>
        [RelayCommand]
        private void FindDuplicates()
        {
            _windowService.ShowDuplicateManagerWindow();
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

            if (_downloadGameInfoCts != null)
            {
                await _downloadGameInfoCts.CancelAsync();
                _downloadGameInfoCts.Dispose();
            }
            _downloadGameInfoCts = new CancellationTokenSource();
            CancellationToken token = _downloadGameInfoCts.Token;

            IsDownloadingCovers = true;
            StatusMessage = "Downloading game covers and requirements...";

            try
            {
                Progress<string> progress = new(msg =>
                {
                    _ = _dispatcherService.TryEnqueue(() => StatusMessage = msg);
                });

                await _gameInfoDownloadService.DownloadGameInfoAsync(GameSourceFolders, progress, OverwriteExistingCovers, token);

                StatusMessage = "Game info download complete";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Game info download canceled.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error downloading game info: {ex.Message}";
            }
            finally
            {
                IsDownloadingCovers = false;
                _downloadGameInfoCts?.Dispose();
                _downloadGameInfoCts = null;
            }
        }

        /// <summary>
        /// Cancels an in-progress bulk cover art and requirements download operation.
        /// </summary>
        [RelayCommand]
        private async Task CancelDownloadGameInfoAsync()
        {
            if (_downloadGameInfoCts != null && !_downloadGameInfoCts.IsCancellationRequested)
            {
                StatusMessage = "Canceling download...";
                await _downloadGameInfoCts.CancelAsync();
            }
        }

        /// <summary>
        /// Disposes managed resources and active cancellation tokens.
        /// </summary>
        public void Dispose()
        {
            _downloadGameInfoCts?.Dispose();
            _downloadGameInfoCts = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously prompts the user to select and append a new source folder for the specified library category type.
        /// </summary>
        /// <param name="category">The folder category enum value.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddNewSourceFolderAsync(SourceFolderCategory category)
        {
            (ObservableCollection<string>? targetFolders, string categoryLabel) = category switch
            {
                SourceFolderCategory.Game => (GameSourceFolders, "game"),
                SourceFolderCategory.App => (AppSourceFolders, "app"),
                SourceFolderCategory.TvAndFilm => (TvAndFilmSourceFolders, "film/tv"),
                SourceFolderCategory.OsImage => (OsImageSourceFolders, "OS image"),
                _ => (null, string.Empty)
            };

            if (targetFolders != null)
            {
                await AddSourceFolderAsync(targetFolders, categoryLabel);
            }
        }

        /// <summary>
        /// Command handler accepting a category string (e.g. from XAML bindings) and delegating to <see cref="AddNewSourceFolderAsync(SourceFolderCategory)"/>.
        /// </summary>
        /// <param name="folderType">The folder category string ("Game", "App", "TvAndFilm", or "OsImage").</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [RelayCommand]
        private async Task AddNewSourceFolderAsync(string folderType)
        {
            if (Enum.TryParse(folderType, true, out SourceFolderCategory category))
            {
                await AddNewSourceFolderAsync(category);
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
                if (!System.IO.Directory.Exists(folderPath))
                {
                    StatusMessage = $"Selected folder does not exist or is not accessible: {folderPath}";
                    await _dialogService.ShowMessageDialogAsync("Folder Inaccessible", $"The selected directory '{folderPath}' cannot be accessed or does not exist.");
                    return;
                }

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
            foreach (ObservableCollection<string> collection in _allFolderCollections)
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
            StatusMessage = "Validating folders...";

            AppSettings settings = GetSettings();

            // Validate configured source folders before saving to alert user of inaccessible or detached drives
            IReadOnlyList<SourceFolder> validatedGame = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.GameSourceFolders);
            IReadOnlyList<SourceFolder> validatedApp = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.AppSourceFolders);
            IReadOnlyList<SourceFolder> validatedTv = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.TvAndFilmSourceFolders ?? []);
            IReadOnlyList<SourceFolder> validatedOs = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.OsImageSourceFolders ?? []);

            List<string> inaccessibleFolders = validatedGame.Concat(validatedApp).Concat(validatedTv).Concat(validatedOs)
                .Where(f => !f.IsValid)
                .Select(f => f.FolderPath)
                .Distinct()
                .ToList();

            if (inaccessibleFolders.Count > 0)
            {
                string folderList = string.Join("\n", inaccessibleFolders.Select(p => $"• {p}"));
                string prompt = $"The following {inaccessibleFolders.Count} configured source folder(s) are currently inaccessible or do not exist:\n\n{folderList}\n\nDo you want to save anyway?";
                bool proceed = await _dialogService.ShowConfirmationDialogAsync("Inaccessible Source Folders", prompt, "Save Anyway", "Cancel");
                if (!proceed)
                {
                    StatusMessage = "Save cancelled: resolve inaccessible source folders.";
                    return;
                }
            }

            StatusMessage = "Saving settings...";

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
        }

        /// <summary>
        /// Guard flag preventing re-entrant property changed notifications when resetting <see cref="StartOnLogon"/> upon validation failure.
        /// </summary>
        private bool _isUpdatingStartOnLogon;

        /// <summary>
        /// Partial notification handler invoked when <see cref="StartOnLogon"/> changes to validate application executable directory location.
        /// </summary>
        /// <param name="value">The new start on logon boolean flag.</param>
        partial void OnStartOnLogonChanged(bool value)
        {
            if (_isUpdatingStartOnLogon)
            {
                return;
            }

            if (value)
            {
                string? executablePath = Environment.ProcessPath;
                string expectedFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyCopier");

                if (string.IsNullOrEmpty(executablePath) || !executablePath.StartsWith(expectedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Cannot enable start on logon: Application is not running from {ExpectedFolder}. Current path: {ExecutablePath}", expectedFolder, executablePath);

                    _isUpdatingStartOnLogon = true;
                    try
                    {
                        StartOnLogon = false;
                    }
                    finally
                    {
                        _isUpdatingStartOnLogon = false;
                    }

                    StatusMessage = "Cannot enable startup. App is not in AppData\\Local\\EasyCopier.";
                }
            }
        }
    }
}
