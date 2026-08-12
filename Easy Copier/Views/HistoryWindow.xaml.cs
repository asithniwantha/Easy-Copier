using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace Easy_Copier.Views
{
    public sealed partial class HistoryWindow : Window
    {
        public HistoryViewModel ViewModel { get; }

        public event EventHandler? HistoryClosed;

        private readonly IntPtr _ownerHwnd;
        private readonly IntPtr _hwnd;

        public HistoryWindow()
        {
            InitializeComponent();

            ViewModel = AppServiceLocator.GetService<HistoryViewModel>();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "easy copier ico.ico");
                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }

                appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1000, Height = 700 });
            }

            _ownerHwnd = App.MainWindow != null ? WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow) : IntPtr.Zero;

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
            else if (appWindow != null)
            {
                // Fallback Center window
                DisplayArea displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                if (displayArea != null)
                {
                    int width = 1000;
                    int height = 700;
                    int x = (displayArea.WorkArea.Width - width) / 2;
                    int y = (displayArea.WorkArea.Height - height) / 2;

                    appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
                }
            }

            Closed += HistoryWindow_Closed;

            _ = RootFrame.Navigate(typeof(HistoryPage));
        }

        private void HistoryWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_ownerHwnd != IntPtr.Zero)
            {
                NativeWindowHelper.EnableWindowInput(_ownerHwnd, true);
                NativeWindowHelper.SetForeground(_ownerHwnd);
            }
            this.Content = null;
            HistoryClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
