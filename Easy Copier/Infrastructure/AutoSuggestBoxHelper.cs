using Easy_Copier.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Helper utilities for handling WinUI <see cref="AutoSuggestBox"/> search text changes, autocomplete suggestions, query submissions, and background highlights across tab views.
    /// </summary>
    public static class AutoSuggestBoxHelper
    {
        private static readonly SolidColorBrush SearchHighlightBrush =
            new(ColorHelper.FromArgb(0x3C, 0xFF, 0xC1, 0x07));

        /// <summary>
        /// Handles text changes in an <see cref="AutoSuggestBox"/> to populate autocomplete suggestions when user input changes and update search background highlights.
        /// </summary>
        /// <param name="sender">The <see cref="AutoSuggestBox"/> control raising the event.</param>
        /// <param name="args">The text change event arguments.</param>
        /// <param name="viewModel">The searchable ViewModel providing suggestions.</param>
        public static void HandleTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args, ISearchableTabViewModel? viewModel)
        {
            if (sender == null || args == null)
            {
                return;
            }

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && viewModel != null)
            {
                sender.ItemsSource = viewModel.GetSuggestions(sender.Text);
            }

            UpdateSearchBoxBackground(sender, sender.Text);
        }

        /// <summary>
        /// Handles the suggestion chosen event from an <see cref="AutoSuggestBox"/> to update the ViewModel's search text.
        /// </summary>
        /// <param name="args">The suggestion chosen event arguments.</param>
        /// <param name="viewModel">The searchable ViewModel to update.</param>
        public static void HandleSuggestionChosen(AutoSuggestBoxSuggestionChosenEventArgs args, ISearchableTabViewModel? viewModel)
        {
            if (viewModel != null && args?.SelectedItem is string chosenText)
            {
                viewModel.SearchText = chosenText;
            }
        }

        /// <summary>
        /// Handles the query submitted event from an <see cref="AutoSuggestBox"/> to update the ViewModel's search text.
        /// </summary>
        /// <param name="args">The query submitted event arguments.</param>
        /// <param name="viewModel">The searchable ViewModel to update.</param>
        public static void HandleQuerySubmitted(AutoSuggestBoxQuerySubmittedEventArgs args, ISearchableTabViewModel? viewModel)
        {
            if (viewModel != null && args != null)
            {
                viewModel.SearchText = args.QueryText;
            }
        }

        /// <summary>
        /// Updates the background color and theme resources of the specified <see cref="AutoSuggestBox"/> based on search text activity.
        /// </summary>
        /// <param name="searchBox">The target <see cref="AutoSuggestBox"/> control.</param>
        /// <param name="text">The current search text string.</param>
        public static void UpdateSearchBoxBackground(AutoSuggestBox? searchBox, string? text)
        {
            if (searchBox == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                searchBox.Resources["TextControlBackground"] = SearchHighlightBrush;
                searchBox.Resources["TextControlBackgroundPointerOver"] = SearchHighlightBrush;
                searchBox.Resources["TextControlBackgroundFocused"] = SearchHighlightBrush;
                searchBox.Background = SearchHighlightBrush;
            }
            else
            {
                _ = searchBox.Resources.Remove("TextControlBackground");
                _ = searchBox.Resources.Remove("TextControlBackgroundPointerOver");
                _ = searchBox.Resources.Remove("TextControlBackgroundFocused");
                searchBox.ClearValue(Control.BackgroundProperty);
            }
        }
    }
}
