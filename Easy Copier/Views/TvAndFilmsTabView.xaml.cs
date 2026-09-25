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

        /// <summary>
        /// Initializes a new instance of the <see cref="TvAndFilmsTabView"/> class.
        /// </summary>
        public TvAndFilmsTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries() => TvAndFilmsGridView.GetSelectedEntries();

        /// <inheritdoc />
        public void ClearSelection() => TvAndFilmsGridView.ClearMultiSelection();

        private void TvAndFilmsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutHelper.HandleOpenFolderClick(sender, ViewModel?.MainViewModel?.OpenItemFolderCommand);
        }

        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await FlyoutHelper.ShowGameDetailsFlyoutAsync(sender, e);
        }
    }
}
