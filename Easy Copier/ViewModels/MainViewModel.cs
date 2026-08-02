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
        private readonly IGameScannerService _gameScannerService;
        private readonly IDriveDiscoveryService _driveDiscoveryService;
        private readonly IDriveValidationService _driveValidationService;
        private readonly IFileTransferService _fileTransferService;
        private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
        private CancellationTokenSource? _scanCancellationTokenSource;
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

        public ObservableCollection<GameEntry> Games { get; } = new();
        public ObservableCollection<RemovableDrive> AvailableDrives { get; } = new();
        public ObservableCollection<ValidationResult> ValidationMessages { get; } = new();

        public event EventHandler? TransferCompleted;

        public MainViewModel(
            ISettingsService settingsService,
            IGameScannerService gameScannerService,
            IDriveDiscoveryService driveDiscoveryService,
            IDriveValidationService driveValidationService,
            IFileTransferService fileTransferService)
        {
            _settingsService = settingsService;
            _gameScannerService = gameScannerService;
            _driveDiscoveryService = driveDiscoveryService;
            _driveValidationService = driveValidationService;
            _fileTransferService = fileTransferService;
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

                if (settings.AutoScanOnStartup && settings.SourceFolders.Any())
                {
                    await ScanLibraryAsync();
                }
                else
                {
                    StatusMessage = "Ready - No source folders configured";
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

        [RelayCommand]
        private async Task ScanLibraryAsync()
        {
            try
            {
                IsScanning = true;
                Games.Clear();
                ValidationMessages.Clear();

                _scanCancellationTokenSource?.Cancel();
                _scanCancellationTokenSource = new CancellationTokenSource();

                var settings = await _settingsService.LoadSettingsAsync();

                if (!settings.SourceFolders.Any())
                {
                    StatusMessage = "No source folders configured. Please add folders in Settings.";
                    return;
                }

                var progress = new Progress<string>(message =>
                {
                    StatusMessage = message;
                });

                var games = await _gameScannerService.ScanLibraryAsync(
                    settings.SourceFolders,
                    progress,
                    _scanCancellationTokenSource.Token);

                foreach (var game in games)
                {
                    Games.Add(game);
                }

                if (Games.Count == 0)
                {
                    StatusMessage = "No games found in configured folders";
                }
                else
                {
                    StatusMessage = $"Found {Games.Count} games";
                }

                settings.LastScanTime = DateTime.Now;
                await _settingsService.SaveSettingsAsync(settings);
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
                var destinationPath = Path.Combine($"{SelectedDrive.DriveLetter}\\", "EasyCopier_Games");

                var validation = await _driveValidationService.ValidateTransferAsync(
                    _selectedGames, SelectedDrive, destinationPath);

                foreach (var result in validation)
                {
                    ValidationMessages.Add(result);
                }

                if (validation.Any(v => v.Severity == ValidationSeverity.Error))
                {
                    StatusMessage = "Cannot copy: validation failed. See warnings.";
                    return;
                }

                IsTransferring = true;
                StatusMessage = $"Copying {SelectedGamesCount} games to {SelectedDrive.DriveLetter}...";

                var request = new TransferRequest(_selectedGames, SelectedDrive, destinationPath);
                var outcome = await _fileTransferService.TransferGamesAsync(request);

                StatusMessage = outcome.Message;

                if (outcome.Success)
                {
                    await RefreshDrivesAsync();
                    TransferCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Transfer error: {ex.Message}";
            }
            finally
            {
                IsTransferring = false;
            }
        }

        private bool CanCopyGames()
        {
            return SelectedGamesCount > 0 && SelectedDrive != null && !IsTransferring;
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
