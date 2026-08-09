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

        public HistoryWindow()
        {
            InitializeComponent();

            ViewModel = AppServiceLocator.GetService<HistoryViewModel>();

            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "easy copier ico.ico");
                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }

                // Center window
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
            HistoryClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
