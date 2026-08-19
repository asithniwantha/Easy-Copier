using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    public sealed partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILibraryCacheService _libraryCacheService;
        private readonly IGameScannerService _gameScannerService;
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
        [NotifyPropertyChangedFor(nameof(EmptyGamesMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyAppsMessage))]
        [NotifyPropertyChangedFor(nameof(EmptyTvAndFilmsMessage))]
        public partial string SearchText { get; set; } = string.Empty;

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
            : $"{SelectedGamesCount} item(s) selected \u2022 {FormattingHelpers.FormatBytes(SelectedGamesTotalBytes)}";

        public int CurrentTabIndex { get; set; }

        public ObservableCollection<GameEntry> Games { get; } = [];
        public ObservableCollection<GameEntry> Apps { get; } = [];
        public ObservableCollection<GameEntry> TvAndFilms { get; } = [];
        public ObservableCollection<RemovableDrive> AvailableDrives { get; } = [];
        public ObservableCollection<ValidationResult> ValidationMessages { get; } = [];
        public ObservableCollection<TransferQueueItem> TransferQueue => _transferQueueService.QueueItems;

        public event EventHandler? ItemQueued;

        public MainViewModel(
            IUpdateService updateService,
            ISettingsService settingsService,
            ILibraryCacheService libraryCacheService,
            IGameScannerService gameScannerService,
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
            _settingsService = settingsService;
            _libraryCacheService = libraryCacheService;
            _gameScannerService = gameScannerService;
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

        [RelayCommand]
        private async Task ScanLibraryAsync()
        {
            try
            {
                IsScanning = true;
                _allGames.Clear();
                _allApps.Clear();
                _allTvAndFilms.Clear();
                Games.Clear();
                Apps.Clear();
                TvAndFilms.Clear();
                ValidationMessages.Clear();

                if (_scanCancellationTokenSource != null)
                {
                    await _scanCancellationTokenSource.CancelAsync();
                }
                _scanCancellationTokenSource = new CancellationTokenSource();

                AppSettings settings = await _settingsService.LoadSettingsAsync();

                if (settings.GameSourceFolders.Count == 0 && settings.AppSourceFolders.Count == 0 && (settings.TvAndFilmSourceFolders == null || settings.TvAndFilmSourceFolders.Count == 0))
                {
                    await _libraryCacheService.InvalidateCacheAsync();
                    StatusMessage = "No source folders configured. Please add folders in Settings.";
                    return;
                }

                Progress<string> progress = new(message =>
                {
                    StatusMessage = message;
                });

                if (settings.GameSourceFolders.Count > 0)
                {
                    IReadOnlyList<GameEntry> games = await _gameScannerService.ScanLibraryAsync(
                        settings.GameSourceFolders,
                        LibraryCategory.Game,
                        progress,
                        cancellationToken: _scanCancellationTokenSource.Token);

                    _allGames.AddRange(games);
                }

                if (settings.AppSourceFolders.Count > 0)
                {
                    IReadOnlyList<GameEntry> apps = await _gameScannerService.ScanLibraryAsync(
                        settings.AppSourceFolders,
                        LibraryCategory.App,
                        progress,
                        cancellationToken: _scanCancellationTokenSource.Token);

                    _allApps.AddRange(apps);
                }

                if (settings.TvAndFilmSourceFolders != null && settings.TvAndFilmSourceFolders.Count > 0)
                {
                    IReadOnlyList<GameEntry> tvAndFilms = await _gameScannerService.ScanLibraryAsync(
                        settings.TvAndFilmSourceFolders,
                        LibraryCategory.TvAndFilm,
                        progress,
                        settings.VideoFileExtensions,
                        _scanCancellationTokenSource.Token);

                    _allTvAndFilms.AddRange(tvAndFilms);
                }

                ApplyFilter();

                StatusMessage = _allGames.Count == 0 && _allApps.Count == 0 && _allTvAndFilms.Count == 0
                    ? "No items found in configured folders"
                    : $"Found {_allGames.Count} game(s), {_allApps.Count} app(s), {_allTvAndFilms.Count} film/TV(s)";

                settings.LastScanTime = DateTime.Now;
                await _settingsService.SaveSettingsAsync(settings);

                await SaveCacheSnapshotAsync(settings);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Scan cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Scan error: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task SaveCacheSnapshotAsync(AppSettings settings)
        {
            try
            {
                Dictionary<string, ItemFingerprint> fingerprints = [];

                List<GameEntry> allEntries = [.. _allGames, .. _allApps, .. _allTvAndFilms];

                foreach (GameEntry? entry in allEntries)
                {
                    try
                    {
                        ItemFingerprint fingerprint = await _libraryCacheService.ComputeItemFingerprintAsync(entry.FolderPath);
                        string normalizedPath = Path.GetFullPath(entry.FolderPath)
                            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        fingerprints[normalizedPath] = fingerprint;
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Warning: Could not compute fingerprint for {entry.Name}: {ex.Message}";
                    }
                }

                LibraryCacheSnapshot snapshot = new(
                    LibraryCacheSnapshot.CurrentSchemaVersion,
                    [.. _allGames],
                    [.. _allApps],
                    [.. _allTvAndFilms],
                    [.. settings.GameSourceFolders],
                    [.. settings.AppSourceFolders],
                    [.. settings.TvAndFilmSourceFolders ?? []],
                    DateTime.Now,
                    fingerprints);

                await _libraryCacheService.SaveCacheAsync(snapshot);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save cache: {ex.Message}";
            }
        }

        partial void OnSearchTextChanged(string oldValue, string newValue) => ApplyFilter();

        private void ApplyFilter()
        {
            string query = SearchText?.Trim() ?? string.Empty;

            IEnumerable<GameEntry> FilterEntries(IEnumerable<GameEntry> source)
            {
                return string.IsNullOrEmpty(query)
                    ? source
                    : source.Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            Games.UpdateFrom(FilterEntries(_allGames));
            Apps.UpdateFrom(FilterEntries(_allApps));
            TvAndFilms.UpdateFrom(FilterEntries(_allTvAndFilms));

            OnPropertyChanged(nameof(IsGamesEmpty));
            OnPropertyChanged(nameof(IsAppsEmpty));
            OnPropertyChanged(nameof(IsTvAndFilmsEmpty));
        }

        [RelayCommand]
        private async Task RefreshDrivesAsync()
        {
            try
            {
                StatusMessage = "Refreshing drives...";

                IReadOnlyList<RemovableDrive> drives = await _driveDiscoveryService.GetRemovableDrivesAsync();

                AvailableDrives.UpdateFrom(drives);

                if (AvailableDrives.Count == 0)
                {
                    StatusMessage = "No removable drives found";
                    SelectedDrive = null;
                }
                else
                {
                    StatusMessage = $"Found {AvailableDrives.Count} removable drive(s)";

                    if (SelectedDrive == null && AvailableDrives.Any())
                    {
                        SelectedDrive = AvailableDrives.First();
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing drives: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopyGames))]
        private async Task CopySelectedGamesAsync()
        {
            if (SelectedDrive == null || _selectedGames.Count == 0)
            {
                return;
            }

            try
            {

                string destinationPath = $"{SelectedDrive.DriveLetter}\\";

                // Account for bytes already reserved by other queued/in-progress transfers
                // targeting the same drive, so validation reflects true remaining space.
                long reservedBytes = _transferQueueService.GetReservedBytes(SelectedDrive.DriveLetter);
                RemovableDrive driveForValidation = reservedBytes > 0
                    ? SelectedDrive with { FreeBytes = Math.Max(0, SelectedDrive.FreeBytes - reservedBytes) }
                    : SelectedDrive;

                IReadOnlyList<ValidationResult> validation = await _driveValidationService.ValidateTransferAsync(
                    _selectedGames, driveForValidation, destinationPath);

                ValidationMessages.UpdateFrom(validation);

                if (validation.Any(v => v.Severity == ValidationSeverity.Error))
                {
                    StatusMessage = "Cannot queue transfer: validation failed. See warnings.";
                    return;
                }

                List<TransferItem> itemsToQueue = [];
                bool applyToAll = false;
                CopyAction globalAction = CopyAction.Default;

                foreach (GameEntry game in _selectedGames)
                {
                    string destItemPath = Path.Combine(destinationPath, game.Name);
                    if (System.IO.File.Exists(game.FolderPath))
                    {
                        destItemPath = Path.Combine(destinationPath, Path.GetFileName(game.FolderPath));
                    }

                    bool destExists = System.IO.Directory.Exists(destItemPath) || System.IO.File.Exists(destItemPath);

                    if (destExists)
                    {
                        if (applyToAll)
                        {
                            if (globalAction != CopyAction.Skip)
                            {
                                itemsToQueue.Add(new TransferItem(game, globalAction));
                            }
                        }
                        else
                        {
                            (long Size, int Count) = await _fileTransferService.GetFolderStatsAsync(game.FolderPath);
                            (long Size, int Count) destStats = await _fileTransferService.GetFolderStatsAsync(destItemPath);

                            (CopyAction Action, bool ApplyToAll) dialogResult = await _dialogService.ShowConflictDialogAsync(
                                game.Name,
                                Size,
                                Count,
                                destStats.Size,
                                destStats.Count);

                            if (dialogResult.ApplyToAll)
                            {
                                applyToAll = true;
                                globalAction = dialogResult.Action;
                            }

                            if (dialogResult.Action != CopyAction.Skip)
                            {
                                itemsToQueue.Add(new TransferItem(game, dialogResult.Action));
                            }
                        }
                    }
                    else
                    {
                        itemsToQueue.Add(new TransferItem(game, CopyAction.Default));
                    }
                }

                if (itemsToQueue.Count > 0)
                {
                    _ = _transferQueueService.Enqueue(itemsToQueue, SelectedDrive, destinationPath);
                    StatusMessage = $"Queued {itemsToQueue.Count} item(s) for {SelectedDrive.DriveLetter} ({TransferQueue.Count} in queue)";
                    IsTransferring = TransferQueue.Any(i => i.IsActive);
                    ItemQueued?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    StatusMessage = "No items to queue (all skipped).";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Queue error: {ex.Message}";
            }
        }

        private void OnQueueItemCompleted(object? sender, TransferQueueItem completedItem)
        {
            IsTransferring = TransferQueue.Any(i => i.IsActive);

            if (completedItem.Status == TransferQueueItemStatus.Completed)
            {
                StatusMessage = $"Completed: {completedItem.ItemsSummary} \u2192 {completedItem.TargetDrive.DriveLetter}";
                _ = RefreshDrivesAsync();
            }
            else
            {
                StatusMessage = $"Transfer failed: {completedItem.ItemsSummary} - {completedItem.StatusMessage}";
            }
        }

        [RelayCommand]
        private void ClearFinishedQueueItems()
        {
            _transferQueueService.ClearFinished();
        }

        private bool CanCopyGames()
        {
            return SelectedGamesCount > 0 && SelectedDrive != null;
        }

        public void UpdateSelectionSummary(System.Collections.Generic.IEnumerable<GameEntry> selectedGames)
        {
            _selectedGames = [.. selectedGames];
            SelectedGamesCount = _selectedGames.Count;
            SelectedGamesTotalBytes = _selectedGames.Sum(g => g.TotalBytes);
            CopySelectedGamesCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void OpenSettings()
        {
            _windowService.ShowSettingsWindow(async () => await ScanLibraryCommand.ExecuteAsync(null));
        }

        [RelayCommand]
        private void OpenHistory()
        {
            _windowService.ShowHistoryWindow();
        }

        [RelayCommand]
        private void OpenAbout()
        {
            _windowService.ShowAboutWindow();
        }

        [RelayCommand]
        private void OpenDriveInExplorer()
        {
            if (SelectedDrive != null)
            {
                _processService.OpenInExplorer($"{SelectedDrive.DriveLetter}\\");
            }
        }

        [RelayCommand]
        private void OpenItemFolder(string folderPath)
        {
            if (!string.IsNullOrEmpty(folderPath))
            {
                _processService.OpenInExplorer(folderPath);
            }
        }

        public async Task<string> GetFormattedSystemRequirementsAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return string.Empty;
            }

            string rawRequirementsText = await _sourceLibraryService.GetSystemRequirementsAsync(folderPath);
            return SysReqFormatter.FormatText(rawRequirementsText);
        }

        [RelayCommand]
        private void AddSourceFolder()
        {
            SettingsOpenAction openAction = CurrentTabIndex == 0 ? Infrastructure.SettingsOpenAction.AddGameFolder :
                                            CurrentTabIndex == 1 ? Infrastructure.SettingsOpenAction.AddAppFolder :
                                            Infrastructure.SettingsOpenAction.AddTvAndFilmFolder;

            _windowService.ShowSettingsWindow(null, openAction);
        }

        public void Dispose()
        {
            _scanCancellationTokenSource?.Dispose();
            _validationCancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
private async Task CheckForUpdatesBackgroundAsync()
        {
            try
            {
                bool hasUpdate = await _updateService.CheckForUpdatesAsync();
                if (hasUpdate)
                {
                    AppSettings settings = await _settingsService.LoadSettingsAsync();
                    if (settings.AutoDownloadUpdates)
                    {
                        _dispatcherService.TryEnqueue(() =>
                        {
                            IsUpdateAvailable = true;
                            UpdateMessage = "Downloading update in background...";
                        });

                        await _updateService.DownloadUpdateAsync();

                        _dispatcherService.TryEnqueue(() =>
                        {
                            IsUpdateAvailable = false;
                            IsUpdateReadyToInstall = true;
                            UpdateMessage = "Update ready to install. Restart to apply.";
                        });
                    }
                    else
                    {
                        _dispatcherService.TryEnqueue(() =>
                        {
                            IsUpdateAvailable = true;
                            UpdateMessage = "A new update is available!";
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for updates in background: {ex}");
            }
        }

        [RelayCommand]
        private async Task DownloadUpdateAsync()
        {
            try
            {
                IsUpdateAvailable = true;
                UpdateMessage = "Downloading update...";

                await _updateService.DownloadUpdateAsync();

                IsUpdateAvailable = false;
                IsUpdateReadyToInstall = true;
                UpdateMessage = "Update ready to install. Restart to apply.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading update manually: {ex}");
                UpdateMessage = "Failed to download update.";
            }
        }

        [RelayCommand]
        private void RestartAndApplyUpdate()
        {
            _updateService.RestartAndApplyUpdate();
        }
    }
}
