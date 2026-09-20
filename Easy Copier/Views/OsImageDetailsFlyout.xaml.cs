using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Easy_Copier.Views
{
    /// <summary>
    /// User control displaying detailed information and folder contents for an OS image item in a flyout.
    /// </summary>
    public sealed partial class OsImageDetailsFlyout : UserControl
    {
        /// <summary>
        /// Gets the <see cref="OsImageDetailsViewModel"/> backing this details view.
        /// </summary>
        public OsImageDetailsViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OsImageDetailsFlyout"/> class.
        /// </summary>
        /// <param name="viewModel">The view model containing OS image details.</param>
        public OsImageDetailsFlyout(OsImageDetailsViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
