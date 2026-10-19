using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for interacting with UI folder selection picker dialogs.
    /// </summary>
    public interface IFolderPickerService
    {
        /// <summary>
        /// Prompts the user to select a folder from the operating system folder selection dialog.
        /// </summary>
        /// <param name="startingDirectory">The optional starting directory path.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the chosen directory folder path, or <see langword="null"/> if cancelled.</returns>
        Task<string?> PickFolderAsync(string? startingDirectory = null);
    }
}
