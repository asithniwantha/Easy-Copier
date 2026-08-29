using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using System;

namespace Easy_Copier.Views
{
    public sealed partial class HistoryWindow : Window
    {
        public HistoryViewModel ViewModel { get; }
        private readonly Window _owner;

        public event EventHandler? HistoryClosed;

        public HistoryWindow(HistoryViewModel viewModel, Window owner)
        {
            ViewModel = viewModel;
            _owner = owner;
            InitializeComponent();

            NativeWindowHelper.InitializeWindow(this, 1000, 700);
            NativeWindowHelper.ShowAsModal(this, _owner);

            Closed += HistoryWindow_Closed;

            _ = RootFrame.Navigate(typeof(HistoryPage), ViewModel);
        }

        /// <summary>
        /// Handles the Closed event of the HistoryWindow.
        /// </summary>
        private void HistoryWindow_Closed(object sender, WindowEventArgs args)
        {
            NativeWindowHelper.RestoreOwnerInput(_owner);
            Content = null;
            HistoryClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
