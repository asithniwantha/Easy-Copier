using Easy_Copier.Views;
using Microsoft.UI.Xaml;

namespace Easy_Copier
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow(ViewModels.MainViewModel viewModel)
        {
            InitializeComponent();

            Easy_Copier.Infrastructure.NativeWindowHelper.InitializeWindow(this, 1400, 900);

            _ = RootFrame.Navigate(typeof(MainPage), viewModel);
        }
    }
}
