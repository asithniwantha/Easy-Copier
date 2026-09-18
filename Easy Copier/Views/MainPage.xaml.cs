using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Represents the main page view housing library tabs and transfer action center.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Gets the <see cref="MainViewModel"/> bound to this page.
        /// </summary>
        public MainViewModel ViewModel { get; private set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            base.OnNavigatedTo(e);
            if (e.Parameter is MainViewModel viewModel)
            {
                ViewModel = viewModel;
                DataContext = ViewModel;
                ViewModel.ItemQueued += (s, args) => ClearGameSelection();
                ViewModel.ClearSelectionRequested += (s, args) => ClearGameSelection();
                Bindings.Update();
                _ = ViewModel.InitializeAsync();
            }
        }

        private void TabOrPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void UpdateCombinedSelection()
        {
            if (ViewModel.CurrentTabIndex == 3)
            {
                IEnumerable<GameEntry> osImageItems = OsImagesTab?.SelectedItems.Cast<GameEntry>() ?? [];
                ViewModel.UpdateSelectionSummary(osImageItems);
            }
            else
            {
                IEnumerable<GameEntry> tvItems = TvAndFilmsTab?.SelectedItems.Cast<GameEntry>() ?? [];
                IEnumerable<GameEntry> gamesItems = GamesTab?.SelectedItems.Cast<GameEntry>() ?? [];
                IEnumerable<GameEntry> appsItems = AppsTab?.SelectedItems.Cast<GameEntry>() ?? [];
                IEnumerable<GameEntry> selectedItems = gamesItems
                    .Concat(appsItems)
                    .Concat(tvItems);
                ViewModel.UpdateSelectionSummary(selectedItems);
            }
        }

        private void ClearGameSelection()
        {
            GamesTab?.ClearSelection();
            AppsTab?.ClearSelection();
            TvAndFilmsTab?.ClearSelection();
            OsImagesTab?.ClearSelection();
        }
    }
}
