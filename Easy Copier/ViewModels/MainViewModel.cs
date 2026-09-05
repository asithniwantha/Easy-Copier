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
        private CancellationTokenSource? _scanCancellationTokenSource;
        private CancellationTokenSource? _validationCancellationTokenSource;
        private List<GameEntry> _selectedGames = [];
        private System.Threading.Timer? _updateCheckTimer;
        private bool _isCheckingForUpdates;

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial bool IsUpdateAvailable { get; set; }

        [ObservableProperty]
        public partial bool IsUpdateReadyToInstall { get; set; }

        [ObservableProperty]
        public partial string UpdateMessage { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGamesEmpty))]
        [NotifyPropertyChangedFor(nameof(IsAppsEmpty))]
        [NotifyPropertyChangedFor(nameof(IsTvAndFilmsEmpty))]
        public partial bool IsScanning { get; set; }

        [ObservableProperty]
        public partial bool IsTransferring { get; set; }

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = "Ready";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedDrive))]
        [NotifyPropertyChangedFor(nameof(DriveSpaceSummary))]
        [NotifyPropertyChangedFor(nameof(DriveDetailsSummary))]
        public partial RemovableDrive? SelectedDrive { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectionSummary))]
        public partial int SelectedGamesCount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectionSummary))]
        public partial long SelectedGamesTotalBytes { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectionSummary))]
        public partial int SelectedGamesTotalPrice { get; set; }


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EmptyGamesMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyAppsMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyTvAndFilmsMessage))]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EmptyGamesMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyAppsMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyTvAndFilmsMessage))]
        public partial GameCategory SelectedCategory { get; set; } = GameCategory.All;

        public IReadOnlyList<GameCategory> AvailableCategories { get; } = Enum.GetValues<GameCategory>();


        private readonly List<GameEntry> _allGames = [];
        private readonly List<GameEntry> _allApps = [];
        private readonly List<GameEntry> _allTvAndFilms = [];

        public bool IsGamesEmpty => !IsScanning && Games.Count == 0;
        public bool IsAppsEmpty => !IsScanning && Apps.Count == 0;
        public bool IsTvAndFilmsEmpty => !IsScanning && TvAndFilms.Count == 0;

        public string EmptyGamesMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No games found. Add a game folder in Settings and scan your library."
            : $"No games match \"{SearchText}\".";

        public string EmptyAppsMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No apps found. Add an app folder in Settings and scan your library."
            : $"No apps match \"{SearchText}\".";

        public string EmptyTvAndFilmsMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No films/TV series found. Add a folder in Settings and scan your library."
            : $"No films/TV series match \"{SearchText}\".";

        public bool HasSelectedDrive => SelectedDrive != null;

        public string DriveSpaceSummary => SelectedDrive == null
            ? string.Empty
            : $"{FormattingHelpers.FormatBytes(SelectedDrive.FreeBytes)} free of {FormattingHelpers.FormatBytes(SelectedDrive.TotalBytes)}";

        public string DriveDetailsSummary => SelectedDrive == null
            ? string.Empty
            : $"{SelectedDrive.DriveLetter} \u2022 {SelectedDrive.Brand} \u2022 {SelectedDrive.FileSystem}";

        public string SelectionSummary => SelectedGamesCount == 0
            ? "No items selected"
            : $"{SelectedGamesCount} item(s) selected \u2022 {FormattingHelpers.FormatBytes(SelectedGamesTotalBytes)} \u2022 Rs. {SelectedGamesTotalPrice}";

        public int CurrentTabIndex { get; set; }

        public ObservableCollection<GameEntry> Games { get; } = [];
        public ObservableCollection<GameEntry> Apps { get; } = [];
        public ObservableCollection<GameEntry> TvAndFilms { get; } = [];
        public ObservableCollection<RemovableDrive> AvailableDrives { get; } = [];
        public ObservableCollection<ValidationResult> ValidationMessages { get; } = [];
        public ObservableCollection<TransferQueueItem> TransferQueue => _transferQueueService.QueueItems;

        public event EventHandler? ItemQueued;

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
            IDialogService dialogService)
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

            _driveDiscoveryService.DrivesChanged += (s, e) =>
            {
                if (!_dispatcherService.HasThreadAccess)
                {
                    _ = _dispatcherService.TryEnqueue(async () => await RefreshDrivesAsync());
                }
                else
                {
                    _ = RefreshDrivesAsync();
                }
            };

            _transferQueueService.ItemCompleted += OnQueueItemCompleted;
        }

        public async Task InitializeAsync()
        {
            _ = CheckForUpdatesBackgroundAsync();

            // Start periodic update checks every 4 hours
            _updateCheckTimer = new System.Threading.Timer(
                _ => { _ = _dispatcherService.TryEnqueue(() => { _ = CheckForUpdatesBackgroundAsync(); }); },
                null,
                TimeSpan.FromHours(4),
                TimeSpan.FromHours(4));

            try
            {
                IsLoading = true;
                StatusMessage = "Loading settings...";

                AppSettings settings = await _settingsService.LoadSettingsAsync();

                _driveDiscoveryService.StartWatching();
                await RefreshDrivesAsync();

                if (settings.GameSourceFolders.Count == 0 && settings.AppSourceFolders.Count == 0 && (settings.TvAndFilmSourceFolders == null || settings.TvAndFilmSourceFolders.Count == 0))
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

                    ApplyFilter();

                    TimeSpan cacheAge = DateTime.Now - cache.CachedAt;
                    string ageText = cacheAge.TotalHours < 1
                        ? $"{(int)cacheAge.TotalMinutes}m ago"
                        : cacheAge.TotalDays < 1
                            ? $"{(int)cacheAge.TotalHours}h ago"
                            : $"{(int)cacheAge.TotalDays}d ago";

                    StatusMessage = $"Loaded {_allGames.Count} game(s), {_allApps.Count} app(s), {_allTvAndFilms.Count} film/TV(s) from cache (scanned {ageText}) - Validating...";

                    if (settings.AutoScanOnStartup)
                    {
                        _ = Task.Run(async () => await ValidateAndRefreshCacheAsync(cache, settings));
                    }
                    else
                    {
                        StatusMessage = $"Loaded {_allGames.Count} game(s), {_allApps.Count} app(s), {_allTvAndFilms.Count} film/TV(s) from cache (scanned {ageText})";
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
                }
                _validationCancellationTokenSource = new CancellationTokenSource();

                CacheValidationOutcome validationResult = await _libraryCacheService.ValidateCacheAsync(
                    cache,
                    settings,
                    _validationCancellationTokenSource.Token);

                if (validationResult.Result == CacheValidationResult.Valid)
                {
                    _ = _dispatcherService.TryEnqueue(() =>
                        {
                            StatusMessage = $"Library is up to date: {_allGames.Count} game(s), {_allApps.Count} app(s), {_allTvAndFilms.Count} film/TV(s)";
                        });
                    return;
                }

                _ = _dispatcherService.TryEnqueue(() =>
                    {
                        StatusMessage = "Changes detected - Rescanning library...";
                    });

                await Task.Run(async () =>
                {
                    _ = _dispatcherService.TryEnqueue(async () => await ScanLibraryAsync());
                });
            }
            catch (OperationCanceledException)
            {
                // Validation cancelled - cache remains displayed
            }
            catch (Exception ex)
            {
                _ = _dispatcherService.TryEnqueue(() =>
                    {
                        StatusMessage = $"Validation error: {ex.Message}";
                    });
            }
        }



    }
}
