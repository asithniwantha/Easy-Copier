using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the OS Images tab in the library view.
    /// </summary>
    public sealed partial class OsImagesTabView : UserControl
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
        public System.Collections.Generic.IList<object> SelectedItems => OsImagesGridView.SelectedItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="OsImagesTabView"/> class.
        /// </summary>
        public OsImagesTabView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clears all selected items in the OS Images grid.
        /// </summary>
        public void ClearSelection()
        {
            OsImagesGridView.SelectedItems.Clear();
        }

        private void OsImagesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is GameEntry gameEntry && ViewModel?.MainViewModel != null)
            {
                ViewModel.MainViewModel.OpenItemFolderCommand.Execute(gameEntry.FolderPath);
            }
        }

        private void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameEntry gameEntry)
            {
                OsImageDetailsViewModel osImageVm = new();
                osImageVm.Initialize(gameEntry.Name, gameEntry.FolderPath);
                OsImageDetailsFlyout osImageFlyout = new(osImageVm);

                Flyout flyout = new()
                {
                    Content = osImageFlyout,
                    Placement = FlyoutPlacementMode.RightEdgeAlignedTop
                };

                flyout.ShowAt(fe, new FlyoutShowOptions { Position = e.GetPosition(fe) });
            }
        }
    }
}
