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
    /// UserControl representing the Applications tab in the library view.
    /// </summary>
    public sealed partial class AppsTabView : UserControl, ILibraryTabView
    {
        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(AppsTabViewModel),
                typeof(AppsTabView),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="AppsTabViewModel"/> for this control.
        /// </summary>
        public AppsTabViewModel? ViewModel
        {
            get => (AppsTabViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Event raised when the selection in the apps grid changes.
        /// </summary>
        public event SelectionChangedEventHandler? SelectionChanged;

        /// <summary>
        /// Gets the selected items from the apps grid.
        /// </summary>
        public IList<object> SelectedItems => AppsGridView.SelectedItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppsTabView"/> class.
        /// </summary>
        public AppsTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries() => AppsGridView.GetSelectedEntries();

        /// <inheritdoc />
        public void ClearSelection() => AppsGridView.ClearMultiSelection();

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the applications grid.
        /// </summary>
        /// <param name="sender">The source of the selection changed event.</param>
        /// <param name="e">The event data describing selection modifications.</param>
        private void AppsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Handles the Click event for the Open Folder button on an application card.
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">The event arguments.</param>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutHelper.HandleOpenFolderClick(sender, ViewModel?.MainViewModel);
        }

        /// <summary>
        /// Handles the RightTapped event on an application card to present the details flyout.
        /// </summary>
        /// <param name="sender">The source of the right-tap event.</param>
        /// <param name="e">The event arguments containing input position details.</param>
        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await FlyoutHelper.ShowGameDetailsFlyoutAsync(sender, e, ViewModel?.MainViewModel);
        }
    }
}
