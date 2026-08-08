using System;
using System.Collections.Generic;

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
            var settingsWindow = new Views.SettingsWindow();
            if (onClosed != null)
            {
                settingsWindow.SettingsClosed += (s, e) => onClosed();
            }
            if (onViewModelCreated != null)
            {
                onViewModelCreated(settingsWindow.ViewModel);
            }
            settingsWindow.Activate();
        }

        public void ShowHistoryWindow()
        {
            var historyWindow = new Views.HistoryWindow();
            historyWindow.Activate();
        }
    }
}
