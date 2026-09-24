using Easy_Copier.Views;
using Microsoft.UI.Xaml;

namespace Easy_Copier
{
    /// <summary>
    /// Represents the main top-level application window hosting the primary navigation and pages.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The main view model for the application state.</param>
        public MainWindow(ViewModels.MainViewModel viewModel)
        {
            InitializeComponent();

            Easy_Copier.Infrastructure.NativeWindowHelper.InitializeWindow(this, 1400, 900);
            Easy_Copier.Infrastructure.NativeWindowHelper.SetMinimumSize(this, 960, 640);

            _ = RootFrame.Navigate(typeof(MainPage), viewModel);
        }
    }
}
