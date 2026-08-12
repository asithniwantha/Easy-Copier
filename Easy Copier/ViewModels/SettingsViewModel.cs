using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Easy_Copier.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IFolderPickerService _folderPickerService;
        private readonly ISourceLibraryService _sourceLibraryService;
        private readonly IGameInfoDownloadService _gameInfoDownloadService;

        [ObservableProperty]
        public partial bool AutoScanOnStartup { get; set; } = true;

        [ObservableProperty]
        public partial string PriceTier1 { get; set; } = "100";

        [ObservableProperty]
        public partial string PriceTier2 { get; set; } = "200";

        [ObservableProperty]
        public partial string PriceTier3 { get; set; } = "300";

        [ObservableProperty]
        public partial string PriceTier4 { get; set; } = "400";

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        public ObservableCollection<string> GameSourceFolders { get; } = [];
        public ObservableCollection<string> AppSourceFolders { get; } = [];

        private readonly Infrastructure.IProcessService _processService;

        public SettingsViewModel(
            ISettingsService settingsService,
            IFolderPickerService folderPickerService,
            ISourceLibraryService sourceLibraryService,
            Infrastructure.IProcessService processService,
            IGameInfoDownloadService gameInfoDownloadService)
        {
            _settingsService = settingsService;
            _folderPickerService = folderPickerService;
            _sourceLibraryService = sourceLibraryService;
            _processService = processService;
            _gameInfoDownloadService = gameInfoDownloadService;
        }

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
                var progress = new Progress<string>(msg =>
                {
                    var dispatcher = AppServiceLocator.GetService<IDispatcherService>();
                    dispatcher.TryEnqueue(() => StatusMessage = msg);
                });

                await _gameInfoDownloadService.DownloadGameInfoAsync(GameSourceFolders, progress, CancellationToken.None);

                StatusMessage = "Game info download complete";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error downloading game info: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddGameSourceFolderAsync()
        {
            await AddSourceFolderAsync(GameSourceFolders, "game");
        }

        [RelayCommand]
        private async Task AddAppSourceFolderAsync()
        {
            await AddSourceFolderAsync(AppSourceFolders, "app");
        }

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

        [RelayCommand]
        private async Task RemoveGameSourceFolderAsync(string folderPath)
        {
            _ = GameSourceFolders.Remove(folderPath);
            StatusMessage = $"Removed: {folderPath}";
            await SaveSettingsAsync();
        }

        [RelayCommand]
        private async Task RemoveAppSourceFolderAsync(string folderPath)
        {
            _ = AppSourceFolders.Remove(folderPath);
            StatusMessage = $"Removed: {folderPath}";
            await SaveSettingsAsync();
        }

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

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            StatusMessage = "Saving settings...";

            AppSettings settings = GetSettings();
            await _settingsService.SaveSettingsAsync(settings);

            StatusMessage = "Settings saved";
        }

        public async Task LoadSettingsAsync()
        {
            AppSettings settings = await _settingsService.LoadSettingsAsync();
            LoadSettings(settings);

            IReadOnlyList<SourceFolder> validatedGameFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.GameSourceFolders);
            IReadOnlyList<SourceFolder> validatedAppFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.AppSourceFolders);
            int invalidCount = validatedGameFolders.Count(f => !f.IsValid) + validatedAppFolders.Count(f => !f.IsValid);

            if (invalidCount > 0)
            {
                StatusMessage = $"Warning: {invalidCount} source folder(s) are not accessible";
            }
        }

        public void LoadSettings(AppSettings settings)
        {
            AutoScanOnStartup = settings.AutoScanOnStartup;
            PriceTier1 = settings.PriceTier1.ToString();
            PriceTier2 = settings.PriceTier2.ToString();
            PriceTier3 = settings.PriceTier3.ToString();
            PriceTier4 = settings.PriceTier4.ToString();

            GameSourceFolders.UpdateFrom(settings.GameSourceFolders);
            AppSourceFolders.UpdateFrom(settings.AppSourceFolders);
        }

        public AppSettings GetSettings()
        {
            return new AppSettings
            {
                AutoScanOnStartup = AutoScanOnStartup,
                PriceTier1 = int.TryParse(PriceTier1, out int p1) ? p1 : 100,
                PriceTier2 = int.TryParse(PriceTier2, out int p2) ? p2 : 200,
                PriceTier3 = int.TryParse(PriceTier3, out int p3) ? p3 : 300,
                PriceTier4 = int.TryParse(PriceTier4, out int p4) ? p4 : 400,
                GameSourceFolders = [.. GameSourceFolders],
                AppSourceFolders = [.. AppSourceFolders]
            };
        }
    }
}
