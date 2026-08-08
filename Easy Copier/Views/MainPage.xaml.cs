using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Easy_Copier.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel ViewModel { get; }

        public bool IsGamesEmpty => !ViewModel.IsScanning && ViewModel.Games.Count == 0;
        public bool IsAppsEmpty => !ViewModel.IsScanning && ViewModel.Apps.Count == 0;
        public string EmptyGamesMessage => string.IsNullOrWhiteSpace(ViewModel.SearchText)
            ? "No games found. Add a game folder in Settings and scan your library."
            : $"No games match \"{ViewModel.SearchText}\".";
        public string EmptyAppsMessage => string.IsNullOrWhiteSpace(ViewModel.SearchText)
            ? "No apps found. Add an app folder in Settings and scan your library."
            : $"No apps match \"{ViewModel.SearchText}\".";
        public bool HasSelectedDrive => ViewModel.SelectedDrive != null;
        public string DriveSpaceSummary => ViewModel.SelectedDrive == null
            ? string.Empty
            : $"{FormattingHelpers.FormatBytes(ViewModel.SelectedDrive.FreeBytes)} free of {FormattingHelpers.FormatBytes(ViewModel.SelectedDrive.TotalBytes)}";
        public string DriveDetailsSummary => ViewModel.SelectedDrive == null
            ? string.Empty
            : $"{ViewModel.SelectedDrive.DriveLetter} \u2022 {ViewModel.SelectedDrive.Brand} \u2022 {ViewModel.SelectedDrive.FileSystem}";
        public string SelectionSummary => ViewModel.SelectedGamesCount == 0
            ? "No items selected"
            : $"{ViewModel.SelectedGamesCount} item(s) selected \u2022 {FormattingHelpers.FormatBytes(ViewModel.SelectedGamesTotalBytes)}";

        public MainPage()
        {
            ViewModel = AppServiceLocator.GetService<MainViewModel>();
            InitializeComponent();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.Games.CollectionChanged += (s, e) => NotifyGamesStateChanged();
            ViewModel.Apps.CollectionChanged += (s, e) => NotifyAppsStateChanged();
            ViewModel.ItemQueued += (s, e) => ClearGameSelection();

            _ = ViewModel.InitializeAsync();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.IsScanning):
                    NotifyGamesStateChanged();
                    NotifyAppsStateChanged();
                    break;
                case nameof(MainViewModel.SelectedDrive):
                    NotifyPropertyChanged(nameof(HasSelectedDrive));
                    NotifyPropertyChanged(nameof(DriveSpaceSummary));
                    NotifyPropertyChanged(nameof(DriveDetailsSummary));
                    break;
                case nameof(MainViewModel.SelectedGamesCount):
                case nameof(MainViewModel.SelectedGamesTotalBytes):
                    NotifyPropertyChanged(nameof(SelectionSummary));
                    break;
                case nameof(MainViewModel.SearchText):
                    NotifyPropertyChanged(nameof(EmptyGamesMessage));
                    NotifyPropertyChanged(nameof(EmptyAppsMessage));
                    break;
            }
        }

        private void NotifyGamesStateChanged()
        {
            NotifyPropertyChanged(nameof(IsGamesEmpty));
        }

        private void NotifyAppsStateChanged()
        {
            NotifyPropertyChanged(nameof(IsAppsEmpty));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AddSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            var windowService = AppServiceLocator.GetService<IWindowService>();

            windowService.ShowSettingsWindow(null, async (settingsViewModel) =>
            {
                if (LibraryPivot.SelectedIndex == 1)
                {
                    await settingsViewModel.AddAppSourceFolderCommand.ExecuteAsync(null);
                }
                else
                {
                    await settingsViewModel.AddGameSourceFolderCommand.ExecuteAsync(null);
                }
            });
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            ClearGameSelection();
        }

        private void OpenHistory_Click(object sender, RoutedEventArgs e)
        {
            var windowService = AppServiceLocator.GetService<IWindowService>();
            windowService.ShowHistoryWindow();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var windowService = AppServiceLocator.GetService<IWindowService>();
            windowService.ShowSettingsWindow(async () => await ViewModel.ScanLibraryCommand.ExecuteAsync(null));
        }

        private void GamesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void AppsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void UpdateCombinedSelection()
        {
            var selectedItems = GamesGridView.SelectedItems.Cast<GameEntry>()
                .Concat(AppsGridView.SelectedItems.Cast<GameEntry>());
            ViewModel.UpdateSelectionSummary(selectedItems);
        }

        private void ClearGameSelection()
        {
            GamesGridView.SelectedItems.Clear();
            AppsGridView.SelectedItems.Clear();
        }

        private void OpenDriveInExplorer_Click(object sender, RoutedEventArgs e)
        {
            var drive = ViewModel.SelectedDrive;
            if (drive == null)
                return;

            var processService = AppServiceLocator.GetService<IProcessService>();
            processService.OpenInExplorer($"{drive.DriveLetter}\\");
        }

        private void OpenItemFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string folderPath)
            {
                var processService = AppServiceLocator.GetService<IProcessService>();
                processService.OpenInExplorer(folderPath);
            }
        }

    }
}
