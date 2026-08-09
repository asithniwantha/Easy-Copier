using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Easy_Copier.Views
{
    public sealed partial class HistoryPage : Page
    {
        public HistoryViewModel ViewModel { get; }

        public HistoryPage()
        {
            InitializeComponent();
            ViewModel = AppServiceLocator.GetService<HistoryViewModel>();
            _ = ViewModel.InitializeAsync();
        }
    }
}
