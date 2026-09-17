using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the Games tab in the library view.
    /// </summary>
    public sealed partial class GamesTabView : UserControl
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
        public System.Collections.Generic.IList<object> SelectedItems => GamesGridView.SelectedItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesTabView"/> class.
        /// </summary>
        public GamesTabView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clears all selected items in the games grid.
        /// </summary>
        public void ClearSelection()
        {
            GamesGridView.SelectedItems.Clear();
        }

        private void GamesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameEntry gameEntry && ViewModel?.MainViewModel != null)
            {
                string formattedText = await ViewModel.MainViewModel.GetFormattedSystemRequirementsAsync(gameEntry.FolderPath);
                GameDetailsViewModel gameDetailsViewModel = ViewModel.MainViewModel.CreateGameDetailsViewModel();
                GameDetailsFlyout detailsFlyout = new(gameDetailsViewModel, formattedText, gameEntry.FolderPath);

                Style flyoutStyle = new(typeof(FlyoutPresenter));
                flyoutStyle.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, double.PositiveInfinity));

                Flyout flyout = new()
                {
                    Content = detailsFlyout,
                    Placement = FlyoutPlacementMode.RightEdgeAlignedTop,
                    FlyoutPresenterStyle = flyoutStyle
                };

                flyout.ShowAt(fe, new FlyoutShowOptions { Position = e.GetPosition(fe) });
            }
        }
    }
}
