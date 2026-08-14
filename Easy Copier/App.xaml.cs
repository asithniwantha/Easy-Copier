using Easy_Copier.Infrastructure;
using Easy_Copier.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
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
            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();

            ICopyHistoryService copyHistoryService = Services.GetRequiredService<Services.ICopyHistoryService>();
            await copyHistoryService.InitializeAsync();
        }

        public void DisposeServices()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
