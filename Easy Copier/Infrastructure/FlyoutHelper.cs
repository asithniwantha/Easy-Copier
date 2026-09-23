using Easy_Copier.Models;
using Easy_Copier.Services;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Encapsulates shared flyout and card interaction behaviors for library item views, delegating to <see cref="IFlyoutService"/>.
    /// </summary>
    public static class FlyoutHelper
    {
        private static IFlyoutService? s_flyoutService;

        /// <summary>
        /// Configures the static helper with an <see cref="IFlyoutService"/> instance for UI views that use static helper methods.
        /// </summary>
        /// <param name="flyoutService">The flyout service instance.</param>
        public static void Initialize(IFlyoutService flyoutService)
        {
            s_flyoutService = flyoutService;
        }

        /// <summary>
        /// Handles the click event for opening an item's folder in File Explorer.
        /// </summary>
        /// <param name="sender">The button control triggering the click.</param>
        /// <param name="mainViewModel">The main ViewModel containing the open folder command.</param>
        public static void HandleOpenFolderClick(object sender, MainViewModel? mainViewModel)
        {
            if (s_flyoutService != null)
            {
                s_flyoutService.HandleOpenFolderClick(sender, mainViewModel);
            }
            else if (sender is Microsoft.UI.Xaml.Controls.Button { DataContext: GameEntry gameEntry } && mainViewModel != null)
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
            if (s_flyoutService != null)
            {
                await s_flyoutService.ShowGameDetailsFlyoutAsync(sender, e, mainViewModel);
            }
        }

        /// <summary>
        /// Displays the OS image details flyout when right-tapping an OS image card.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped routed event args.</param>
        public static void ShowOsImageDetailsFlyout(object sender, RightTappedRoutedEventArgs e)
        {
            s_flyoutService?.ShowOsImageDetailsFlyout(sender, e);
        }
    }
}
