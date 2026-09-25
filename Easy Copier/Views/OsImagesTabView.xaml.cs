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
    /// UserControl representing the OS Images tab in the library view.
    /// </summary>
    public sealed partial class OsImagesTabView : UserControl, ILibraryTabView
    {
        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(OsImagesTabViewModel),
                typeof(OsImagesTabView),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="OsImagesTabViewModel"/> for this control.
        /// </summary>
        public OsImagesTabViewModel? ViewModel
        {
            get => (OsImagesTabViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Event raised when the selection in the OS Images grid changes.
        /// </summary>
        public event SelectionChangedEventHandler? SelectionChanged;

        /// <summary>
        /// Gets the selected items from the OS Images grid.
        /// </summary>
        public IList<object> SelectedItems => OsImagesGridView.SelectedItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="OsImagesTabView"/> class.
        /// </summary>
        public OsImagesTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries() => OsImagesGridView.GetSelectedEntries();

        /// <inheritdoc />
        public void ClearSelection()
        {
            // OsImagesGridView uses Single selection mode.
            // In single selection mode, set SelectedItem to null instead of calling SelectedItems.Clear() to prevent COMExceptions.
            if (OsImagesGridView != null)
            {
                OsImagesGridView.SelectedItem = null;
            }
        }

        private void OsImagesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutHelper.HandleOpenFolderClick(sender, ViewModel?.MainViewModel);
        }

        private void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutHelper.ShowOsImageDetailsFlyout(sender, e);
        }
    }
}
