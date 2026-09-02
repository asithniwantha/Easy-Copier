using Easy_Copier.Infrastructure;
using Easy_Copier.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using System;

namespace Easy_Copier
{
    public partial class App : Application
    {
        private Window? _window;
        private IServiceProvider? _serviceProvider;

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
                var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogCritical(e.Exception, "A fatal XAML exception occurred.");
            }
            e.Handled = true; // Attempt to prevent crashing where possible
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (_serviceProvider != null && e.ExceptionObject is Exception ex)
            {
                var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
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
            var logger = Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Easy Copier application starting up.");

            ViewModels.MainViewModel mainViewModel = Services.GetRequiredService<ViewModels.MainViewModel>();
            _window = new MainWindow(mainViewModel);
            MainWindow = _window;

            _window.Closed += (s, e) =>
            {
                logger.LogInformation("Easy Copier application shutting down.");
                DisposeServices();
            };

            _window.Activate();

            ICopyHistoryService copyHistoryService = Services.GetRequiredService<Services.ICopyHistoryService>();
            await copyHistoryService.InitializeAsync();

            IDatabaseService databaseService = Services.GetRequiredService<IDatabaseService>();
            await databaseService.InitializeAsync();
        }

        public void DisposeServices()
        {
            if (Services is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
