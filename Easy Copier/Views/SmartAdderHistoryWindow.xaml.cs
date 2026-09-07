using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;

namespace Easy_Copier.Views
{
    public sealed partial class SmartAdderHistoryWindow : Window
    {
        public SmartAdderHistoryViewModel ViewModel { get; }
        private readonly Window _owner;

        public SmartAdderHistoryWindow(SmartAdderHistoryViewModel viewModel, Window owner)
        {
            ViewModel = viewModel;
            _owner = owner;
            InitializeComponent();
            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 360, 720);

            _ = ViewModel.InitializeAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
