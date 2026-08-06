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
        private const int GWLP_HWNDPARENT = -8;

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

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
                SetWindowLongPtr(_hwnd, GWLP_HWNDPARENT, _ownerHwnd);

                // Disable the owner so input can't reach it while this window is open,
                // then re-enable and restore focus once this window closes.
                EnableWindow(_ownerHwnd, false);
                CenterOverOwner();
            }

            Closed += SettingsWindow_Closed;

            _ = LoadAsync();
        }

        private void SettingsWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_ownerHwnd != IntPtr.Zero)
            {
                EnableWindow(_ownerHwnd, true);
                SetForegroundWindow(_ownerHwnd);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void CenterOverOwner()
        {
            if (!GetWindowRect(_ownerHwnd, out var ownerRect) || !GetWindowRect(_hwnd, out var selfRect))
                return;

            var ownerWidth = ownerRect.Right - ownerRect.Left;
            var ownerHeight = ownerRect.Bottom - ownerRect.Top;
            var selfWidth = selfRect.Right - selfRect.Left;
            var selfHeight = selfRect.Bottom - selfRect.Top;

            var x = ownerRect.Left + (ownerWidth - selfWidth) / 2;
            var y = ownerRect.Top + (ownerHeight - selfHeight) / 2;

            SetWindowPos(_hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }

        private static void SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            }
            else
            {
                SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32());
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
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = folderPath,
                    UseShellExecute = true
                });

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

