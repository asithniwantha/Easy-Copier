using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using WinRT.Interop;

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

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            ViewModel.WindowHandle = hwnd;
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "easy copier ico.ico");
                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }

                // Center window
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                if (displayArea != null)
                {
                    var width = 1000;
                    var height = 700;
                    var x = ((displayArea.WorkArea.Width - width) / 2);
                    var y = ((displayArea.WorkArea.Height - height) / 2);

                    appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
                }
            }

            this.Closed += HistoryWindow_Closed;

            RootFrame.Navigate(typeof(HistoryPage));
        }

        private void HistoryWindow_Closed(object sender, WindowEventArgs args)
        {
            HistoryClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
