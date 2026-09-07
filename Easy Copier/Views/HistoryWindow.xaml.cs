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

            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 1000, 700);
            Closed += (s, e) => HistoryClosed?.Invoke(this, EventArgs.Empty);

            _ = RootFrame.Navigate(typeof(HistoryPage), ViewModel);
        }
    }
}
