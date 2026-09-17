using Easy_Copier.Models;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides an abstraction for displaying UI dialogs and prompts from ViewModels.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Displays a folder conflict resolution dialog allowing the user to select how to handle duplicate items.
        /// </summary>
        /// <param name="itemName">The name of the conflicting item.</param>
        /// <param name="srcSize">The total byte size of the source item.</param>
        /// <param name="srcCount">The total file count in the source item.</param>
        /// <param name="destSize">The total byte size of the existing destination item.</param>
        /// <param name="destCount">The total file count in the existing destination item.</param>
        /// <returns>A tuple containing the selected <see cref="CopyAction"/> and a boolean indicating if the action applies to all subsequent conflicts.</returns>
        Task<(CopyAction Action, bool ApplyToAll)> ShowConflictDialogAsync(string itemName, long srcSize, int srcCount, long destSize, int destCount);

        /// <summary>
        /// Displays a standard modal message dialog with a title, message body, and close button text.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The body text of the message.</param>
        /// <param name="closeButtonText">The text displayed on the close button. Defaults to "OK".</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ShowMessageDialogAsync(string title, string message, string closeButtonText = "OK");
    }
}
