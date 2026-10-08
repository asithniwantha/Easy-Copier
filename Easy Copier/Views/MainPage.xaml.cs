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
        private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush SearchHighlightBrush =
            new(Microsoft.UI.ColorHelper.FromArgb(0x3C, 0xFF, 0xC1, 0x07));

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

        /// <inheritdoc />
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            base.OnNavigatedTo(e);
            if (e.Parameter is MainViewModel viewModel)
            {
                ViewModel = viewModel;
                DataContext = ViewModel;
                ViewModel.ItemQueued += OnItemQueued;
                ViewModel.ClearSelectionRequested += OnClearSelectionRequested;
                Bindings.Update();
                UpdateSearchBoxBackground(ViewModel.SearchText);
                _ = ViewModel.InitializeAsync();
            }
        }

        /// <inheritdoc />
        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (ViewModel != null)
            {
                ViewModel.ItemQueued -= OnItemQueued;
                ViewModel.ClearSelectionRequested -= OnClearSelectionRequested;
            }
        }

        /// <summary>
        /// Handles the <see cref="MainViewModel.ItemQueued"/> event by clearing active library selections.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event args.</param>
        private void OnItemQueued(object? sender, EventArgs e) => ClearGameSelection();

        /// <summary>
        /// Handles the <see cref="MainViewModel.ClearSelectionRequested"/> event by clearing active library selections.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event args.</param>
        private void OnClearSelectionRequested(object? sender, EventArgs e) => ClearGameSelection();

        /// <summary>
        /// Handles text changes in the search box to dynamically update visual background highlights.
        /// </summary>
        /// <param name="sender">The auto-suggest box control.</param>
        /// <param name="args">Event args containing text change details.</param>
        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            UpdateSearchBoxBackground(sender?.Text);
        }

        /// <summary>
        /// Updates the background styling resources for the search box based on whether search text is populated.
        /// </summary>
        /// <param name="text">The current search text string.</param>
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

        /// <summary>
        /// Handles pivot tab selection changes to recalculate and aggregate entry selections.
        /// </summary>
        /// <param name="sender">The selection control.</param>
        /// <param name="e">Event args containing selection change info.</param>
        private void TabOrPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        /// <summary>
        /// Retrieves instantiated library tab views from the Pivot item collection.
        /// </summary>
        /// <returns>An enumeration of active <see cref="ILibraryTabView"/> instances.</returns>
        private IEnumerable<ILibraryTabView> GetTabViews()
        {
            return LibraryPivot == null
                ? []
                : LibraryPivot.Items
                    .OfType<PivotItem>()
                    .Select(p => p.Content)
                    .OfType<ILibraryTabView>();
        }

        /// <summary>
        /// Recalculates combined library selection across tabs and updates the View Models selection summary.
        /// </summary>
        private void UpdateCombinedSelection()
        {
            if (ViewModel == null || LibraryPivot == null)
            {
                return;
            }

            if (ViewModel.IsOsImagesTabActive)
            {
                if (LibraryPivot.SelectedItem is PivotItem activeItem && activeItem.Content is ILibraryTabView activeTab)
                {
                    ViewModel.UpdateSelectionSummary(activeTab.GetSelectedEntries());
                }
            }
            else
            {
                IEnumerable<GameEntry> selectedItems = GetTabViews()
                    .Where(tab => !tab.IsOsImagesTab)
                    .SelectMany(tab => tab.GetSelectedEntries());
                ViewModel.UpdateSelectionSummary(selectedItems);
            }
        }

        /// <summary>
        /// Clears entry selections across all loaded library tab views.
        /// </summary>
        private void ClearGameSelection()
        {
            foreach (ILibraryTabView tab in GetTabViews())
            {
                tab.ClearSelection();
            }
        }
    }
}
