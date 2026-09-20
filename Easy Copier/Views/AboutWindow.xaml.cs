using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Displays application version, license, and copyright information in a modal window.
    /// </summary>
    public sealed partial class AboutWindow : Window
    {
        /// <summary>
        /// Gets the <see cref="AboutViewModel"/> providing view state and commands for this window.
        /// </summary>
        public AboutViewModel ViewModel { get; }

        /// <summary>
        /// The owner window used for modal positioning and centering.
        /// </summary>
        private readonly Window _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The view model backing the window.</param>
        /// <param name="owner">The owner window for modal positioning.</param>
        public AboutWindow(AboutViewModel viewModel, Window owner)
        {
            ViewModel = viewModel;
            _owner = owner;
            InitializeComponent();
            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 500, 600);

            ViewModel.CloseRequested += (s, e) => Close();
        }
    }
}
