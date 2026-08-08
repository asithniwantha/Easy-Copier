using Easy_Copier.Infrastructure;
using Easy_Copier.Services;
using Easy_Copier.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

        public SettingsWindow()
        {
            ViewModel = AppServiceLocator.GetService<SettingsViewModel>();
            InitializeComponent();

            _hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(760, 640));
            }

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

            _ = LoadAsync();
        }

        private void SettingsWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_ownerHwnd != IntPtr.Zero)
            {
                NativeWindowHelper.EnableWindowInput(_ownerHwnd, true);
                NativeWindowHelper.SetForeground(_ownerHwnd);
            }
        }

        private async Task LoadAsync()
        {
            await ViewModel.LoadSettingsAsync();
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = AppServiceLocator.GetService<ISettingsService>();
            var folderPath = Path.GetDirectoryName(settingsService.GetSettingsFilePath());

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                ViewModel.StatusMessage = "Unable to resolve the data folder";
                return;
            }

            try
            {
                var processService = AppServiceLocator.GetService<IProcessService>();
                processService.OpenInExplorer(folderPath);

                ViewModel.StatusMessage = $"Opened data folder: {folderPath}";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Unable to open data folder: {ex.Message}";
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

        private async void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveSettingsCommand.ExecuteAsync(null);
            SettingsClosed?.Invoke(this, EventArgs.Empty);
            Close();
        }
    }
}

