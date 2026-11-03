using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Easy_Copier.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using WinRT.Interop;

namespace Easy_Copier
{
    /// <summary>
    /// Represents the main top-level application window hosting the primary navigation, pages, and system tray lifecycle handling.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly ISystemTrayService _systemTrayService;
        private readonly ITransferQueueService _transferQueueService;
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The main view model for the application state.</param>
        /// <param name="systemTrayService">The system tray service managing tray notification icons and context menus.</param>
        /// <param name="transferQueueService">The transfer queue service tracking active background transfers.</param>
        /// <param name="settingsService">The settings service for querying user preferences.</param>
        public MainWindow(
            ViewModels.MainViewModel viewModel,
            ISystemTrayService systemTrayService,
            ITransferQueueService transferQueueService,
            ISettingsService settingsService)
        {
            _systemTrayService = systemTrayService ?? throw new ArgumentNullException(nameof(systemTrayService));
            _transferQueueService = transferQueueService ?? throw new ArgumentNullException(nameof(transferQueueService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            InitializeComponent();

            NativeWindowHelper.InitializeWindow(this, 1400, 900);
            NativeWindowHelper.SetMinimumSize(this, 960, 640);

            // Initialize system tray integration with this main window
            _systemTrayService.Initialize(this);

            ConfigureWindowLifecycle();

            _ = RootFrame.Navigate(typeof(MainPage), viewModel);
        }

        /// <summary>
        /// Intercepts the window closing event to minimize to the system tray if active transfers are running.
        /// </summary>
        private void ConfigureWindowLifecycle()
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.Closing += (s, args) =>
                {
                    AppSettings settings = _settingsService.LoadSettingsSync();
                    bool hasActiveTransfers = _transferQueueService.QueueItems.Any(i => i.IsActive);

                    // Intent: Prevent accidental termination of running/queued transfers by intercepting the close action
                    // and keeping execution alive in the system tray.
                    if (hasActiveTransfers && settings.MinimizeToTrayOnClose)
                    {
                        args.Cancel = true;
                        _systemTrayService.MinimizeToTray();
                    }
                    else
                    {
                        _systemTrayService.RemoveTrayIcon();
                    }
                };
            }
        }
    }
}
