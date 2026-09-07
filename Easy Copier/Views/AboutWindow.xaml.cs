using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;

namespace Easy_Copier.Views
{
    public sealed partial class AboutWindow : Window
    {
        public AboutViewModel ViewModel { get; }
        private readonly Window _owner;

        public AboutWindow(AboutViewModel viewModel, Window owner)
        {
            ViewModel = viewModel;
            _owner = owner;
            InitializeComponent();
            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 500, 600);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
