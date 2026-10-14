using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the Films &amp; TV series tab in the library view.
    /// </summary>
    public sealed partial class TvAndFilmsTabView : UserControl, ILibraryTabView
    {
        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(TvAndFilmsTabViewModel),
                typeof(TvAndFilmsTabView),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="TvAndFilmsTabViewModel"/> for this control.
        /// </summary>
        public TvAndFilmsTabViewModel? ViewModel
        {
            get => (TvAndFilmsTabViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Event raised when the selection in the TV and films grid changes.
        /// </summary>
        public event SelectionChangedEventHandler? SelectionChanged;

        /// <summary>
        /// Gets the selected items from the TV and films grid.
        /// </summary>
        public IList<object> SelectedItems => TvAndFilmsGridView.SelectedItems;

        /// <inheritdoc />
        public bool IsOsImagesTab => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvAndFilmsTabView"/> class.
        /// </summary>
        public TvAndFilmsTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries()
        {
            return TvAndFilmsGridView.GetSelectedEntries();
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            TvAndFilmsGridView.ClearMultiSelection();
        }

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the TV and films grid.
        /// </summary>
        /// <param name="sender">The source of the selection changed event.</param>
        /// <param name="e">The event data describing selection modifications.</param>
        private void TvAndFilmsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Handles the Click event for the Open Folder button on a TV/film card.
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">The event arguments.</param>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutHelper.HandleOpenFolderClick(sender);
        }

        /// <summary>
        /// Handles the RightTapped event on a TV/film card to present the details flyout.
        /// </summary>
        /// <param name="sender">The source of the right-tap event.</param>
        /// <param name="e">The event arguments containing input position details.</param>
        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await FlyoutHelper.ShowGameDetailsFlyoutAsync(sender, e);
        }

        /// <summary>
        /// Handles text changes in the search box to provide suggestions and update background highlights.
        /// </summary>
        private void SearchBox_TextChanged(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == Microsoft.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (ViewModel != null)
                {
                    // Update suggestions based on input
                    sender.ItemsSource = ViewModel.GetSuggestions(sender.Text);
                }
            }
            UpdateSearchBoxBackground(sender.Text);
        }

        /// <summary>
        /// Handles the event when a suggestion is chosen from the AutoSuggestBox.
        /// </summary>
        private void SearchBox_SuggestionChosen(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (ViewModel != null && args.SelectedItem is string chosenText)
            {
                ViewModel.SearchText = chosenText;
            }
        }

        /// <summary>
        /// Handles the event when the user submits a query from the AutoSuggestBox.
        /// </summary>
        private void SearchBox_QuerySubmitted(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ViewModel?.SearchText = args.QueryText;
        }

        private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush SearchHighlightBrush =
            new(Microsoft.UI.ColorHelper.FromArgb(0x3C, 0xFF, 0xC1, 0x07));

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
                SearchBox.ClearValue(Microsoft.UI.Xaml.Controls.Control.BackgroundProperty);
            }
        }
    }
}
