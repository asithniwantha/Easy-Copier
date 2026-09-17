using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the Applications tab in the library view.
    /// </summary>
    public sealed partial class AppsTabView : UserControl
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
        public System.Collections.Generic.IList<object> SelectedItems => AppsGridView.SelectedItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppsTabView"/> class.
        /// </summary>
        public AppsTabView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clears all selected items in the apps grid.
        /// </summary>
        public void ClearSelection()
        {
            AppsGridView.SelectedItems.Clear();
        }

        private void AppsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutHelper.HandleOpenFolderClick(sender, ViewModel?.MainViewModel);
        }

        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await FlyoutHelper.ShowGameDetailsFlyoutAsync(sender, e, ViewModel?.MainViewModel);
        }
    }
}
