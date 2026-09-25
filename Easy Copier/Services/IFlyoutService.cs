using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines methods for presenting item detail flyouts and handling folder launch actions in library views.
    /// </summary>
    public interface IFlyoutService
    {
        /// <summary>
        /// Handles button click events to open an item's folder in File Explorer using the provided open folder command.
        /// </summary>
        /// <param name="sender">The control triggering the click.</param>
        /// <param name="openFolderCommand">The command to execute for opening the item folder.</param>
        void HandleOpenFolderClick(object sender, ICommand? openFolderCommand);

        /// <summary>
        /// Displays the game/app/media details flyout asynchronously when an item card is right-tapped.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="position">The optional point coordinates relative to the sender for flyout placement.</param>
        /// <returns>A task representing the asynchronous flyout presentation.</returns>
        Task ShowGameDetailsFlyoutAsync(object sender, Point? position);

        /// <summary>
        /// Displays the OS image details flyout when an OS image card is right-tapped.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="position">The optional point coordinates relative to the sender for flyout placement.</param>
        void ShowOsImageDetailsFlyout(object sender, Point? position);
    }
}
