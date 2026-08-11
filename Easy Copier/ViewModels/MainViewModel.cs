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
    public partial class MainViewModel : ObservableObject
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
        private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
        private CancellationTokenSource? _scanCancellationTokenSource;
        private CancellationTokenSource? _validationCancellationTokenSource;
        private List<GameEntry> _selectedGames = [];

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGamesEmpty))]
        [NotifyPropertyChangedFor(nameof(IsAppsEmpty))]
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
        public partial string SearchText { get; set; } = string.Empty;

        private readonly List<GameEntry> _allGames = [];
        private readonly List<GameEntry> _allApps = [];

        public bool IsGamesEmpty => !IsScanning && Games.Count == 0;
        public bool IsAppsEmpty => !IsScanning && Apps.Count == 0;

        public string EmptyGamesMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No games found. Add a game folder in Settings and scan your library."
            : $"No games match \"{SearchText}\".";

        public string EmptyAppsMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No apps found. Add an app folder in Settings and scan your library."
            : $"No apps match \"{SearchText}\".";

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

        public int CurrentTabIndex { get; set; } = 0;

        public ObservableCollection<GameEntry> Games { get; } = [];
        public ObservableCollection<GameEntry> Apps { get; } = [];
        public ObservableCollection<RemovableDrive> AvailableDrives { get; } = [];
        public ObservableCollection<ValidationResult> ValidationMessages { get; } = [];
        public ObservableCollection<TransferQueueItem> TransferQueue => _transferQueueService.QueueItems;

        public event EventHandler? ItemQueued;

        public MainViewModel(
            ISettingsService settingsService,
            ILibraryCacheService libraryCacheService,
            IGameScannerService gameScannerService,
            IDriveDiscoveryService driveDiscoveryService,
            IDriveValidationService driveValidationService,
            IFileTransferService fileTransferService,
            ITransferQueueService transferQueueService,
            IWindowService windowService,
            IProcessService processService)
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
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            _driveDiscoveryService.DrivesChanged += (s, e) =>
            {
                if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
                {
                    _ = _dispatcherQueue.TryEnqueue(async () => await RefreshDrivesAsync());
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
            try
            {
                IsLoading = true;
                StatusMessage = "Loading settings...";

                AppSettings settings = await _settingsService.LoadSettingsAsync();

                _driveDiscoveryService.StartWatching();
                await RefreshDrivesAsync();

                if (!settings.GameSourceFolders.Any() && !settings.AppSourceFolders.Any())
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

                    ApplyFilter();

                    TimeSpan cacheAge = DateTime.Now - cache.CachedAt;
                    string ageText = cacheAge.TotalHours < 1
                        ? $"{(int)cacheAge.TotalMinutes}m ago"
                        : cacheAge.TotalDays < 1
                            ? $"{(int)cacheAge.TotalHours}h ago"
                            : $"{(int)cacheAge.TotalDays}d ago";

                    StatusMessage = $"Loaded {_allGames.Count} game(s), {_allApps.Count} app(s) from cache (scanned {ageText}) - Validating...";

                    if (settings.AutoScanOnStartup)
                    {
                        _ = Task.Run(async () => await ValidateAndRefreshCacheAsync(cache, settings));
                    }
                    else
                    {
                        StatusMessage = $"Loaded {_allGames.Count} game(s), {_allApps.Count} app(s) from cache (scanned {ageText})";
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
                _validationCancellationTokenSource?.Cancel();
                _validationCancellationTokenSource = new CancellationTokenSource();

                CacheValidationOutcome validationResult = await _libraryCacheService.ValidateCacheAsync(
                    cache,
                    settings,
                    _validationCancellationTokenSource.Token);

                if (validationResult.Result == CacheValidationResult.Valid)
                {
                    _ = _dispatcherQueue?.TryEnqueue(() =>
                        {
                            StatusMessage = $"Library is up to date: {_allGames.Count} game(s), {_allApps.Count} app(s)";
                        });
                    return;
                }

                _ = _dispatcherQueue?.TryEnqueue(() =>
                    {
                        StatusMessage = "Changes detected - Rescanning library...";
                    });

                await Task.Run(async () =>
                {
                    _ = _dispatcherQueue?.TryEnqueue(async () => await ScanLibraryAsync());
                });
            }
            catch (OperationCanceledException)
            {
                // Validation cancelled - cache remains displayed
            }
            catch (Exception ex)
            {
                _ = _dispatcherQueue?.TryEnqueue(() =>
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
                Games.Clear();
                Apps.Clear();
                ValidationMessages.Clear();

                _scanCancellationTokenSource?.Cancel();
                _scanCancellationTokenSource = new CancellationTokenSource();

                AppSettings settings = await _settingsService.LoadSettingsAsync();

                if (!settings.GameSourceFolders.Any() && !settings.AppSourceFolders.Any())
                {
                    await _libraryCacheService.InvalidateCacheAsync();
                    StatusMessage = "No source folders configured. Please add folders in Settings.";
                    return;
                }

                Progress<string> progress = new(message =>
                {
                    StatusMessage = message;
                });

                if (settings.GameSourceFolders.Any())
                {
                    IReadOnlyList<GameEntry> games = await _gameScannerService.ScanLibraryAsync(
                        settings.GameSourceFolders,
                        LibraryCategory.Game,
                        progress,
                        _scanCancellationTokenSource.Token);

                    _allGames.AddRange(games);
                }

                if (settings.AppSourceFolders.Any())
                {
                    IReadOnlyList<GameEntry> apps = await _gameScannerService.ScanLibraryAsync(
                        settings.AppSourceFolders,
                        LibraryCategory.App,
                        progress,
                        _scanCancellationTokenSource.Token);

                    _allApps.AddRange(apps);
                }

                ApplyFilter();

                StatusMessage = _allGames.Count == 0 && _allApps.Count == 0
                    ? "No games or apps found in configured folders"
                    : $"Found {_allGames.Count} game(s), {_allApps.Count} app(s)";

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

                List<GameEntry> allEntries = _allGames.Concat(_allApps).ToList();

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
                    _allGames.ToList(),
                    _allApps.ToList(),
                    settings.GameSourceFolders.ToList(),
                    settings.AppSourceFolders.ToList(),
                    DateTime.Now,
                    fingerprints);

                await _libraryCacheService.SaveCacheAsync(snapshot);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save cache: {ex.Message}";
            }
        }

        partial void OnSearchTextChanged(string? oldValue, string newValue) => ApplyFilter();

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

            OnPropertyChanged(nameof(IsGamesEmpty));
            OnPropertyChanged(nameof(IsAppsEmpty));
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
            if (SelectedDrive == null || !_selectedGames.Any())
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

                List<GameEntry> itemsToQueue = _selectedGames.ToList();
                _ = _transferQueueService.Enqueue(itemsToQueue, SelectedDrive, destinationPath);

                StatusMessage = $"Queued {itemsToQueue.Count} item(s) for {SelectedDrive.DriveLetter} ({TransferQueue.Count} in queue)";
                IsTransferring = TransferQueue.Any(i => i.IsActive);

                ItemQueued?.Invoke(this, EventArgs.Empty);
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
            _selectedGames = selectedGames.ToList();
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

        [RelayCommand]
        private void AddSourceFolder()
        {
            var openAction = CurrentTabIndex == 1
                ? Infrastructure.SettingsOpenAction.AddAppFolder
                : Infrastructure.SettingsOpenAction.AddGameFolder;

            _windowService.ShowSettingsWindow(null, openAction);
        }
    }
}
