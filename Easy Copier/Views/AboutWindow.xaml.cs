using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;

namespace Easy_Copier.Views
{
    public sealed partial class AboutWindow : Window
    {
        public AboutViewModel ViewModel { get; }

        public AboutWindow(AboutViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();

            if (Content is FrameworkElement fe)
            {
                fe.DataContext = ViewModel;
            }




            NativeWindowHelper.InitializeWindow(this, 500, 600);
            NativeWindowHelper.ShowAsModal(this, App.MainWindow);

            Closed += AboutWindow_Closed;
        }

        private void AboutWindow_Closed(object sender, WindowEventArgs args)
        {
            NativeWindowHelper.RestoreOwnerInput(App.MainWindow);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
