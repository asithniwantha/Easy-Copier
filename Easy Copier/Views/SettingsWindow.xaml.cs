using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using WinRT.Interop;

namespace Easy_Copier.Views
{
    public sealed partial class SettingsWindow : Window
    {
        public SettingsViewModel ViewModel { get; }
        public event EventHandler? SettingsClosed;

        private readonly IntPtr _ownerHwnd;
        private readonly IntPtr _hwnd;

        public SettingsWindow(SettingsOpenAction openAction = SettingsOpenAction.None)
        {
            ViewModel = AppServiceLocator.GetService<SettingsViewModel>();
            InitializeComponent();

            _hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            appWindow?.Resize(new Windows.Graphics.SizeInt32(960, 640));

            _ownerHwnd = App.MainWindow != null ? WindowNative.GetWindowHandle(App.MainWindow) : IntPtr.Zero;

            if (_ownerHwnd != IntPtr.Zero)
            {
                // Establish native ownership so this window stays above the main window
                // and is treated as its modal child by the OS.
                NativeWindowHelper.SetOwner(_hwnd, _ownerHwnd);

                // Disable the owner so input can't reach it while this window is open,
                // then re-enable and restore focus once this window closes.
                NativeWindowHelper.EnableWindowInput(_ownerHwnd, false);
                NativeWindowHelper.CenterWindow(_hwnd, _ownerHwnd);
            }

            Closed += SettingsWindow_Closed;

            _ = LoadAsync(openAction);
        }

        private void SettingsWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_ownerHwnd != IntPtr.Zero)
            {
                NativeWindowHelper.EnableWindowInput(_ownerHwnd, true);
                NativeWindowHelper.SetForeground(_ownerHwnd);
            }
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

