using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using System;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Modal host window displaying detected duplicate library items with one-click cleanup controls.
    /// </summary>
    public sealed partial class DuplicateManagerWindow : Window
    {
        /// <summary>
        /// Gets the <see cref="DuplicateManagerViewModel"/> driving this window.
        /// </summary>
        public DuplicateManagerViewModel ViewModel { get; }

        /// <summary>
        /// The owner window used for modal placement.
        /// </summary>
        private readonly Window _owner;

        /// <summary>
        /// Event raised when the duplicate manager window is closed.
        /// </summary>
        public event EventHandler? DuplicateManagerClosed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateManagerWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The view model supplying duplicate item data.</param>
        /// <param name="owner">The parent owner window for modal anchoring.</param>
        public DuplicateManagerWindow(DuplicateManagerViewModel viewModel, Window owner)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));

            InitializeComponent();

            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 1020, 720);
            Closed += (s, e) => DuplicateManagerClosed?.Invoke(this, EventArgs.Empty);

            // Automatically kick off duplicate scan if not yet loaded
            if (ViewModel.DuplicateGroups.Count == 0 && !ViewModel.IsScanning)
            {
                _ = ViewModel.ScanDuplicatesAsync();
            }
        }

        /// <summary>
        /// Handles click on a group-specific "Keep Newest Copies" button.
        /// </summary>
        private void GroupSelectOlder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DuplicateGroupViewModel group })
            {
                group.SelectOlderCopies();
            }
        }

        /// <summary>
        /// Handles click on a group-specific "Keep Oldest Copies" button.
        /// </summary>
        private void GroupSelectNewer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DuplicateGroupViewModel group })
            {
                group.SelectNewerCopies();
            }
        }

        /// <summary>
        /// Handles click on a group-specific "Deselect All" button.
        /// </summary>
        private void GroupDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DuplicateGroupViewModel group })
            {
                group.DeselectAll();
            }
        }
    }
}

