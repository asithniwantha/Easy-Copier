using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Easy_Copier.ViewModels;

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
