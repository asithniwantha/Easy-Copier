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
    /// UserControl representing the Applications tab in the library view.
    /// </summary>
    public sealed partial class AppsTabView : UserControl, ILibraryTabView
    {
        private readonly IFlyoutService _flyoutService;

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
            _flyoutService = new FlyoutService();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries()
        {
            return AppsGridView?.SelectedItems?.OfType<GameEntry>() ?? Enumerable.Empty<GameEntry>();
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            if (AppsGridView?.SelectedItems?.Count > 0)
            {
                AppsGridView.SelectedItems.Clear();
            }
        }

        private void AppsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
