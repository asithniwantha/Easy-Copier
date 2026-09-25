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

        private void OnItemQueued(object? sender, EventArgs e) => ClearGameSelection();

        private void OnClearSelectionRequested(object? sender, EventArgs e) => ClearGameSelection();

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            UpdateSearchBoxBackground(sender?.Text);
        }

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
            if (LibraryPivot == null)
            {
                return [];
            }

            return LibraryPivot.Items
                .OfType<PivotItem>()
                .Select(p => p.Content)
                .OfType<ILibraryTabView>();
        }

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
                    .Where(tab => tab is not OsImagesTabView)
                    .SelectMany(tab => tab.GetSelectedEntries());
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
