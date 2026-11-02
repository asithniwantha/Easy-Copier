using Easy_Copier.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;

namespace Easy_Copier.Behaviors
{
    /// <summary>
    /// Behavior that attaches to an <see cref="AutoSuggestBox"/> to manage autocomplete suggestions,
    /// query submissions, and background highlights for an <see cref="ISearchableTabViewModel"/>.
    /// </summary>
    public sealed class AutoSuggestBoxSearchBehavior : Behavior<AutoSuggestBox>
    {
        /// <summary>
        /// The semi-transparent highlight brush applied as background for active search boxes.
        /// </summary>
        private static readonly SolidColorBrush SearchHighlightBrush =
            new(ColorHelper.FromArgb(0x3C, 0xFF, 0xC1, 0x07));

        /// <summary>
        /// Identifies the <see cref="SearchableViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchableViewModelProperty =
            DependencyProperty.Register(
                nameof(SearchableViewModel),
                typeof(ISearchableTabViewModel),
                typeof(AutoSuggestBoxSearchBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="ISearchableTabViewModel"/> instance providing suggestions and receiving queries.
        /// </summary>
        public ISearchableTabViewModel? SearchableViewModel
        {
            get => (ISearchableTabViewModel?)GetValue(SearchableViewModelProperty);
            set => SetValue(SearchableViewModelProperty, value);
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.TextChanged += OnTextChanged;
            AssociatedObject.SuggestionChosen += OnSuggestionChosen;
            AssociatedObject.QuerySubmitted += OnQuerySubmitted;
            UpdateHighlight(AssociatedObject.Text);
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            AssociatedObject.TextChanged -= OnTextChanged;
            AssociatedObject.SuggestionChosen -= OnSuggestionChosen;
            AssociatedObject.QuerySubmitted -= OnQuerySubmitted;
            base.OnDetaching();
        }

        /// <summary>
        /// Handles text changes to populate autocomplete suggestions when the user types and update highlights.
        /// </summary>
        private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && SearchableViewModel != null)
            {
                sender.ItemsSource = SearchableViewModel.GetSuggestions(sender.Text);
            }

            UpdateHighlight(sender.Text);
        }

        /// <summary>
        /// Handles chosen suggestions to assign the selected text to the ViewModel search query.
        /// </summary>
        private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (SearchableViewModel != null && args?.SelectedItem is string chosenText)
            {
                SearchableViewModel.SearchText = chosenText;
            }
        }

        /// <summary>
        /// Handles query submissions to commit the query text to the ViewModel search query.
        /// </summary>
        private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (SearchableViewModel != null && args != null)
            {
                SearchableViewModel.SearchText = args.QueryText;
            }
        }

        /// <summary>
        /// Updates the background color and theme resources of the associated <see cref="AutoSuggestBox"/> based on search text activity.
        /// </summary>
        private void UpdateHighlight(string? text)
        {
            if (AssociatedObject == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                AssociatedObject.Resources["TextControlBackground"] = SearchHighlightBrush;
                AssociatedObject.Resources["TextControlBackgroundPointerOver"] = SearchHighlightBrush;
                AssociatedObject.Resources["TextControlBackgroundFocused"] = SearchHighlightBrush;
                AssociatedObject.Background = SearchHighlightBrush;
            }
            else
            {
                _ = AssociatedObject.Resources.Remove("TextControlBackground");
                _ = AssociatedObject.Resources.Remove("TextControlBackgroundPointerOver");
                _ = AssociatedObject.Resources.Remove("TextControlBackgroundFocused");
                AssociatedObject.ClearValue(Control.BackgroundProperty);
            }
        }
    }
}

