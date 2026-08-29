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

            if (Content is FrameworkElement fe)
            {
                fe.DataContext = ViewModel;
            }




            NativeWindowHelper.InitializeWindow(this, 500, 600);
            NativeWindowHelper.ShowAsModal(this, _owner);

            Closed += AboutWindow_Closed;
        }
        private void AboutWindow_Closed(object sender, WindowEventArgs args)
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
