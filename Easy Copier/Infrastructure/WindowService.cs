using Easy_Copier.Views;
using System;

namespace Easy_Copier.Infrastructure
{
    public interface IWindowService
    {
        void ShowSettingsWindow(Action? onClosed = null, Action<ViewModels.SettingsViewModel>? onViewModelCreated = null);
        void ShowHistoryWindow();
    }

    public class WindowService : IWindowService
    {
        public void ShowSettingsWindow(Action? onClosed = null, Action<ViewModels.SettingsViewModel>? onViewModelCreated = null)
        {
            SettingsWindow settingsWindow = new();
            if (onClosed != null)
            {
                settingsWindow.SettingsClosed += (s, e) => onClosed();
            }
            onViewModelCreated?.Invoke(settingsWindow.ViewModel);
            settingsWindow.Activate();
        }

        public void ShowHistoryWindow()
        {
            HistoryWindow historyWindow = new();
            historyWindow.Activate();
        }
    }
}
