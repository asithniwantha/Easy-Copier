using Easy_Copier.Views;
using System;

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
    }

    public class WindowService : IWindowService
    {
        public void ShowSettingsWindow(Action? onClosed = null, SettingsOpenAction openAction = SettingsOpenAction.None)
        {
            SettingsWindow settingsWindow = new(openAction);
            if (onClosed != null)
            {
                settingsWindow.SettingsClosed += (s, e) => onClosed();
            }
            settingsWindow.Activate();
        }

        public void ShowHistoryWindow()
        {
            HistoryWindow historyWindow = new();
            historyWindow.Activate();
        }
    }
}
