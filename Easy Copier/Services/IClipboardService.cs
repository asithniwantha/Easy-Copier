namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines an abstraction for system clipboard operations, decoupling ViewModels from platform-specific clipboard APIs.
    /// </summary>
    public interface IClipboardService
    {
        /// <summary>
        /// Copies the specified text string to the system clipboard.
        /// </summary>
        /// <param name="text">The text content to place on the clipboard.</param>
        void SetText(string text);
    }
}

