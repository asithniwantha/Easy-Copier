using CommunityToolkit.Mvvm.ComponentModel;
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
    /// <summary>
    /// Partial class implementation of <see cref="MainViewModel"/> handling library scanning, filtering, drive actions, and background update checks.
    /// </summary>
    public sealed partial class MainViewModel
    {
        /// <summary>
        /// Asynchronously scans configured source library folders for games, applications, TV/films, and OS images.
        /// </summary>
        /// <returns>A task representing the asynchronous scanning operation.</returns>
        [RelayCommand]
        private async Task ScanLibraryAsync()
        {
            try
            {
                IsScanning = true;
                _allGames.Clear();
                _allApps.Clear();
                _allTvAndFilms.Clear();
                _allOsImages.Clear();
                Games.Clear();
                Apps.Clear();
                TvAndFilms.Clear();
                OsImages.Clear();
                ValidationMessages.Clear();

                if (_scanCancellationTokenSource != null)
                {
                    await _scanCancellationTokenSource.CancelAsync();
                    _scanCancellationTokenSource.Dispose();
                }
                _scanCancellationTokenSource = new CancellationTokenSource();

                AppSettings settings = await _settingsService.LoadSettingsAsync();
                _cachedSettings = settings;

                if (settings.GameSourceFolders.Count == 0 && settings.AppSourceFolders.Count == 0 && (settings.TvAndFilmSourceFolders == null || settings.TvAndFilmSourceFolders.Count == 0) && (settings.OsImageSourceFolders == null || settings.OsImageSourceFolders.Count == 0))
                {
                    await _libraryCacheService.InvalidateCacheAsync();
                    StatusMessage = "No source folders configured. Please add folders in Settings.";
                    return;
                }

                Progress<string> progress = new(message =>
                {
                    StatusMessage = message;
                });

                (IReadOnlyList<GameEntry> Games, IReadOnlyList<GameEntry> Apps, IReadOnlyList<GameEntry> TvAndFilms, IReadOnlyList<GameEntry> OsImages) scanResult = await _libraryScannerService.ScanAllLibrariesAsync(
                    settings,
                    progress,
                    _scanCancellationTokenSource.Token);

                _allGames.AddRange(scanResult.Games);
                _allApps.AddRange(scanResult.Apps);
                _allTvAndFilms.AddRange(scanResult.TvAndFilms);
                _allOsImages.AddRange(scanResult.OsImages);

                ApplyFilter();

                StatusMessage = _allGames.Count == 0 && _allApps.Count == 0 && _allTvAndFilms.Count == 0 && _allOsImages.Count == 0
                    ? "No items found in configured folders"
                    : $"Found {_allGames.Count} game(s), {_allApps.Count} app(s), {_allTvAndFilms.Count} film/TV(s), {_allOsImages.Count} OS image(s)";

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

        /// <summary>
        /// Saves a library cache JSON snapshot of all discovered entries using the active settings.
        /// </summary>
        /// <param name="settings">The application settings to save with the snapshot.</param>
        /// <returns>A task representing the asynchronous snapshot saving operation.</returns>
        private async Task SaveCacheSnapshotAsync(AppSettings settings)
        {
            List<GameEntry> allEntries = [.. _allGames, .. _allApps, .. _allTvAndFilms, .. _allOsImages];
            string? resultMessage = await _libraryCacheService.CreateAndSaveSnapshotAsync(allEntries, settings);
            if (!string.IsNullOrEmpty(resultMessage))
            {
                StatusMessage = resultMessage;
            }
        }

        /// <summary>
        /// Handles changes to the SearchText property to re-apply library filtering.
        /// Uses nullable string? for oldValue to match the CommunityToolkit.Mvvm partial method declaration for reference types.
        /// </summary>
        /// <param name="oldValue">The previous search string.</param>
        /// <param name="newValue">The new search string.</param>
        partial void OnSearchTextChanged(string oldValue, string newValue) => ApplyFilter();

        /// <summary>
        /// Handles changes to the SelectedCategory property to re-apply library filtering.
        /// </summary>
        /// <param name="oldValue">The previous category value.</param>
        /// <param name="newValue">The new category value.</param>
        partial void OnSelectedCategoryChanged(GameCategory oldValue, GameCategory newValue) => ApplyFilter();

        /// <summary>
        /// Handles changes to the SelectedOsImageSortOption property to re-apply OS image sorting.
        /// </summary>
        /// <param name="oldValue">The previous sort option.</param>
        /// <param name="newValue">The new sort option.</param>
        partial void OnSelectedOsImageSortOptionChanged(OsImageSortOption oldValue, OsImageSortOption newValue)
        {
            IsOsImageSortAscending = newValue switch
            {
                OsImageSortOption.Name => true,
                OsImageSortOption.DateCreated => false,
                OsImageSortOption.Size => false,
                _ => true
            };
            ApplyFilter();
        }

        /// <summary>
        /// Handles changes to the IsOsImageSortAscending property to re-apply OS image sorting.
        /// </summary>
        /// <param name="oldValue">The previous sort direction value.</param>
        /// <param name="newValue">The new sort direction value.</param>
        partial void OnIsOsImageSortAscendingChanged(bool oldValue, bool newValue) => ApplyFilter();

        /// <summary>
        /// Toggles the OS image sorting direction between ascending and descending.
        /// </summary>
        [RelayCommand]
        private void ToggleOsImageSortDirection()
        {
            IsOsImageSortAscending = !IsOsImageSortAscending;
        }

        /// <summary>
        /// Filters and sorts active library collections based on current search query, category, and OS image sort parameters.
        /// </summary>
        private void ApplyFilter()
        {
            Games.UpdateFrom(_libraryFilterService.FilterEntries(_allGames, SearchText, SelectedCategory));
            Apps.UpdateFrom(_libraryFilterService.FilterEntries(_allApps, SearchText, SelectedCategory));
            TvAndFilms.UpdateFrom(_libraryFilterService.FilterEntries(_allTvAndFilms, SearchText, SelectedCategory));
            OsImages.UpdateFrom(_libraryFilterService.SortOsImages(
                _libraryFilterService.FilterEntries(_allOsImages, SearchText, SelectedCategory),
                SelectedOsImageSortOption,
                IsOsImageSortAscending));

            OnPropertyChanged(nameof(IsGamesEmpty));
            OnPropertyChanged(nameof(IsAppsEmpty));
            OnPropertyChanged(nameof(IsTvAndFilmsEmpty));
            OnPropertyChanged(nameof(IsOsImagesEmpty));
        }

        /// <summary>
        /// Asynchronously queries connected removable storage devices and updates <see cref="MainViewModel.AvailableDrives"/>.
        /// </summary>
        /// <returns>A task representing the asynchronous drive discovery operation.</returns>
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


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCustomDestination))]
        public partial string? CustomDestinationPath { get; set; }

        public bool HasCustomDestination => !string.IsNullOrEmpty(CustomDestinationPath);

        [RelayCommand]
        private async Task SelectCustomDestinationAsync()
        {
            if (SelectedDrive == null)
            {
                return;
            }

            string? selectedFolder = await _folderPickerService.PickFolderAsync($"{SelectedDrive.DriveLetter}\\");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                // Verify the selected folder is on the selected drive
                string rootDrive = Path.GetPathRoot(selectedFolder) ?? string.Empty;
                if (!rootDrive.Equals($"{SelectedDrive.DriveLetter}\\", StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = $"Selected destination must be on the target drive ({SelectedDrive.DriveLetter}).";
                    return;
                }
                CustomDestinationPath = selectedFolder;
            }
        }

        [RelayCommand]
        private void ClearCustomDestination()
        {
            CustomDestinationPath = null;
        }

        /// <summary>
        /// Validates selected items and target drive space, resolves folder conflicts, and enqueues new transfer jobs into the queue.
        /// </summary>
        /// <returns>A task representing the asynchronous copy queuing operation.</returns>
        [RelayCommand(CanExecute = nameof(CanCopyGames))]
        private async Task CopySelectedGamesAsync()
        {
            if (SelectedDrive == null || _selectedGames.Count == 0)
            {
                return;
            }

            try
            {
                string destinationPath = CustomDestinationPath ?? $"{SelectedDrive.DriveLetter}\\";

                RemovableDrive driveForValidation = GetDriveForValidation(SelectedDrive);

                IReadOnlyList<ValidationResult> validation = await _driveValidationService.ValidateTransferAsync(
                    _selectedGames, driveForValidation, destinationPath);

                ValidationMessages.UpdateFrom(validation);

                if (validation.Any(v => v.Severity == ValidationSeverity.Error))
                {
                    StatusMessage = "Cannot queue transfer: validation failed. See warnings.";
                    return;
                }

                List<TransferItem> itemsToQueue = await BuildItemsToQueueAsync(destinationPath);

                if (itemsToQueue.Count > 0)
                {
                    _ = _transferQueueService.Enqueue(itemsToQueue, SelectedDrive, destinationPath);
                    StatusMessage = $"Queued {itemsToQueue.Count} item(s) for {SelectedDrive.DriveLetter} ({TransferQueue.Count} in queue)";
                    IsTransferring = TransferQueue.Any(i => i.IsActive);
                    ItemQueued?.Invoke(this, EventArgs.Empty);
                    ClearCustomDestination();
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

        /// <summary>
        /// Calculates remaining free drive bytes after subtracting currently reserved queue byte allocations for validation.
        /// </summary>
        /// <param name="drive">The target drive to adjust.</param>
        /// <returns>An updated <see cref="RemovableDrive"/> instance with adjusted free byte capacity.</returns>
        private RemovableDrive GetDriveForValidation(RemovableDrive drive)
        {
            long reservedBytes = _transferQueueService.GetReservedBytes(drive.DriveLetter);
            return reservedBytes > 0
                ? drive with { FreeBytes = Math.Max(0, drive.FreeBytes - reservedBytes) }
                : drive;
        }

        /// <summary>
        /// Builds the list of transfer items to queue, prompting the user for conflict resolution decisions when target files or folders exist.
        /// </summary>
        /// <param name="destinationPath">The destination root directory path on the target volume.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of <see cref="TransferItem"/> instances to queue.</returns>
        private async Task<List<TransferItem>> BuildItemsToQueueAsync(string destinationPath)
        {
            List<TransferItem> itemsToQueue = [];
            bool applyToAll = false;
            CopyAction globalAction = CopyAction.Default;

            foreach (GameEntry game in _selectedGames)
            {
                string destItemPath = _fileSystemService.FileExists(game.FolderPath)
                    ? Path.Combine(destinationPath, Path.GetFileName(game.FolderPath))
                    : Path.Combine(destinationPath, game.Name);

                bool destExists = _fileSystemService.DirectoryExists(destItemPath) || _fileSystemService.FileExists(destItemPath);

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

            return itemsToQueue;
        }

        /// <summary>
        /// Displays a global in-app notification bar with the specified title and message.
        /// </summary>
        /// <param name="title">The title text of the notification.</param>
        /// <param name="message">The body text message of the notification.</param>
        /// <param name="isSuccess"><see langword="true"/> to display a success notification; <see langword="false"/> to display an error notification.</param>
        public void ShowGlobalNotification(string title, string message, bool isSuccess = true)
        {
            _ = _dispatcherService.TryEnqueue(async () =>
            {
                GlobalNotificationTitle = title;
                GlobalNotificationMessage = message;
                IsGlobalNotificationVisible = true;
                GlobalNotificationSeverity = isSuccess ? ValidationSeverity.Success : ValidationSeverity.Error;

                if (_notificationCancellationTokenSource != null)
                {
                    await _notificationCancellationTokenSource.CancelAsync();
                    _notificationCancellationTokenSource.Dispose();
                }
                _notificationCancellationTokenSource = new CancellationTokenSource();

                CancellationToken token = _notificationCancellationTokenSource.Token;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), token);
                    if (!token.IsCancellationRequested)
                    {
                        IsGlobalNotificationVisible = false;
                    }
                }
                catch (TaskCanceledException)
                {
                    // Ignore, another notification replaced this one
                }
            });
        }

        /// <summary>
        /// Event handler triggered when a transfer queue batch operation completes to display a global notification.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">Batch completion arguments containing title, message, and success status.</param>
        private void OnBatchCompleted(object? sender, (string Title, string Message, bool IsSuccess) args)
        {
            if (IsDisposed)
            {
                return;
            }

            ShowGlobalNotification(args.Title, args.Message, args.IsSuccess);
        }

        /// <summary>
        /// Event handler triggered when a single queue item finishes transferring.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="completedItem">The completed queue item details.</param>
        private void OnQueueItemCompleted(object? sender, TransferQueueItem completedItem)
        {
            if (IsDisposed)
            {
                return;
            }

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

        /// <summary>
        /// Clears all completed, failed, or cancelled items from the transfer queue.
        /// </summary>
        [RelayCommand]
        private void ClearFinishedQueueItems()
        {
            _transferQueueService.ClearFinished();
        }

        /// <summary>
        /// Determines whether the copy command can execute based on selection and target drive availability.
        /// </summary>
        /// <returns><c>true</c> if at least one item is selected and a target drive is chosen; otherwise, <c>false</c>.</returns>
        private bool CanCopyGames()
        {
            return SelectedGamesCount > 0 && SelectedDrive != null;
        }

        /// <summary>
        /// Updates selected game entries and recalculates size, item count, and price summary values.
        /// </summary>
        /// <param name="selectedGames">The collection of selected library entries.</param>
        public void UpdateSelectionSummary(System.Collections.Generic.IEnumerable<GameEntry> selectedGames)
        {
            _selectedGames = [.. selectedGames];
            SelectedGamesCount = _selectedGames.Count;
            SelectedGamesTotalBytes = _selectedGames.Sum(g => g.TotalBytes);

            AppSettings settings = _cachedSettings ??= _settingsService.LoadSettingsSync();
            SelectedGamesTotalPrice = _selectedGames.Sum(g => Infrastructure.FormattingHelpers.CalculatePrice(g.TotalBytes, settings));

            CopySelectedGamesCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Displays the settings window.
        /// </summary>
        [RelayCommand]
        private void OpenSettings()
        {
            _windowService.ShowSettingsWindow(async () => await ScanLibraryCommand.ExecuteAsync(null));
        }

        /// <summary>
        /// Displays the operation history window.
        /// </summary>
        [RelayCommand]
        private void OpenHistory()
        {
            _windowService.ShowHistoryWindow();
        }

        /// <summary>
        /// Displays the application About dialog window.
        /// </summary>
        [RelayCommand]
        private void OpenAbout()
        {
            _windowService.ShowAboutWindow();
        }

        /// <summary>
        /// Triggers the <see cref="ClearSelectionRequested"/> event to unselect active items in library tabs.
        /// </summary>
        [RelayCommand]
        private void ClearSelection()
        {
            ClearSelectionRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Opens the root directory of the selected drive in Windows File Explorer.
        /// </summary>
        [RelayCommand]
        private void OpenDriveInExplorer()
        {
            if (SelectedDrive != null)
            {
                _processService.OpenInExplorer($"{SelectedDrive.DriveLetter}\\");
            }
        }

        /// <summary>
        /// Opens the Windows native format dialog for the selected drive letter.
        /// </summary>
        [RelayCommand]
        private void FormatDrive()
        {
            if (SelectedDrive != null)
            {
                _processService.OpenFormatDialog(SelectedDrive.DriveLetter);
            }
        }

        /// <summary>
        /// Opens the specified item folder path in Windows File Explorer.
        /// </summary>
        /// <param name="folderPath">The target directory path to open.</param>
        [RelayCommand]
        private void OpenItemFolder(string folderPath)
        {
            if (!string.IsNullOrEmpty(folderPath))
            {
                _processService.OpenInExplorer(folderPath);
            }
        }

        /// <summary>
        /// Launches Rufus with the selected OS image ISO file.
        /// </summary>
        /// <returns>A task representing the asynchronous Rufus launch operation.</returns>
        [RelayCommand]
        private async Task OpenInRufusAsync()
        {
            if (_selectedGames.Count != 1)
            {
                StatusMessage = "Please select exactly one OS image.";
                return;
            }

            GameEntry selectedImage = _selectedGames[0];
            (bool _, string message) = await _rufusService.LaunchWithIsoAsync(selectedImage.FolderPath);
            StatusMessage = message;
        }

        /// <summary>
        /// Asynchronously retrieves and formats system requirements for a specified game item directory.
        /// </summary>
        /// <param name="folderPath">The file system directory path to inspect for system requirements.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the formatted system requirements text string.</returns>
        public async Task<string> GetFormattedSystemRequirementsAsync(string folderPath)
        {
            return await _gameRequirementsService.GetFormattedRequirementsAsync(folderPath);
        }

        /// <summary>
        /// Opens the settings window configured to add a new source folder for the active tab category.
        /// </summary>
        [RelayCommand]
        private void AddSourceFolder()
        {
            SettingsOpenAction openAction = CurrentTabIndex == 0 ? Infrastructure.SettingsOpenAction.AddGameFolder :
                                            CurrentTabIndex == 1 ? Infrastructure.SettingsOpenAction.AddAppFolder :
                                            CurrentTabIndex == 2 ? Infrastructure.SettingsOpenAction.AddTvAndFilmFolder :
                                            Infrastructure.SettingsOpenAction.AddOsImageFolder;

            _windowService.ShowSettingsWindow(null, openAction);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            // Stop external notifications first so teardown does not race queued UI work.
            _driveDiscoveryService.DrivesChanged -= OnDrivesChanged;
            _transferQueueService.ItemCompleted -= OnQueueItemCompleted;
            _transferQueueService.BatchCompleted -= OnBatchCompleted;
            _driveDiscoveryService.StopWatching();

            _ = (_updateCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite));
            _updateCheckTimer?.Dispose();
            _updateCheckTimer = null;

            if (_scanCancellationTokenSource != null)
            {
                _scanCancellationTokenSource.Cancel();
                _scanCancellationTokenSource.Dispose();
                _scanCancellationTokenSource = null;
            }

            if (_validationCancellationTokenSource != null)
            {
                _validationCancellationTokenSource.Cancel();
                _validationCancellationTokenSource.Dispose();
                _validationCancellationTokenSource = null;
            }

            if (_notificationCancellationTokenSource != null)
            {
                _notificationCancellationTokenSource.Cancel();
                _notificationCancellationTokenSource.Dispose();
                _notificationCancellationTokenSource = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Checks for application updates in the background and optionally triggers automatic download if enabled.
        /// </summary>
        /// <returns>A task representing the asynchronous background update check.</returns>
        private async Task CheckForUpdatesBackgroundAsync()
        {
            if (IsDisposed || _isCheckingForUpdates)
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
                        _ = TryEnqueueIfActive(() =>
                        {
                            IsUpdateAvailable = true;
                            UpdateMessage = "Downloading update in background...";
                        });

                        await _updateService.DownloadUpdateAsync();

                        _logger.LogInformation("Background update download completed. Update is ready to install.");
                        _ = TryEnqueueIfActive(() =>
                        {
                            IsUpdateAvailable = false;
                            IsUpdateReadyToInstall = true;
                            UpdateMessage = "Update ready to install. Restart to apply.";
                        });
                    }
                    else
                    {
                        _logger.LogInformation("Automatic update download is disabled. Notifying user.");
                        _ = TryEnqueueIfActive(() =>
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

        /// <summary>
        /// Initiates manual download of an available application update.
        /// </summary>
        /// <returns>A task representing the asynchronous update download operation.</returns>
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
