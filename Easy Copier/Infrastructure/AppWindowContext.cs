using Microsoft.UI.Xaml;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides access to the main application window context and XAML root element.
    /// </summary>
    public class AppWindowContext : IAppWindowContext
    {
        /// <summary>
        /// Gets the current main application window object.
        /// </summary>
        public object? MainWindow => App.MainWindow;

        /// <summary>
        /// Gets the main XAML root element of the main window content, if available.
        /// </summary>
        public object? MainXamlRoot => App.MainWindow?.Content is FrameworkElement rootElement ? rootElement.XamlRoot : (object?)null;
    }
}
