using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Easy_Copier.Views
{
    public sealed partial class OsImageDetailsFlyout : UserControl
    {
        public OsImageDetailsViewModel ViewModel { get; }

        public OsImageDetailsFlyout(OsImageDetailsViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
