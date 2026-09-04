using System;
using Microsoft.UI.Xaml;

namespace Easy_Copier.Infrastructure
{
    public class AppWindowContext : IAppWindowContext
    {
        public object? MainWindow => App.MainWindow;

        public object? MainXamlRoot
        {
            get
            {
                if (App.MainWindow?.Content is FrameworkElement rootElement)
                {
                    return rootElement.XamlRoot;
                }
                return null;
            }
        }
    }
}
