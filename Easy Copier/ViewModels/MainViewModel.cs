using CommunityToolkit.Mvvm.ComponentModel;
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
    /// <summary>
    /// Core ViewModel backing the main application page, coordinating library navigation, media scanning, transfer queueing, and drive operations.
    /// </summary>
    public sealed partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly ISettingsService _settingsService;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly ILibraryScannerService _libraryScannerService;
        private readonly IDriveDiscoveryService _driveDiscoveryService;
        private readonly IDriveValidationService _driveValidationService;
        private readonly IFileTransferService _fileTransferService;
        private readonly ITransferQueueService _transferQueueService;
        private readonly IWindowService _windowService;
        private readonly IProcessService _processService;
        private readonly IDispatcherService _dispatcherService;
        private readonly IUpdateService _updateService;
        private readonly ISourceLibraryService _sourceLibraryService;
        private readonly IDialogService _dialogService;
        private readonly ILibraryFilterService _libraryFilterService;
        private readonly IRufusService _rufusService;
        private readonly IGameRequirementsService _gameRequirementsService;
        private readonly IFileSystemService _fileSystemService;
        private readonly Func<GameDetailsViewModel> _gameDetailsViewModelFactory;
        private CancellationTokenSource? _scanCancellationTokenSource;
        private CancellationTokenSource? _validationCancellationTokenSource;
        private CancellationTokenSource? _notificationCancellationTokenSource;
        private List<GameEntry> _selectedGames = [];
        private AppSettings? _cachedSettings;
        private System.Threading.Timer? _updateCheckTimer;
        private bool _isCheckingForUpdates;
        private int _isDisposed;

        /// <summary>
        /// Occurs when a request to clear all active item selections across library tab views is issued.
        /// </summary>
        public event EventHandler? ClearSelectionRequested;

        /// <summary>
        /// Occurs when a new transfer job item has been enqueued in the transfer queue.
        /// </summary>
        public event EventHandler? ItemQueued;

        /// <summary>
        /// Gets or sets a value indicating whether the global in-app notification bar is currently visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsGlobalNotificationVisible { get; set; }

        /// <summary>
        /// Gets or sets the severity level of the active global in-app notification bar.
        /// </summary>
        [ObservableProperty]
        public partial ValidationSeverity GlobalNotificationSeverity { get; set; } = ValidationSeverity.Info;

        /// <summary>
        /// Gets or sets the title text displayed in the global in-app notification bar.
        /// </summary>
        [ObservableProperty]
        public partial string GlobalNotificationTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the body message text displayed in the global in-app notification bar.
        /// </summary>
        [ObservableProperty]
        public partial string GlobalNotificationMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the initial application state loading routine is active.
        /// </summary>
        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a new software update is available for download.
        /// </summary>
        [ObservableProperty]
        public partial bool IsUpdateAvailable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a downloaded software update is ready to be installed.
        /// </summary>
        [ObservableProperty]
        public partial bool IsUpdateReadyToInstall { get; set; }

        /// <summary>
        /// Gets or sets the update status message string presented to the user.
        /// </summary>
        [ObservableProperty]
        public partial string UpdateMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether library directory scanning is actively running.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGamesEmpty))]
        [NotifyPropertyChangedFor(nameof(IsAppsEmpty))]
        [NotifyPropertyChangedFor(nameof(IsTvAndFilmsEmpty))]
        [NotifyPropertyChangedFor(nameof(IsOsImagesEmpty))]
        public partial bool IsScanning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a file transfer operation is actively running in the transfer queue.
        /// </summary>
        [ObservableProperty]
        public partial bool IsTransferring { get; set; }

        /// <summary>
        /// Gets or sets the current status message displayed in the application status bar.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = "Ready";

        /// <summary>
        /// Gets or sets the currently selected target removable drive.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedDrive))]
        [NotifyPropertyChangedFor(nameof(DriveSpaceSummary))]
        [NotifyPropertyChangedFor(nameof(DriveDetailsSummary))]
        public partial RemovableDrive? SelectedDrive { get; set; }

        /// <summary>
        /// Gets or sets the count of currently selected library items.
        /// </summary>
        [ObservableProperty]
        public partial int SelectedGamesCount { get; set; }
        /// <summary>
        /// Gets a value indicating whether any library items are currently selected.
        /// </summary>
        public bool HasSelectedGames => SelectedGamesCount > 0;

        /// <summary>
        /// Gets or sets the aggregate byte size of currently selected library items.
        /// </summary>
        [ObservableProperty]
        public partial long SelectedGamesTotalBytes { get; set; }

        /// <summary>
        /// Gets or sets the total calculated monetary price of currently selected library items.
        /// </summary>
        [ObservableProperty]
        public partial int SelectedGamesTotalPrice { get; set; }

        /// <summary>
        /// Gets or sets the search filter text query.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EmptyGamesMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyAppsMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyTvAndFilmsMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyOsImagesMessage))]
        public partial string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the currently selected category filter option.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EmptyGamesMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyAppsMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyTvAndFilmsMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyOsImagesMessage))]
        public partial GameCategory SelectedCategory { get; set; } = GameCategory.All;

        /// <summary>
        /// Gets the list of available game category options for filtering.
        /// </summary>
        public IReadOnlyList<GameCategory> AvailableCategories { get; } = Enum.GetValues<GameCategory>();

        /// <summary>
        /// Gets or sets the active sort criterion for OS image library entries.
        /// </summary>
        [ObservableProperty]
        public partial OsImageSortOption SelectedOsImageSortOption { get; set; } = OsImageSortOption.DateCreated;

        /// <summary>
        /// Gets or sets a value indicating whether OS image sorting is in ascending order.
        /// </summary>
        [ObservableProperty]
        public partial bool IsOsImageSortAscending { get; set; } = false;

        /// <summary>
        /// Gets the list of available sorting options for OS images.
        /// </summary>
        public IReadOnlyList<OsImageSortOption> AvailableOsImageSortOptions { get; } = Enum.GetValues<OsImageSortOption>();

        private readonly List<GameEntry> _allGames = [];
        private readonly List<GameEntry> _allApps = [];
        private readonly List<GameEntry> _allTvAndFilms = [];
        private readonly List<GameEntry> _allOsImages = [];

        /// <summary>
        /// Gets a value indicating whether the games collection is empty and not currently scanning.
        /// </summary>
        public bool IsGamesEmpty => !IsScanning && Games.Count == 0;

        /// <summary>
        /// Gets a value indicating whether the applications collection is empty and not currently scanning.
        /// </summary>
        public bool IsAppsEmpty => !IsScanning && Apps.Count == 0;

        /// <summary>
        /// Gets a value indicating whether the TV and films collection is empty and not currently scanning.
        /// </summary>
        public bool IsTvAndFilmsEmpty => !IsScanning && TvAndFilms.Count == 0;

        /// <summary>
        /// Gets a value indicating whether the OS images collection is empty and not currently scanning.
        /// </summary>
        public bool IsOsImagesEmpty => !IsScanning && OsImages.Count == 0;

        /// <summary>
        /// Gets the empty view status message for the games tab.
        /// </summary>
        public string EmptyGamesMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No games found. Add a game folder in Settings and scan your library."
            : $"No games match \"{SearchText}\".";

        /// <summary>
        /// Gets the empty view status message for the applications tab.
        /// </summary>
        public string EmptyAppsMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No apps found. Add an app folder in Settings and scan your library."
            : $"No apps match \"{SearchText}\".";

        /// <summary>
        /// Gets the empty view status message for the TV and films tab.
        /// </summary>
        public string EmptyTvAndFilmsMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No films/TV series found. Add a folder in Settings and scan your library."
            : $"No films/TV series match \"{SearchText}\".";

        /// <summary>
        /// Gets the empty view status message for the OS images tab.
        /// </summary>
        public string EmptyOsImagesMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No OS images found. Add an OS image folder in Settings and scan your library."
            : $"No OS images match \"{SearchText}\".";

        /// <summary>
        /// Gets a value indicating whether a target removable drive is currently selected.
        /// </summary>
        public bool HasSelectedDrive => SelectedDrive != null;

        /// <summary>
        /// Gets a summary string displaying free and total storage space of the selected drive.
        /// </summary>
        public string DriveSpaceSummary => SelectedDrive == null
            ? string.Empty
            : $"{FormattingHelpers.FormatBytes(SelectedDrive.FreeBytes)} free of {FormattingHelpers.FormatBytes(SelectedDrive.TotalBytes)}";

        /// <summary>
        /// Gets a formatted string describing the drive letter, brand, and file system of the selected drive.
        /// </summary>
        public string DriveDetailsSummary => SelectedDrive == null
            ? string.Empty
            : $"{SelectedDrive.DriveLetter} \u2022 {SelectedDrive.Brand} \u2022 {SelectedDrive.FileSystem}";

        /// <summary>
        /// Gets a formatted summary string describing item count, total byte size, and price for selected items.
        /// </summary>
        public string SelectionSummary => SelectedGamesCount == 0
            ? "No items selected"
            : $"{SelectedGamesCount} item(s) selected \u2022 {FormattingHelpers.FormatBytes(SelectedGamesTotalBytes)} \u2022 Rs. {SelectedGamesTotalPrice}";

        /// <summary>
        /// Gets or sets the active tab index in the main view Pivot control.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOsImagesTabActive))]
        public partial int CurrentTabIndex { get; set; }

        /// <summary>
        /// Gets a value indicating whether the active tab is the OS Images tab (index 3).
        /// </summary>
        public bool IsOsImagesTabActive => CurrentTabIndex == 3;

        /// <summary>
        /// Gets the observable collection of game items in the active library view.
        /// </summary>
        public ObservableCollection<GameEntry> Games { get; } = [];

        /// <summary>
        /// Gets the observable collection of application items in the active library view.
        /// </summary>
        public ObservableCollection<GameEntry> Apps { get; } = [];

        /// <summary>
        /// Gets the observable collection of film and TV series items in the active library view.
        /// </summary>
        public ObservableCollection<GameEntry> TvAndFilms { get; } = [];

        /// <summary>
        /// Gets the observable collection of OS image items in the active library view.
        /// </summary>
        public ObservableCollection<GameEntry> OsImages { get; } = [];

        /// <summary>
        /// Gets the observable collection of discovered removable drives available for transfer operations.
        /// </summary>
        public ObservableCollection<RemovableDrive> AvailableDrives { get; } = [];

        /// <summary>
        /// Gets the observable collection of drive validation messages for active transfers.
        /// </summary>
        public ObservableCollection<ValidationResult> ValidationMessages { get; } = [];

        /// <summary>
        /// Gets the transfer queue observable collection tracking queued and active transfer batch jobs.
        /// </summary>
        public ObservableCollection<TransferQueueItem> TransferQueue => _transferQueueService.QueueItems;

        /// <summary>
        /// Gets the child SmartAdder overlay calculator ViewModel instance.
        /// </summary>
        public SmartAdderViewModel SmartAdderViewModel { get; }

        /// <summary>
        /// Gets the child Games tab ViewModel instance.
        /// </summary>
        public GamesTabViewModel GamesTabViewModel { get; }

        /// <summary>
        /// Gets the child Apps tab ViewModel instance.
        /// </summary>
        public AppsTabViewModel AppsTabViewModel { get; }

        /// <summary>
        /// Gets the child TV and Films tab ViewModel instance.
        /// </summary>
        public TvAndFilmsTabViewModel TvAndFilmsTabViewModel { get; }

        /// <summary>
        /// Gets the child OS Images tab ViewModel instance.
        /// </summary>
        public OsImagesTabViewModel OsImagesTabViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class with required infrastructure and domain services.
        /// </summary>
        /// <param name="logger">The logger instance for operational diagnostic output.</param>
        /// <param name="updateService">The update service for checking and downloading updates.</param>
        /// <param name="settingsService">The settings service for loading and persisting user configuration.</param>
        /// <param name="libraryCacheService">The library cache service for snapshot storage and validation.</param>
        /// <param name="libraryScannerService">The library scanner service for folder indexing.</param>
        /// <param name="driveDiscoveryService">The drive discovery service for detecting removable media.</param>
        /// <param name="driveValidationService">The drive validation service for checking destination space and file system constraints.</param>
        /// <param name="fileTransferService">The file transfer service executing underlying disk I/O operations.</param>
        /// <param name="transferQueueService">The transfer queue service managing batch execution and notifications.</param>
        /// <param name="windowService">The window service for launching dialogs and windows.</param>
        /// <param name="processService">The process service for external process interactions.</param>
        /// <param name="dispatcherService">The dispatcher service for UI thread marshaling.</param>
        /// <param name="sourceLibraryService">The source library service for validating source folders.</param>
        /// <param name="dialogService">The dialog service for presenting conflict dialogs.</param>
        /// <param name="libraryFilterService">The library filter service for query filtering and sorting.</param>
        /// <param name="rufusService">The Rufus service for ISO imaging operations.</param>
        /// <param name="gameRequirementsService">The game system requirements service.</param>
        /// <param name="fileSystemService">The file system service for path checking.</param>
        /// <param name="smartAdderViewModel">The SmartAdder calculator view model.</param>
        /// <param name="gameDetailsViewModelFactory">Factory delegate for instantiating transient <see cref="GameDetailsViewModel"/> instances.</param>
        public MainViewModel(
            ILogger<MainViewModel> logger,
            IUpdateService updateService,
            ISettingsService settingsService,
            ILibraryCacheService libraryCacheService,
            ILibraryScannerService libraryScannerService,
            IDriveDiscoveryService driveDiscoveryService,
            IDriveValidationService driveValidationService,
            IFileTransferService fileTransferService,
            ITransferQueueService transferQueueService,
            IWindowService windowService,
            IProcessService processService,
            IDispatcherService dispatcherService,
            ISourceLibraryService sourceLibraryService,
            IDialogService dialogService,
            ILibraryFilterService libraryFilterService,
            IRufusService rufusService,
            IGameRequirementsService gameRequirementsService,
            IFileSystemService fileSystemService,
            SmartAdderViewModel smartAdderViewModel,
            Func<GameDetailsViewModel> gameDetailsViewModelFactory)
        {
            _logger = logger;
            _settingsService = settingsService;
            _libraryCacheService = libraryCacheService;
            _libraryScannerService = libraryScannerService;
            _driveDiscoveryService = driveDiscoveryService;
            _driveValidationService = driveValidationService;
            _fileTransferService = fileTransferService;
            _transferQueueService = transferQueueService;
            _windowService = windowService;
            _processService = processService;
            _dispatcherService = dispatcherService;
            _updateService = updateService;
            _sourceLibraryService = sourceLibraryService;
            _dialogService = dialogService;
            _libraryFilterService = libraryFilterService;
            _rufusService = rufusService;
            _gameRequirementsService = gameRequirementsService;
            _fileSystemService = fileSystemService;
            SmartAdderViewModel = smartAdderViewModel;
            _gameDetailsViewModelFactory = gameDetailsViewModelFactory;
            GamesTabViewModel = new GamesTabViewModel(this);
            AppsTabViewModel = new AppsTabViewModel(this);
            TvAndFilmsTabViewModel = new TvAndFilmsTabViewModel(this);
            OsImagesTabViewModel = new OsImagesTabViewModel(this);

            _driveDiscoveryService.DrivesChanged += OnDrivesChanged;
            _transferQueueService.ItemCompleted += OnQueueItemCompleted;
            _transferQueueService.BatchCompleted += OnBatchCompleted;
        }

        private bool IsDisposed => Volatile.Read(ref _isDisposed) != 0;

        private void OnDrivesChanged(object? sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (!_dispatcherService.HasThreadAccess)
            {
                if (!_dispatcherService.TryEnqueue(RefreshDrivesAfterDriveChangeAsync))
                {
                    _logger.LogDebug("Skipped drive refresh because the dispatcher is shutting down.");
                }

                return;
            }

            _ = RefreshDrivesAfterDriveChangeAsync();
        }

        private async Task RefreshDrivesAfterDriveChangeAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            await RefreshDrivesAsync();
        }

        private void OnUpdateCheckTimerTick(object? state)
        {
            if (!TryEnqueueIfActive(CheckForUpdatesIfActiveAsync))
            {
                _logger.LogDebug("Skipped update check because the dispatcher is shutting down.");
            }
        }

        private bool TryEnqueueIfActive(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            return !IsDisposed && _dispatcherService.TryEnqueue(() =>
            {
                if (IsDisposed)
                {
                    return;
                }

                action();
            });
        }

        private bool TryEnqueueIfActive(Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            return !IsDisposed && _dispatcherService.TryEnqueue((Func<Task>)(async () =>
            {
                if (IsDisposed)
                {
                    return;
                }

                await action();
            }));
        }

        private async Task CheckForUpdatesIfActiveAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            await CheckForUpdatesBackgroundAsync();
        }

        /// <summary>
        /// Creates a new transient instance of <see cref="GameDetailsViewModel"/> using the registered factory.
        /// </summary>
        /// <returns>A new <see cref="GameDetailsViewModel"/> instance.</returns>
        public GameDetailsViewModel CreateGameDetailsViewModel()
        {
            return _gameDetailsViewModelFactory();
        }

        /// <summary>
        /// Asynchronously initializes settings, drive monitoring, and library contents from cache or disk scan.
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        public async Task InitializeAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            _ = CheckForUpdatesBackgroundAsync();

            // Start periodic update checks every 4 hours.
            _updateCheckTimer ??= new System.Threading.Timer(
                OnUpdateCheckTimerTick,
                null,
                TimeSpan.FromHours(4),
                TimeSpan.FromHours(4));

            try
            {
                IsLoading = true;
                StatusMessage = "Loading settings...";

                AppSettings settings = await _settingsService.LoadSettingsAsync();
                _cachedSettings = settings;

                _driveDiscoveryService.StartWatching();
                await RefreshDrivesAsync();

                if (settings.GameSourceFolders.Count == 0 && settings.AppSourceFolders.Count == 0 && (settings.TvAndFilmSourceFolders == null || settings.TvAndFilmSourceFolders.Count == 0) && (settings.OsImageSourceFolders == null || settings.OsImageSourceFolders.Count == 0))
                {
                    StatusMessage = "Ready - No source folders configured";
                    return;
                }

                LibraryCacheSnapshot? cache = await _libraryCacheService.LoadCacheAsync();

                if (cache != null)
                {
                    _allGames.Clear();
                    _allGames.AddRange(cache.Games);

                    _allApps.Clear();
                    _allApps.AddRange(cache.Apps);

                    _allTvAndFilms.Clear();
                    _allTvAndFilms.AddRange(cache.TvAndFilms ?? []);

                    _allOsImages.Clear();
                    _allOsImages.AddRange(cache.OsImages ?? []);

                    ApplyFilter();

                    TimeSpan cacheAge = DateTime.Now - cache.CachedAt;
                    string ageText = cacheAge.TotalHours < 1
                        ? $"{(int)cacheAge.TotalMinutes}m ago"
                        : cacheAge.TotalDays < 1
                            ? $"{(int)cacheAge.TotalHours}h ago"
                            : $"{(int)cacheAge.TotalDays}d ago";

                    StatusMessage = $"Loaded {_allGames.Count} game(s), {_allApps.Count} app(s), {_allTvAndFilms.Count} film/TV(s), {_allOsImages.Count} OS image(s) from cache (scanned {ageText}) - Validating...";

                    if (settings.AutoScanOnStartup)
                    {
                        _ = Task.Run(async () => await ValidateAndRefreshCacheAsync(cache, settings));
                    }
                    else
                    {
                        StatusMessage = $"Loaded {_allGames.Count} game(s), {_allApps.Count} app(s), {_allTvAndFilms.Count} film/TV(s), {_allOsImages.Count} OS image(s) from cache (scanned {ageText})";
                    }
                }
                else
                {
                    if (settings.AutoScanOnStartup)
                    {
                        await ScanLibraryAsync();
                    }
                    else
                    {
                        StatusMessage = "Ready - Click Scan to discover games and apps";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during initialization: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ValidateAndRefreshCacheAsync(LibraryCacheSnapshot cache, AppSettings settings)
        {
            try
            {
                if (_validationCancellationTokenSource != null)
                {
                    await _validationCancellationTokenSource.CancelAsync();
                    _validationCancellationTokenSource.Dispose();
                }
                _validationCancellationTokenSource = new CancellationTokenSource();

                CacheValidationOutcome validationResult = await _libraryCacheService.ValidateCacheAsync(
                    cache,
                    settings,
                    _validationCancellationTokenSource.Token);

                if (IsDisposed)
                {
                    return;
                }

                if (validationResult.Result == CacheValidationResult.Valid)
                {
                    _ = TryEnqueueIfActive(() =>
                    {
                        StatusMessage = $"Library is up to date: {_allGames.Count} game(s), {_allApps.Count} app(s), {_allTvAndFilms.Count} film/TV(s), {_allOsImages.Count} OS image(s)";
                    });
                    return;
                }

                _ = TryEnqueueIfActive(() =>
                {
                    StatusMessage = "Changes detected - Rescanning library...";
                });

                _ = TryEnqueueIfActive(ScanLibraryAsync);
            }
            catch (OperationCanceledException)
            {
                // Validation cancelled - cache remains displayed
                _logger.LogInformation("Library cache validation was cancelled.");
            }
            catch (Exception ex)
            {
                _ = TryEnqueueIfActive(() =>
                {
                    StatusMessage = $"Validation error: {ex.Message}";
                });
            }
        }
    }
}
