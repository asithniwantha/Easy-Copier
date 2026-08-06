using Easy_Copier.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace Easy_Copier
{
    public partial class App : Application
    {
        private Window? _window;
        private IServiceProvider? _serviceProvider;

        public static Window? MainWindow { get; private set; }

        public App()
        {
            InitializeComponent();
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddApplicationServices();
            services.AddViewModels();

            _serviceProvider = services.BuildServiceProvider();
            AppServiceLocator.Initialize(_serviceProvider);
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
        }
    }
}
