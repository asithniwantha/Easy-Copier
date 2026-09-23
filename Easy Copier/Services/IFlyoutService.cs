using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines methods for presenting item detail flyouts and handling folder launch actions in library views.
    /// </summary>
    public interface IFlyoutService
    {
        /// <summary>
        /// Handles button click events to open an item's folder in File Explorer.
        /// </summary>
        /// <param name="sender">The control triggering the click.</param>
        /// <param name="mainViewModel">The main ViewModel providing the open item folder command.</param>
        void HandleOpenFolderClick(object sender, MainViewModel? mainViewModel);

        /// <summary>
        /// Displays the game/app/media details flyout asynchronously when an item card is right-tapped.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped event arguments.</param>
        /// <param name="mainViewModel">The main ViewModel providing requirement formatting and ViewModel factory.</param>
        /// <returns>A task representing the asynchronous flyout presentation.</returns>
        Task ShowGameDetailsFlyoutAsync(object sender, RightTappedRoutedEventArgs e, MainViewModel? mainViewModel);

        /// <summary>
        /// Displays the OS image details flyout when an OS image card is right-tapped.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped event arguments.</param>
        void ShowOsImageDetailsFlyout(object sender, RightTappedRoutedEventArgs e);
    }
}
