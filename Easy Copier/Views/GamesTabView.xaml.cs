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
    /// UserControl representing the Games tab in the library view.
    /// </summary>
    public sealed partial class GamesTabView : UserControl, ILibraryTabView
    {
        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(GamesTabViewModel),
                typeof(GamesTabView),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="GamesTabViewModel"/> for this control.
        /// </summary>
        public GamesTabViewModel? ViewModel
        {
            get => (GamesTabViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Event raised when the selection in the games grid changes.
        /// </summary>
        public event SelectionChangedEventHandler? SelectionChanged;

        /// <summary>
        /// Gets the selected items from the games grid.
        /// </summary>
        public IList<object> SelectedItems => GamesGridView.SelectedItems;

        /// <inheritdoc />
        public bool IsOsImagesTab => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesTabView"/> class.
        /// </summary>
        public GamesTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries() => GamesGridView.GetSelectedEntries();

        /// <inheritdoc />
        public void ClearSelection() => GamesGridView.ClearMultiSelection();

        /// <summary>
        /// Handles the SelectionChanged event for the games grid.
        /// </summary>
        /// <param name="sender">The source of the selection changed event.</param>
        /// <param name="e">The event data describing selection modifications.</param>
        private void GamesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Handles the Click event for the Open Folder button on a game card.
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">The event arguments.</param>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutHelper.HandleOpenFolderClick(sender);
        }

        /// <summary>
        /// Handles the RightTapped event on a game card to present the details flyout.
        /// </summary>
        /// <param name="sender">The source of the right-tap event.</param>
        /// <param name="e">The event arguments containing input position details.</param>
        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await FlyoutHelper.ShowGameDetailsFlyoutAsync(sender, e);
        }
    }
}
