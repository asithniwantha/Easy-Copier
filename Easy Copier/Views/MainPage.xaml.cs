using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Views
{
    public sealed partial class MainPage : Page
    {
        private static readonly string[] LineSeparators = ["\r\n", "\n"];

        public MainViewModel ViewModel { get; private set; } = null!;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            base.OnNavigatedTo(e);
            if (e.Parameter is MainViewModel viewModel)
            {
                ViewModel = viewModel;
                DataContext = ViewModel;
                ViewModel.ItemQueued += (s, args) => ClearGameSelection();
                Bindings.Update();
                _ = ViewModel.InitializeAsync();
            }
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            ClearGameSelection();
        }

        private void GamesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void AppsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void TvAndFilmsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void UpdateCombinedSelection()
        {
            IEnumerable<GameEntry> tvItems = TvAndFilmsGridView?.SelectedItems.Cast<GameEntry>() ?? [];
            IEnumerable<GameEntry> selectedItems = GamesGridView.SelectedItems.Cast<GameEntry>()
                .Concat(AppsGridView.SelectedItems.Cast<GameEntry>())
                .Concat(tvItems);
            ViewModel.UpdateSelectionSummary(selectedItems);
        }

        private void ClearGameSelection()
        {
            GamesGridView.SelectedItems.Clear();
            AppsGridView.SelectedItems.Clear();
        }

        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameEntry gameEntry)
            {
                string formattedText = await ViewModel.GetFormattedSystemRequirementsAsync(gameEntry.FolderPath);

                GameDetailsFlyout detailsFlyout = new(formattedText, gameEntry.FolderPath);

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
