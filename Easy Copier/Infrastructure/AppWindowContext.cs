using Microsoft.UI.Xaml;

namespace Easy_Copier.Infrastructure
{
    public class AppWindowContext : IAppWindowContext
    {
        public object? MainWindow => App.MainWindow;

        public object? MainXamlRoot => App.MainWindow?.Content is FrameworkElement rootElement ? rootElement.XamlRoot : (object?)null;
    }
}
