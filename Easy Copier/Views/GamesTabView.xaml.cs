using Easy_Copier.Models;
using Easy_Copier.Services;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the Games tab in the library view.
    /// </summary>
    public sealed partial class GamesTabView : UserControl, ILibraryTabView
    {
        private readonly IFlyoutService _flyoutService;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesTabView"/> class.
        /// </summary>
        public GamesTabView()
        {
            InitializeComponent();
            _flyoutService = new FlyoutService();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries()
        {
            return GamesGridView?.SelectedItems?.OfType<GameEntry>() ?? Enumerable.Empty<GameEntry>();
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            if (GamesGridView?.SelectedItems?.Count > 0)
            {
                GamesGridView.SelectedItems.Clear();
            }
        }

        private void GamesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _flyoutService.HandleOpenFolderClick(sender, ViewModel?.MainViewModel);
        }

        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await _flyoutService.ShowGameDetailsFlyoutAsync(sender, e, ViewModel?.MainViewModel);
        }
    }
}
