using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IFolderPickerService _folderPickerService;
        private readonly ISourceLibraryService _sourceLibraryService;

        [ObservableProperty]
        private bool _autoScanOnStartup = true;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public ObservableCollection<string> GameSourceFolders { get; } = new();
        public ObservableCollection<string> AppSourceFolders { get; } = new();

        private readonly Infrastructure.IProcessService _processService;

        public SettingsViewModel(
            ISettingsService settingsService,
            IFolderPickerService folderPickerService,
            ISourceLibraryService sourceLibraryService,
            Infrastructure.IProcessService processService)
        {
            _settingsService = settingsService;
            _folderPickerService = folderPickerService;
            _sourceLibraryService = sourceLibraryService;
            _processService = processService;
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

            var folderPath = await _folderPickerService.PickFolderAsync();

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
            GameSourceFolders.Remove(folderPath);
            StatusMessage = $"Removed: {folderPath}";
            await SaveSettingsAsync();
        }

        [RelayCommand]
        private async Task RemoveAppSourceFolderAsync(string folderPath)
        {
            AppSourceFolders.Remove(folderPath);
            StatusMessage = $"Removed: {folderPath}";
            await SaveSettingsAsync();
        }

        [RelayCommand]
        private void OpenDataFolder()
        {
            var folderPath = System.IO.Path.GetDirectoryName(_settingsService.GetSettingsFilePath());

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

            var settings = GetSettings();
            await _settingsService.SaveSettingsAsync(settings);

            StatusMessage = "Settings saved";
        }

        public async Task LoadSettingsAsync()
        {
            var settings = await _settingsService.LoadSettingsAsync();
            LoadSettings(settings);

            var validatedGameFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.GameSourceFolders);
            var validatedAppFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.AppSourceFolders);
            var invalidCount = validatedGameFolders.Count(f => !f.IsValid) + validatedAppFolders.Count(f => !f.IsValid);

            if (invalidCount > 0)
            {
                StatusMessage = $"Warning: {invalidCount} source folder(s) are not accessible";
            }
        }

        public void LoadSettings(AppSettings settings)
        {
            AutoScanOnStartup = settings.AutoScanOnStartup;

            GameSourceFolders.Clear();
            foreach (var folder in settings.GameSourceFolders)
            {
                GameSourceFolders.Add(folder);
            }

            AppSourceFolders.Clear();
            foreach (var folder in settings.AppSourceFolders)
            {
                AppSourceFolders.Add(folder);
            }
        }

        public AppSettings GetSettings()
        {
            return new AppSettings
            {
                AutoScanOnStartup = AutoScanOnStartup,
                GameSourceFolders = new List<string>(GameSourceFolders),
                AppSourceFolders = new List<string>(AppSourceFolders)
            };
        }
    }
}
