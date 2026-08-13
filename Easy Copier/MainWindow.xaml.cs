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

            Easy_Copier.Infrastructure.NativeWindowHelper.InitializeWindow(this, 1400, 900);

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
