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
        private readonly IAppWindowContext _appWindowContext;

        public WindowService(IServiceProvider serviceProvider, IAppWindowContext appWindowContext)
        {
            _serviceProvider = serviceProvider;
            _appWindowContext = appWindowContext;
        }

        private Microsoft.UI.Xaml.Window? MainWindow => _appWindowContext.MainWindow as Microsoft.UI.Xaml.Window;

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
