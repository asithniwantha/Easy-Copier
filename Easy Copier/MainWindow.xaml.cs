using Easy_Copier.Views;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Easy_Copier
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
            }

            RootFrame.Navigate(typeof(MainPage));
        }
    }
}
