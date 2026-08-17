using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Views
{
    public sealed partial class SettingsWindow : Window
    {
        public SettingsViewModel ViewModel { get; }
        public event EventHandler? SettingsClosed;

        public SettingsWindow(SettingsViewModel viewModel, SettingsOpenAction openAction = SettingsOpenAction.None)
        {
            ViewModel = viewModel;
            InitializeComponent();

            NativeWindowHelper.InitializeWindow(this, 960, 640);
            NativeWindowHelper.ShowAsModal(this, App.MainWindow);

            Closed += SettingsWindow_Closed;

            _ = LoadAsync(openAction);
        }

        private void SettingsWindow_Closed(object sender, WindowEventArgs args)
        {
            NativeWindowHelper.RestoreOwnerInput(App.MainWindow);
        }

        private async Task LoadAsync(SettingsOpenAction openAction)
        {
            await ViewModel.LoadSettingsAsync();
            if (openAction == SettingsOpenAction.AddAppFolder)
            {
                await ViewModel.AddAppSourceFolderCommand.ExecuteAsync(null);
            }
            else if (openAction == SettingsOpenAction.AddGameFolder)
            {
                await ViewModel.AddGameSourceFolderCommand.ExecuteAsync(null);
            }
            else if (openAction == SettingsOpenAction.AddTvAndFilmFolder)
            {
                await ViewModel.AddTvAndFilmSourceFolderCommand.ExecuteAsync(null);
            }
        }

        private void SettingsNav_Loaded(object sender, RoutedEventArgs e)
        {
            if (SettingsNav.MenuItems.Count > 0)
            {
                SettingsNav.SelectedItem = SettingsNav.MenuItems[0];
            }
        }

        private void SettingsNav_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is Microsoft.UI.Xaml.Controls.NavigationViewItem selectedItem)
            {
                string tag = selectedItem.Tag?.ToString() ?? string.Empty;

                GeneralPanel.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
                GamesPanel.Visibility = tag == "Games" ? Visibility.Visible : Visibility.Collapsed;
                AppsPanel.Visibility = tag == "Apps" ? Visibility.Visible : Visibility.Collapsed;
                FilmAndTvPanel.Visibility = tag == "FilmAndTv" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void RemoveGameFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string folderPath)
            {
                await ViewModel.RemoveGameSourceFolderCommand.ExecuteAsync(folderPath);
            }
        }

        private async void RemoveAppFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string folderPath)
            {
                await ViewModel.RemoveAppSourceFolderCommand.ExecuteAsync(folderPath);
            }
        }

        private async void RemoveTvAndFilmFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string folderPath)
            {
                await ViewModel.RemoveTvAndFilmSourceFolderCommand.ExecuteAsync(folderPath);
            }
        }

        private async void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveSettingsCommand.ExecuteAsync(null);
            SettingsClosed?.Invoke(this, EventArgs.Empty);
            Close();
        }
    }
}

