using Easy_Copier.Views;
using Easy_Copier.ViewModels;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Easy_Copier.Infrastructure
{
    public enum SettingsOpenAction
    {
        None,
        AddGameFolder,
        AddAppFolder,
        AddTvAndFilmFolder
    }

    public interface IWindowService
    {
        void ShowSettingsWindow(Action? onClosed = null, SettingsOpenAction openAction = SettingsOpenAction.None);
        void ShowHistoryWindow();
        Microsoft.UI.Xaml.Window? GetMainWindow();
    }

    public class WindowService : IWindowService
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Microsoft.UI.Xaml.Window? GetMainWindow()
        {
            return App.MainWindow;
        }

        public void ShowSettingsWindow(Action? onClosed = null, SettingsOpenAction openAction = SettingsOpenAction.None)
        {
            var viewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
            SettingsWindow settingsWindow = new(viewModel, openAction);
            if (onClosed != null)
            {
                settingsWindow.SettingsClosed += (s, e) => onClosed();
            }
            settingsWindow.Activate();
        }

        public void ShowHistoryWindow()
        {
            var viewModel = _serviceProvider.GetRequiredService<HistoryViewModel>();
            HistoryWindow historyWindow = new(viewModel);
            historyWindow.Activate();
        }
    }
}
