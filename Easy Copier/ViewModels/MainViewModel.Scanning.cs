using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    public sealed partial class MainViewModel
    {
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

                (IReadOnlyList<GameEntry> Games, IReadOnlyList<GameEntry> Apps, IReadOnlyList<GameEntry> TvAndFilms) scanResult = await _libraryScannerService.ScanAllLibrariesAsync(
                    settings,
                    progress,
                    _scanCancellationTokenSource.Token);

                _allGames.AddRange(scanResult.Games);
                _allApps.AddRange(scanResult.Apps);
                _allTvAndFilms.AddRange(scanResult.TvAndFilms);

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

        /// <summary>
        /// Handles changes to the SearchText property to re-apply library filtering.
        /// Uses nullable string? for oldValue to match the CommunityToolkit.Mvvm partial method declaration for reference types.
        /// </summary>
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

            AppSettings settings = _settingsService.LoadSettingsSync();
            SelectedGamesTotalPrice = _selectedGames.Sum(g => Infrastructure.FormattingHelpers.CalculatePrice(g.TotalBytes, settings));

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
            _updateCheckTimer?.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task CheckForUpdatesBackgroundAsync()
        {
            if (_isCheckingForUpdates)
            {
                return;
            }

            try
            {
                _isCheckingForUpdates = true;
                _logger.LogInformation("Checking for new updates in the background...");
                bool hasUpdate = await _updateService.CheckForUpdatesAsync();
                if (hasUpdate)
                {
                    _logger.LogInformation("A new update is available.");
                    AppSettings settings = await _settingsService.LoadSettingsAsync();
                    if (settings.AutoDownloadUpdates)
                    {
                        _logger.LogInformation("Automatic update download is enabled. Starting background download...");
                        _ = _dispatcherService.TryEnqueue(() =>
                        {
                            IsUpdateAvailable = true;
                            UpdateMessage = "Downloading update in background...";
                        });

                        await _updateService.DownloadUpdateAsync();

                        _logger.LogInformation("Background update download completed. Update is ready to install.");
                        _ = _dispatcherService.TryEnqueue(() =>
                        {
                            IsUpdateAvailable = false;
                            IsUpdateReadyToInstall = true;
                            UpdateMessage = "Update ready to install. Restart to apply.";
                        });
                    }
                    else
                    {
                        _logger.LogInformation("Automatic update download is disabled. Notifying user.");
                        _ = _dispatcherService.TryEnqueue(() =>
                        {
                            IsUpdateAvailable = true;
                            UpdateMessage = "A new update is available!";
                        });
                    }
                }
                else
                {
                    _logger.LogInformation("No new updates found in the background.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates in background");
                System.Diagnostics.Debug.WriteLine($"Error checking for updates in background: {ex}");
            }
            finally
            {
                _isCheckingForUpdates = false;
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
    }
}
