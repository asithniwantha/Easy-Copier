using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for displaying right-click item details flyouts and handling item folder navigation.
    /// Decouples view interaction handling from infrastructure layers.
    /// </summary>
    public interface IFlyoutService
    {
        /// <summary>
        /// Handles folder open actions for library item cards in File Explorer.
        /// </summary>
        /// <param name="sender">The control triggering the click action.</param>
        /// <param name="mainViewModel">The parent main ViewModel.</param>
        void HandleOpenFolderClick(object sender, MainViewModel? mainViewModel);

        /// <summary>
        /// Displays the game and app details flyout when right-tapping an item card.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped routed event args.</param>
        /// <param name="mainViewModel">The main ViewModel providing requirement formatting and ViewModel factory.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ShowGameDetailsFlyoutAsync(object sender, RightTappedRoutedEventArgs e, MainViewModel? mainViewModel);

        /// <summary>
        /// Displays the OS image details flyout when right-tapping an OS image card.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped routed event args.</param>
        void ShowOsImageDetailsFlyout(object sender, RightTappedRoutedEventArgs e);
    }
}
