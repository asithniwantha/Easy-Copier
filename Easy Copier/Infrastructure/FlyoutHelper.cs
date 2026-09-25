using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;

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
        /// <param name="openFolderCommand">The open item folder command.</param>
        public static void HandleOpenFolderClick(object sender, ICommand? openFolderCommand)
        {
            if (s_flyoutService != null)
            {
                s_flyoutService.HandleOpenFolderClick(sender, openFolderCommand);
            }
            else if (sender is Microsoft.UI.Xaml.Controls.Button { DataContext: GameEntry gameEntry } && openFolderCommand != null)
            {
                if (openFolderCommand.CanExecute(gameEntry.FolderPath))
                {
                    openFolderCommand.Execute(gameEntry.FolderPath);
                }
            }
        }

        /// <summary>
        /// Displays the game details flyout when right-tapping a game card.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped routed event args.</param>
        public static async Task ShowGameDetailsFlyoutAsync(object sender, RightTappedRoutedEventArgs e)
        {
            Point? position = sender is FrameworkElement fe && e != null ? e.GetPosition(fe) : null;
            if (s_flyoutService != null)
            {
                await s_flyoutService.ShowGameDetailsFlyoutAsync(sender, position);
            }
        }

        /// <summary>
        /// Displays the OS image details flyout when right-tapping an OS image card.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped routed event args.</param>
        public static void ShowOsImageDetailsFlyout(object sender, RightTappedRoutedEventArgs e)
        {
            Point? position = sender is FrameworkElement fe && e != null ? e.GetPosition(fe) : null;
            s_flyoutService?.ShowOsImageDetailsFlyout(sender, position);
        }
    }
}
