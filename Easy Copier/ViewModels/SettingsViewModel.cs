using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Models;
using Easy_Copier.Services;
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

        public ObservableCollection<string> SourceFolders { get; } = new();

        public SettingsViewModel(
            ISettingsService settingsService,
            IFolderPickerService folderPickerService,
            ISourceLibraryService sourceLibraryService)
        {
            _settingsService = settingsService;
            _folderPickerService = folderPickerService;
            _sourceLibraryService = sourceLibraryService;
        }

        [RelayCommand]
        private async Task AddSourceFolderAsync()
        {
            StatusMessage = "Opening folder picker...";

            var folderPath = await _folderPickerService.PickFolderAsync();

            if (!string.IsNullOrEmpty(folderPath))
            {
                if (!SourceFolders.Contains(folderPath))
                {
                    SourceFolders.Add(folderPath);
                    StatusMessage = $"Added: {folderPath}";
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
        private async Task RemoveSourceFolderAsync(string folderPath)
        {
            SourceFolders.Remove(folderPath);
            StatusMessage = $"Removed: {folderPath}";
            await SaveSettingsAsync();
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

            var validatedFolders = await _sourceLibraryService.ValidateSourceFoldersAsync(settings.SourceFolders);
            var invalidCount = validatedFolders.Count(f => !f.IsValid);

            if (invalidCount > 0)
            {
                StatusMessage = $"Warning: {invalidCount} source folder(s) are not accessible";
            }
        }

        public void LoadSettings(AppSettings settings)
        {
            AutoScanOnStartup = settings.AutoScanOnStartup;
            SourceFolders.Clear();
            foreach (var folder in settings.SourceFolders)
            {
                SourceFolders.Add(folder);
            }
        }

        public AppSettings GetSettings()
        {
            return new AppSettings
            {
                AutoScanOnStartup = AutoScanOnStartup,
                SourceFolders = new System.Collections.Generic.List<string>(SourceFolders)
            };
        }
    }
}
