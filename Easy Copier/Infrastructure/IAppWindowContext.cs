namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Defines a contract for accessing window and XAML root context UI elements without direct coupling.
    /// </summary>
    public interface IAppWindowContext
    {
        /// <summary>
        /// Gets the primary application window reference.
        /// </summary>
        object? MainWindow { get; }

        /// <summary>
        /// Gets the primary XAML root object for displaying content dialogs and overlays.
        /// </summary>
        object? MainXamlRoot { get; }
    }
}
