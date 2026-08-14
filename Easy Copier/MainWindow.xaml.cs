using Easy_Copier.Views;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace Easy_Copier
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Easy_Copier.Infrastructure.NativeWindowHelper.InitializeWindow(this, 1400, 900);

            if (Application.Current is App app)
            {
                var viewModel = app.Services.GetRequiredService<Easy_Copier.ViewModels.MainViewModel>();
                _ = RootFrame.Navigate(typeof(MainPage), viewModel);
            }

            Closed += MainWindow_Closed;
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
