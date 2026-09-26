using Easy_Copier.Infrastructure;
using Easy_Copier.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default <see cref="Application"/> class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private IServiceProvider? _serviceProvider;
        private bool _servicesDisposed;

        /// <summary>
        /// Gets the dependency injection service provider instance configured for the application.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when services have not been initialized.</exception>
        public IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Services not initialized");

        /// <summary>
        /// Gets the primary <see cref="Window"/> instance of the running application.
        /// </summary>
        public static Window? MainWindow { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class, registering global exception handlers and configuring services.
        /// </summary>
        public App()
        {
            InitializeComponent();
            ConfigureServices();

            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        /// <summary>
        /// Handles unhandled exceptions thrown within the XAML framework layer.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The unhandled exception event arguments.</param>
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (_serviceProvider != null)
            {
                ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogCritical(e.Exception, "A fatal XAML exception occurred.");
            }
            e.Handled = true; // Attempt to prevent crashing where possible
        }

        /// <summary>
        /// Handles unhandled exceptions originating from the application domain.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The exception event arguments.</param>
        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (_serviceProvider != null && e.ExceptionObject is Exception ex)
            {
                ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogCritical(ex, "A fatal application domain exception occurred.");
            }
        }

        /// <summary>
        /// Handles unobserved task exceptions from background task executions.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The unobserved task exception event arguments.</param>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception.InnerExceptions.Any(ex => ex is TaskCanceledException or OperationCanceledException))
            {
                e.SetObserved();
            }
            else
            {
                if (_serviceProvider != null)
                {
                    ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                    logger.LogWarning(e.Exception, "An unobserved task exception occurred.");
                }
                e.SetObserved(); // Prevent unobserved task exceptions from terminating the process
            }
        }

        /// <summary>
        /// Configures the Dependency Injection container with services and view models.
        /// </summary>
        private void ConfigureServices()
        {
            ServiceCollection services = new();

            _ = services.AddApplicationServices();
            _ = services.AddViewModels();

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <inheritdoc />
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

            IFlyoutService flyoutService = Services.GetRequiredService<IFlyoutService>();
            FlyoutHelper.Initialize(flyoutService);

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

            ISmartAdderHistoryService smartAdderHistoryService = Services.GetRequiredService<ISmartAdderHistoryService>();
            await smartAdderHistoryService.InitializeAsync();
        }

        /// <summary>
        /// Handles notification invocation callbacks when a user clicks a desktop toast notification.
        /// </summary>
        /// <param name="sender">The notification manager instance.</param>
        /// <param name="args">The notification activation arguments.</param>
        private void AppNotificationManager_NotificationInvoked(Microsoft.Windows.AppNotifications.AppNotificationManager sender, Microsoft.Windows.AppNotifications.AppNotificationActivatedEventArgs args)
        {
            if (_serviceProvider != null)
            {
                ILogger<App> logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogInformation("App notification invoked: {Arguments}", args.Argument);
            }
        }

        /// <summary>
        /// Disposes the dependency injection service provider and flushes pending log sinks.
        /// </summary>
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
