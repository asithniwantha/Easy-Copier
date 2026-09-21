using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using System;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Modal host window displaying file transfer operation history.
    /// </summary>
    public sealed partial class HistoryWindow : Window
    {
        /// <summary>
        /// Gets the <see cref="HistoryViewModel"/> associated with this window.
        /// </summary>
        public HistoryViewModel ViewModel { get; }

        /// <summary>
        /// The owner window used for modal positioning and centering.
        /// </summary>
        private readonly Window _owner;

        /// <summary>
        /// Event raised when the history window is closed.
        /// </summary>
        public event EventHandler? HistoryClosed;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The view model for history data.</param>
        /// <param name="owner">The parent owner window for modal placement.</param>
        public HistoryWindow(HistoryViewModel viewModel, Window owner)
        {
            ViewModel = viewModel;
            _owner = owner;
            InitializeComponent();

            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 1000, 700);
            Closed += (s, e) => HistoryClosed?.Invoke(this, EventArgs.Empty);

            _ = RootFrame.Navigate(typeof(HistoryPage), ViewModel);
        }
    }
}
