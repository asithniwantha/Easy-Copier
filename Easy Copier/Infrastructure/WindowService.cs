using Easy_Copier.ViewModels;
using Easy_Copier.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Specifies actions or tabs to activate when opening the settings window.
    /// </summary>
    public enum SettingsOpenAction
    {
        /// <summary>
        /// No specific action or auto-add tab requested.
        /// </summary>
        None,

        /// <summary>
        /// Automatically prompt to add a new Game folder path.
        /// </summary>
        AddGameFolder,

        /// <summary>
        /// Automatically prompt to add a new Application folder path.
        /// </summary>
        AddAppFolder,

        /// <summary>
        /// Automatically prompt to add a new TV and Film folder path.
        /// </summary>
        AddTvAndFilmFolder,

        /// <summary>
        /// Automatically prompt to add a new OS Image folder path.
        /// </summary>
        AddOsImageFolder
    }

    /// <summary>
    /// Provides abstract methods for creating and displaying secondary windows in the application.
    /// </summary>
    public interface IWindowService
    {
        /// <summary>
        /// Opens the Settings window with optional completion callback and tab action.
        /// </summary>
        /// <param name="onClosed">Optional action invoked when the Settings window is closed.</param>
        /// <param name="openAction">The initial action to perform upon opening Settings.</param>
        void ShowSettingsWindow(Action? onClosed = null, SettingsOpenAction openAction = SettingsOpenAction.None);

        /// <summary>
        /// Opens the Copy History window.
        /// </summary>
        void ShowHistoryWindow();

        /// <summary>
        /// Opens the Smart Adder History window.
        /// </summary>
        void ShowSmartAdderHistoryWindow();

        /// <summary>
        /// Opens the About application information window.
        /// </summary>
        void ShowAboutWindow();
    }

    /// <summary>
    /// Implements window creation and activation services using Dependency Injection.
    /// </summary>
    public class WindowService : IWindowService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppWindowContext _appWindowContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve ViewModels.</param>
        /// <param name="appWindowContext">The main application window context provider.</param>
        public WindowService(IServiceProvider serviceProvider, IAppWindowContext appWindowContext)
        {
            _serviceProvider = serviceProvider;
            _appWindowContext = appWindowContext;
        }

        private Microsoft.UI.Xaml.Window? MainWindow => _appWindowContext.MainWindow as Microsoft.UI.Xaml.Window;

        /// <summary>
        /// Opens the Settings window with optional completion callback and tab action.
        /// </summary>
        /// <param name="onClosed">Optional action invoked when the Settings window is closed.</param>
        /// <param name="openAction">The initial action to perform upon opening Settings.</param>
        public void ShowSettingsWindow(Action? onClosed = null, SettingsOpenAction openAction = SettingsOpenAction.None)
        {
            SettingsViewModel viewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
            // Pass the primary application window context to modal dialogs as the owner window
            SettingsWindow settingsWindow = new(viewModel, MainWindow!, openAction);
            if (onClosed != null)
            {
                settingsWindow.SettingsClosed += (s, e) => onClosed();
            }
            settingsWindow.Activate();
        }

        /// <summary>
        /// Opens the Copy History window.
        /// </summary>
        public void ShowHistoryWindow()
        {
            HistoryViewModel viewModel = _serviceProvider.GetRequiredService<HistoryViewModel>();
            // Pass the primary application window context to modal dialogs as the owner window
            HistoryWindow historyWindow = new(viewModel, MainWindow!);
            historyWindow.Activate();
        }

        /// <summary>
        /// Opens the Smart Adder History window.
        /// </summary>
        public void ShowSmartAdderHistoryWindow()
        {
            SmartAdderHistoryViewModel viewModel = _serviceProvider.GetRequiredService<SmartAdderHistoryViewModel>();
            // Pass the primary application window context to modal dialogs as the owner window
            SmartAdderHistoryWindow smartAdderHistoryWindow = new(viewModel, MainWindow!);
            smartAdderHistoryWindow.Activate();
        }

        /// <summary>
        /// Opens the About application information window.
        /// </summary>
        public void ShowAboutWindow()
        {
            AboutViewModel viewModel = _serviceProvider.GetRequiredService<AboutViewModel>();
            // Pass the primary application window context to modal dialogs as the owner window
            AboutWindow aboutWindow = new(viewModel, MainWindow!);
            aboutWindow.Activate();
        }
    }
}
