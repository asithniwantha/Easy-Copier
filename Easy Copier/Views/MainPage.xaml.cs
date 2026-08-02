using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Easy_Copier.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Easy_Copier.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public bool IsLibraryEmpty => !ViewModel.IsScanning && ViewModel.Games.Count == 0;
        public bool HasSelectedDrive => ViewModel.SelectedDrive != null;
        public string DriveSpaceSummary => ViewModel.SelectedDrive == null
            ? string.Empty
            : $"{FormatBytes(ViewModel.SelectedDrive.FreeBytes)} free of {FormatBytes(ViewModel.SelectedDrive.TotalBytes)}";
        public string DriveDetailsSummary => ViewModel.SelectedDrive == null
            ? string.Empty
            : $"{ViewModel.SelectedDrive.DriveLetter} \u2022 {ViewModel.SelectedDrive.Brand} \u2022 {ViewModel.SelectedDrive.FileSystem}";
        public string SelectionSummary => ViewModel.SelectedGamesCount == 0
            ? "No games selected"
            : $"{ViewModel.SelectedGamesCount} game(s) selected \u2022 {FormatBytes(ViewModel.SelectedGamesTotalBytes)}";

        public MainPage()
        {
            ViewModel = AppServiceLocator.GetService<MainViewModel>();
            InitializeComponent();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.Games.CollectionChanged += (s, e) => NotifyLibraryStateChanged();
            ViewModel.TransferCompleted += (s, e) => ClearGameSelection();

            _ = ViewModel.InitializeAsync();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.IsScanning):
                    NotifyLibraryStateChanged();
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
            }
        }

        private void NotifyLibraryStateChanged()
        {
            NotifyPropertyChanged(nameof(IsLibraryEmpty));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            Bindings.Update();
        }

        private async void AddSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Activate();
            await settingsWindow.ViewModel.AddSourceFolderCommand.ExecuteAsync(null);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            GamesGridView.SelectAll();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.SettingsClosed += async (s, args) => await ViewModel.ScanLibraryCommand.ExecuteAsync(null);
            settingsWindow.Activate();
        }

        private void GamesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedGames = GamesGridView.SelectedItems.Cast<GameEntry>();
            ViewModel.UpdateSelectionSummary(selectedGames);
        }

        private void ClearGameSelection()
        {
            GamesGridView.SelectedItems.Clear();
        }

        private void OpenDriveInExplorer_Click(object sender, RoutedEventArgs e)
        {
            var drive = ViewModel.SelectedDrive;
            if (drive == null)
                return;

            try
            {
                var path = $"{drive.DriveLetter}\\";
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                // Ignore failures opening Explorer (e.g. drive was removed).
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
