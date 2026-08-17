using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Easy_Copier.Views
{
    public sealed partial class HistoryPage : Page
    {
        public HistoryViewModel? ViewModel { get; private set; }

        public HistoryPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            base.OnNavigatedTo(e);
            if (e.Parameter is HistoryViewModel viewModel)
            {
                ViewModel = viewModel;
                Bindings.Update();
                _ = viewModel.InitializeAsync();
            }
        }
    }
}
