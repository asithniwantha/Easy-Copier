using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
        private CancellationTokenSource? _scanCancellationTokenSource;
        private CancellationTokenSource? _validationCancellationTokenSource;
        private List<GameEntry> _selectedGames = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private bool _isTransferring;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private RemovableDrive? _selectedDrive;

        [ObservableProperty]
        private int _selectedGamesCount;

        [ObservableProperty]
        private long _selectedGamesTotalBytes;

        [ObservableProperty]
        private string _searchText = string.Empty;

        private List<GameEntry> _allGames = new();
        private List<GameEntry> _allApps = new();

        public ObservableCollection<GameEntry> Games { get; } = new();
        public ObservableCollection<GameEntry> Apps { get; } = new();
        public ObservableCollection<RemovableDrive> AvailableDrives { get; } = new();
        public ObservableCollection<ValidationResult> ValidationMessages { get; } = new();
        public ObservableCollection<TransferQueueItem> TransferQueue => _transferQueueService.QueueItems;

        public event EventHandler? ItemQueued;

        public MainViewModel(
            ISettingsService settingsService,
            ILibraryCacheService libraryCacheService,
            IGameScannerService gameScannerService,
            IDriveDiscoveryService driveDiscoveryService,
            IDriveValidationService driveValidationService,
            IFileTransferService fileTransferService,
            ITransferQueueService transferQueueService)
        {
            _settingsService = settingsService;
            _libraryCacheService = libraryCacheService;
            _gameScannerService = gameScannerService;
            _driveDiscoveryService = driveDiscoveryService;
            _driveValidationService = driveValidationService;
            _fileTransferService = fileTransferService;
            _transferQueueService = transferQueueService;
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            _driveDiscoveryService.DrivesChanged += (s, e) =>
            {
                if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
                {
                    _dispatcherQueue.TryEnqueue(async () => await RefreshDrivesAsync());
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

                var settings = await _settingsService.LoadSettingsAsync();

                _driveDiscoveryService.StartWatching();
                await RefreshDrivesAsync();

                if (!settings.GameSourceFolders.Any() && !settings.AppSourceFolders.Any())
                {
                    StatusMessage = "Ready - No source folders configured";
                    return;
                }

                var cache = await _libraryCacheService.LoadCacheAsync();

                if (cache != null)
                {
                    _allGames.Clear();
                    _allGames.AddRange(cache.Games);

                    _allApps.Clear();
                    _allApps.AddRange(cache.Apps);

                    ApplyFilter();

                    var cacheAge = DateTime.Now - cache.CachedAt;
                    var ageText = cacheAge.TotalHours < 1
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

                var validationResult = await _libraryCacheService.ValidateCacheAsync(
                    cache,
                    settings,
                    _validationCancellationTokenSource.Token);

                if (validationResult.Result == CacheValidationResult.Valid)
                {
                    if (_dispatcherQueue != null)
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            StatusMessage = $"Library is up to date: {_allGames.Count} game(s), {_allApps.Count} app(s)";
                        });
                    }
                    return;
                }

                if (_dispatcherQueue != null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        StatusMessage = "Changes detected - Rescanning library...";
                    });
                }

                await Task.Run(async () =>
                {
                    if (_dispatcherQueue != null)
                    {
                        _dispatcherQueue.TryEnqueue(async () => await ScanLibraryAsync());
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Validation cancelled - cache remains displayed
            }
            catch (Exception ex)
            {
                if (_dispatcherQueue != null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        StatusMessage = $"Validation error: {ex.Message}";
                    });
                }
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

                var settings = await _settingsService.LoadSettingsAsync();

                if (!settings.GameSourceFolders.Any() && !settings.AppSourceFolders.Any())
                {
                    await _libraryCacheService.InvalidateCacheAsync();
                    StatusMessage = "No source folders configured. Please add folders in Settings.";
                    return;
                }

                var progress = new Progress<string>(message =>
                {
                    StatusMessage = message;
                });

                if (settings.GameSourceFolders.Any())
                {
                    var games = await _gameScannerService.ScanLibraryAsync(
                        settings.GameSourceFolders,
                        LibraryCategory.Game,
                        progress,
                        _scanCancellationTokenSource.Token);

                    _allGames.AddRange(games);
                }

                if (settings.AppSourceFolders.Any())
                {
                    var apps = await _gameScannerService.ScanLibraryAsync(
                        settings.AppSourceFolders,
                        LibraryCategory.App,
                        progress,
                        _scanCancellationTokenSource.Token);

                    _allApps.AddRange(apps);
                }

                ApplyFilter();

                if (_allGames.Count == 0 && _allApps.Count == 0)
                {
                    StatusMessage = "No games or apps found in configured folders";
                }
                else
                {
                    StatusMessage = $"Found {_allGames.Count} game(s), {_allApps.Count} app(s)";
                }

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
                var fingerprints = new Dictionary<string, ItemFingerprint>();

                var allEntries = _allGames.Concat(_allApps).ToList();

                foreach (var entry in allEntries)
                {
                    try
                    {
                        var fingerprint = await ComputeItemFingerprintAsync(entry.FolderPath);
                        var normalizedPath = Path.GetFullPath(entry.FolderPath)
                            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        fingerprints[normalizedPath] = fingerprint;
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Warning: Could not compute fingerprint for {entry.Name}: {ex.Message}";
                    }
                }

                var snapshot = new LibraryCacheSnapshot(
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

        private async Task<ItemFingerprint> ComputeItemFingerprintAsync(string folderPath)
        {
            return await Task.Run(() =>
            {
                var dirInfo = new DirectoryInfo(folderPath);
                var files = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories);

                long totalBytes = 0;
                var latestWriteTime = DateTime.MinValue;

                foreach (var file in files)
                {
                    totalBytes += file.Length;

                    if (file.LastWriteTimeUtc > latestWriteTime)
                    {
                        latestWriteTime = file.LastWriteTimeUtc;
                    }
                }

                var normalizedPath = Path.GetFullPath(folderPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                return new ItemFingerprint(normalizedPath, totalBytes, latestWriteTime);
            });
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = SearchText?.Trim() ?? string.Empty;

            IEnumerable<GameEntry> FilterEntries(IEnumerable<GameEntry> source) =>
                string.IsNullOrEmpty(query)
                    ? source
                    : source.Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

            Games.Clear();
            foreach (var game in FilterEntries(_allGames))
            {
                Games.Add(game);
            }

            Apps.Clear();
            foreach (var app in FilterEntries(_allApps))
            {
                Apps.Add(app);
            }
        }

        [RelayCommand]
        private async Task RefreshDrivesAsync()
        {
            try
            {
                StatusMessage = "Refreshing drives...";

                var drives = await _driveDiscoveryService.GetRemovableDrivesAsync();

                AvailableDrives.Clear();
                foreach (var drive in drives)
                {
                    AvailableDrives.Add(drive);
                }

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
                return;

            try
            {
                ValidationMessages.Clear();
                var destinationPath = $"{SelectedDrive.DriveLetter}\\";

                // Account for bytes already reserved by other queued/in-progress transfers
                // targeting the same drive, so validation reflects true remaining space.
                var reservedBytes = _transferQueueService.GetReservedBytes(SelectedDrive.DriveLetter);
                var driveForValidation = reservedBytes > 0
                    ? SelectedDrive with { FreeBytes = Math.Max(0, SelectedDrive.FreeBytes - reservedBytes) }
                    : SelectedDrive;

                var validation = await _driveValidationService.ValidateTransferAsync(
                    _selectedGames, driveForValidation, destinationPath);

                foreach (var result in validation)
                {
                    ValidationMessages.Add(result);
                }

                if (validation.Any(v => v.Severity == ValidationSeverity.Error))
                {
                    StatusMessage = "Cannot queue transfer: validation failed. See warnings.";
                    return;
                }

                var itemsToQueue = _selectedGames.ToList();
                _transferQueueService.Enqueue(itemsToQueue, SelectedDrive, destinationPath);

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
    }
}
