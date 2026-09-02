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
        void ShowSmartAdderHistoryWindow();
        void ShowAboutWindow();
    }

    public class WindowService : IWindowService
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the primary active window instance of the application.
        /// Exposed as a static property to comply with analyzer rules (CA1024, CA1822)
        /// while providing access to the main application window context.
        /// </summary>
        public static Microsoft.UI.Xaml.Window? MainWindow => App.MainWindow;

        public void ShowSettingsWindow(Action? onClosed = null, SettingsOpenAction openAction = SettingsOpenAction.None)
        {
            var viewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
            // Pass the primary application window context to modal dialogs as the owner window
            SettingsWindow settingsWindow = new(viewModel, MainWindow!, openAction);
            if (onClosed != null)
            {
                settingsWindow.SettingsClosed += (s, e) => onClosed();
            }
            settingsWindow.Activate();
        }

        public void ShowHistoryWindow()
        {
            var viewModel = _serviceProvider.GetRequiredService<HistoryViewModel>();
            // Pass the primary application window context to modal dialogs as the owner window
            HistoryWindow historyWindow = new(viewModel, MainWindow!);
            historyWindow.Activate();
        }

        public void ShowSmartAdderHistoryWindow()
        {
            var viewModel = _serviceProvider.GetRequiredService<SmartAdderHistoryViewModel>();
            // Pass the primary application window context to modal dialogs as the owner window
            SmartAdderHistoryWindow smartAdderHistoryWindow = new(viewModel, MainWindow!);
            smartAdderHistoryWindow.Activate();
        }

        public void ShowAboutWindow()
        {
            var viewModel = _serviceProvider.GetRequiredService<AboutViewModel>();
            // Pass the primary application window context to modal dialogs as the owner window
            AboutWindow aboutWindow = new(viewModel, MainWindow!);
            aboutWindow.Activate();
        }
    }
}
