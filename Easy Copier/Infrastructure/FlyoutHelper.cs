using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Helper class encapsulating shared flyout and card interaction behaviors for library item views.
    /// </summary>
    public static class FlyoutHelper
    {
        /// <summary>
        /// Handles the click event for opening an item's folder in File Explorer.
        /// </summary>
        /// <param name="sender">The button control triggering the click.</param>
        /// <param name="mainViewModel">The main ViewModel containing the open folder command.</param>
        public static void HandleOpenFolderClick(object sender, MainViewModel? mainViewModel)
        {
            if (sender is Button button && button.DataContext is GameEntry gameEntry && mainViewModel != null)
            {
                mainViewModel.OpenItemFolderCommand.Execute(gameEntry.FolderPath);
            }
        }

        /// <summary>
        /// Displays the game details flyout when right-tapping a game card.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped routed event args.</param>
        /// <param name="mainViewModel">The main ViewModel providing system requirement formatting and factory methods.</param>
        public static async Task ShowGameDetailsFlyoutAsync(object sender, RightTappedRoutedEventArgs e, MainViewModel? mainViewModel)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (sender is FrameworkElement fe && fe.DataContext is GameEntry gameEntry && mainViewModel != null)
            {
                string formattedText = await mainViewModel.GetFormattedSystemRequirementsAsync(gameEntry.FolderPath);
                GameDetailsViewModel gameDetailsViewModel = mainViewModel.CreateGameDetailsViewModel();
                Views.GameDetailsFlyout detailsFlyout = new(gameDetailsViewModel, formattedText, gameEntry.FolderPath);

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

        /// <summary>
        /// Displays the OS image details flyout when right-tapping an OS image card.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped routed event args.</param>
        public static void ShowOsImageDetailsFlyout(object sender, RightTappedRoutedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (sender is FrameworkElement fe && fe.DataContext is GameEntry gameEntry)
            {
                OsImageDetailsViewModel osImageVm = new();
                osImageVm.Initialize(gameEntry.Name, gameEntry.FolderPath);
                Views.OsImageDetailsFlyout osImageFlyout = new(osImageVm);

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
