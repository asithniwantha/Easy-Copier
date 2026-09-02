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

            if (Content is FrameworkElement fe)
            {
                fe.DataContext = ViewModel;
            }

            NativeWindowHelper.InitializeWindow(this, 360, 720);
            NativeWindowHelper.ShowAsModal(this, _owner);

            Closed += SmartAdderHistoryWindow_Closed;

            _ = ViewModel.InitializeAsync();
        }

        private void SmartAdderHistoryWindow_Closed(object sender, WindowEventArgs args)
        {
            NativeWindowHelper.RestoreOwnerInput(_owner);
            Content = null;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
