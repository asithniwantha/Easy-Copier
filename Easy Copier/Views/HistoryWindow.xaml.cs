using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using System;

namespace Easy_Copier.Views
{
    public sealed partial class HistoryWindow : Window
    {
        public HistoryViewModel ViewModel { get; }

        public event EventHandler? HistoryClosed;

        public HistoryWindow(HistoryViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();

            NativeWindowHelper.InitializeWindow(this, 1000, 700);
            NativeWindowHelper.ShowAsModal(this, App.MainWindow);

            Closed += HistoryWindow_Closed;

            _ = RootFrame.Navigate(typeof(HistoryPage), ViewModel);
        }

        /// <summary>
        /// Handles the Closed event of the HistoryWindow.
        /// </summary>
        private void HistoryWindow_Closed(object sender, WindowEventArgs args)
        {
            NativeWindowHelper.RestoreOwnerInput(App.MainWindow);
            Content = null;
            HistoryClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
