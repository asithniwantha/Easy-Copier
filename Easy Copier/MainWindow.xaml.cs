using Easy_Copier.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using WinRT.Interop;

namespace Easy_Copier
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            nint hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            // Set the window icon and size
            if (appWindow != null)
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "easy copier ico.ico");
                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }

                appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
            }

            _ = RootFrame.Navigate(typeof(MainPage));

            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Handles the Closed event of the MainWindow.
        /// </summary>
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (Application.Current is App app)
            {
                app.DisposeServices();
            }
        }
    }
}
