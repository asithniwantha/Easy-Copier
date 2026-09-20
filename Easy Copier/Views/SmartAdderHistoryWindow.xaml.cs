using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Modal host window displaying saved SmartAdder calculation history records.
    /// </summary>
    public sealed partial class SmartAdderHistoryWindow : Window
    {
        /// <summary>
        /// Gets the <see cref="SmartAdderHistoryViewModel"/> backing this view.
        /// </summary>
        public SmartAdderHistoryViewModel ViewModel { get; }

        /// <summary>
        /// The owner window used for modal positioning and centering.
        /// </summary>
        private readonly Window _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartAdderHistoryWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The view model for SmartAdder history.</param>
        /// <param name="owner">The parent owner window for modal placement.</param>
        public SmartAdderHistoryWindow(SmartAdderHistoryViewModel viewModel, Window owner)
        {
            ViewModel = viewModel;
            _owner = owner;
            InitializeComponent();
            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 360, 720);

            ViewModel.CloseRequested += (s, e) => Close();

            _ = ViewModel.InitializeAsync();
        }
    }
}
