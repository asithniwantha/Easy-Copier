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

        /// <summary>
        /// Gets the light yellow/amber brush (~23.5% opacity) used to highlight the SearchBox when search text is present.
        /// </summary>
        private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush SearchHighlightBrush = new(Microsoft.UI.ColorHelper.FromArgb(0x3C, 0xFF, 0xC1, 0x07));

        /// <inheritdoc />
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
                UpdateSearchBoxBackground(ViewModel.SearchText);
                _ = ViewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// Handles text change events in the search box to dynamically update its background color.
        /// </summary>
        /// <param name="sender">The AutoSuggestBox control.</param>
        /// <param name="args">Event arguments.</param>
        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            UpdateSearchBoxBackground(sender?.Text);
        }

        /// <summary>
        /// Updates the SearchBox background and resource dictionaries based on whether search text is present.
        /// </summary>
        /// <param name="text">The current search text.</param>
        private void UpdateSearchBoxBackground(string? text)
        {
            if (SearchBox == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                SearchBox.Resources["TextControlBackground"] = SearchHighlightBrush;
                SearchBox.Resources["TextControlBackgroundPointerOver"] = SearchHighlightBrush;
                SearchBox.Resources["TextControlBackgroundFocused"] = SearchHighlightBrush;
                SearchBox.Background = SearchHighlightBrush;
            }
            else
            {
                _ = SearchBox.Resources.Remove("TextControlBackground");
                _ = SearchBox.Resources.Remove("TextControlBackgroundPointerOver");
                _ = SearchBox.Resources.Remove("TextControlBackgroundFocused");
                SearchBox.ClearValue(Control.BackgroundProperty);
            }
        }

        private void TabOrPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private IEnumerable<ILibraryTabView> GetTabViews()
        {
            ILibraryTabView?[] tabs = [GamesTab, AppsTab, TvAndFilmsTab, OsImagesTab];
            return tabs.OfType<ILibraryTabView>();
        }

        private void UpdateCombinedSelection()
        {
            if (ViewModel == null)
            {
                return;
            }

            IEnumerable<ILibraryTabView> tabs = GetTabViews();

            if (ViewModel.IsOsImagesTabActive)
            {
                IEnumerable<GameEntry> osImageItems = tabs.ElementAtOrDefault(3)?.GetSelectedEntries() ?? [];
                ViewModel.UpdateSelectionSummary(osImageItems);
            }
            else
            {
                IEnumerable<GameEntry> selectedItems = tabs
                    .Take(3)
                    .SelectMany(t => t.GetSelectedEntries());
                ViewModel.UpdateSelectionSummary(selectedItems);
            }
        }

        private void ClearGameSelection()
        {
            foreach (ILibraryTabView tab in GetTabViews())
            {
                tab.ClearSelection();
            }
        }
    }
}
