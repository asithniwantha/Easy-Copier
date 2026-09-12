using Easy_Copier.Infrastructure;
using Easy_Copier.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;

namespace Easy_Copier
{
    public partial class App : Application
    {
        private Window? _window;
        private IServiceProvider? _serviceProvider;
        private bool _servicesDisposed;

        public IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Services not initialized");

        public static Window? MainWindow { get; private set; }

        public App()
        {
            InitializeComponent();
            ConfigureServices();

            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (_serviceProvider != null)
            {
                ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogCritical(e.Exception, "A fatal XAML exception occurred.");
            }
            e.Handled = true; // Attempt to prevent crashing where possible
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (_serviceProvider != null && e.ExceptionObject is Exception ex)
            {
                ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogCritical(ex, "A fatal application domain exception occurred.");
            }
        }

        private void ConfigureServices()
        {
            ServiceCollection services = new();

            _ = services.AddApplicationServices();
            _ = services.AddViewModels();

            _serviceProvider = services.BuildServiceProvider();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            ILogger<App> logger = Services.GetRequiredService<ILogger<App>>();

            if (Microsoft.Windows.AppNotifications.AppNotificationManager.IsSupported())
            {
                logger.LogInformation("AppNotificationManager is supported. Registering...");
                try
                {
                    Microsoft.Windows.AppNotifications.AppNotificationManager.Default.NotificationInvoked += AppNotificationManager_NotificationInvoked;
                    Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Register();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to register AppNotificationManager during startup.");
                }
            }
            else
            {
                logger.LogWarning("AppNotificationManager.IsSupported() returned false. Toast notifications will not be displayed. This usually occurs if the application is running as Administrator (elevated) or the Windows App SDK runtime is missing components.");
            }
            logger.LogInformation("Easy Copier application starting up.");

            ViewModels.MainViewModel mainViewModel = Services.GetRequiredService<ViewModels.MainViewModel>();
            _window = new MainWindow(mainViewModel);
            MainWindow = _window;

            _window.Closed += (s, e) =>
            {
                logger.LogInformation("Easy Copier application shutting down.");
                if (Microsoft.Windows.AppNotifications.AppNotificationManager.IsSupported())
                {
                    Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Unregister();
                    Microsoft.Windows.AppNotifications.AppNotificationManager.Default.NotificationInvoked -= AppNotificationManager_NotificationInvoked;
                }
                DisposeServices();
            };

            _window.Activate();

            ICopyHistoryService copyHistoryService = Services.GetRequiredService<Services.ICopyHistoryService>();
            await copyHistoryService.InitializeAsync();

            IDatabaseService databaseService = Services.GetRequiredService<IDatabaseService>();
            await databaseService.InitializeAsync();

            ISmartAdderHistoryService smartAdderHistoryService = Services.GetRequiredService<ISmartAdderHistoryService>();
            await smartAdderHistoryService.InitializeAsync();
        }

        private void AppNotificationManager_NotificationInvoked(Microsoft.Windows.AppNotifications.AppNotificationManager sender, Microsoft.Windows.AppNotifications.AppNotificationActivatedEventArgs args)
        {
            if (_serviceProvider != null)
            {
                ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogInformation("App notification invoked: {Arguments}", args.Argument);
            }
        }

        public void DisposeServices()
        {
            if (_servicesDisposed)
            {
                return;
            }

            _servicesDisposed = true;
            MainWindow = null;

            if (_serviceProvider is IDisposable disposable)
            {
                _serviceProvider = null;
                disposable.Dispose();
            }

            // Ensure Serilog flushes any pending logs and releases file handles
            Serilog.Log.CloseAndFlush();
        }
    }
}
