using Easy_Copier.ViewModels;
using Easy_Copier.Views;
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
    /// Extension methods for <see cref="SettingsOpenAction"/>.
    /// </summary>
    public static class SettingsOpenActionExtensions
    {
        /// <summary>
        /// Maps the <see cref="SettingsOpenAction"/> enum value to its corresponding folder category string parameter.
        /// </summary>
        /// <param name="action">The settings open action value.</param>
        /// <returns>The category string if matching an add action, or <c>null</c> if no action is specified.</returns>
        public static string? ToFolderCategoryString(this SettingsOpenAction action)
        {
            return action switch
            {
                SettingsOpenAction.AddAppFolder => "App",
                SettingsOpenAction.AddGameFolder => "Game",
                SettingsOpenAction.AddTvAndFilmFolder => "TvAndFilm",
                SettingsOpenAction.AddOsImageFolder => "OsImage",
                _ => null
            };
        }
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

        /// <summary>
        /// Opens the Duplicate Manager window.
        /// </summary>
        void ShowDuplicateManagerWindow();
    }

    /// <summary>
    /// Implements window creation and activation services using strongly-typed ViewModel factory delegates.
    /// </summary>
    public class WindowService : IWindowService
    {
        /// <summary>
        /// Factory delegate for creating transient <see cref="SettingsViewModel"/> instances.
        /// </summary>
        private readonly Func<SettingsViewModel> _settingsViewModelFactory;

        /// <summary>
        /// Factory delegate for creating transient <see cref="HistoryViewModel"/> instances.
        /// </summary>
        private readonly Func<HistoryViewModel> _historyViewModelFactory;

        /// <summary>
        /// Factory delegate for creating transient <see cref="SmartAdderHistoryViewModel"/> instances.
        /// </summary>
        private readonly Func<SmartAdderHistoryViewModel> _smartAdderHistoryViewModelFactory;

        /// <summary>
        /// Factory delegate for creating transient <see cref="AboutViewModel"/> instances.
        /// </summary>
        private readonly Func<AboutViewModel> _aboutViewModelFactory;

        /// <summary>
        /// Factory delegate for creating transient <see cref="DuplicateManagerViewModel"/> instances.
        /// </summary>
        private readonly Func<DuplicateManagerViewModel> _duplicateManagerViewModelFactory;

        /// <summary>
        /// The application window context provider used for retrieving the main window.
        /// </summary>
        private readonly IAppWindowContext _appWindowContext;

        /// <summary>
        /// The settings navigation service used for resolving settings pages.
        /// </summary>
        private readonly Services.ISettingsNavigationService _settingsNavigationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowService"/> class using typed ViewModel factories.
        /// </summary>
        /// <param name="settingsViewModelFactory">Factory delegate for resolving <see cref="SettingsViewModel"/> instances.</param>
        /// <param name="historyViewModelFactory">Factory delegate for resolving <see cref="HistoryViewModel"/> instances.</param>
        /// <param name="smartAdderHistoryViewModelFactory">Factory delegate for resolving <see cref="SmartAdderHistoryViewModel"/> instances.</param>
        /// <param name="aboutViewModelFactory">Factory delegate for resolving <see cref="AboutViewModel"/> instances.</param>
        /// <param name="duplicateManagerViewModelFactory">Factory delegate for resolving <see cref="DuplicateManagerViewModel"/> instances.</param>
        /// <param name="appWindowContext">The main application window context provider.</param>
        /// <param name="settingsNavigationService">The settings navigation service dependency.</param>
        public WindowService(
            Func<SettingsViewModel> settingsViewModelFactory,
            Func<HistoryViewModel> historyViewModelFactory,
            Func<SmartAdderHistoryViewModel> smartAdderHistoryViewModelFactory,
            Func<AboutViewModel> aboutViewModelFactory,
            Func<DuplicateManagerViewModel> duplicateManagerViewModelFactory,
            IAppWindowContext appWindowContext,
            Services.ISettingsNavigationService settingsNavigationService)
        {
            _settingsViewModelFactory = settingsViewModelFactory ?? throw new ArgumentNullException(nameof(settingsViewModelFactory));
            _historyViewModelFactory = historyViewModelFactory ?? throw new ArgumentNullException(nameof(historyViewModelFactory));
            _smartAdderHistoryViewModelFactory = smartAdderHistoryViewModelFactory ?? throw new ArgumentNullException(nameof(smartAdderHistoryViewModelFactory));
            _aboutViewModelFactory = aboutViewModelFactory ?? throw new ArgumentNullException(nameof(aboutViewModelFactory));
            _duplicateManagerViewModelFactory = duplicateManagerViewModelFactory ?? throw new ArgumentNullException(nameof(duplicateManagerViewModelFactory));
            _appWindowContext = appWindowContext ?? throw new ArgumentNullException(nameof(appWindowContext));
            _settingsNavigationService = settingsNavigationService ?? throw new ArgumentNullException(nameof(settingsNavigationService));
        }

        /// <summary>
        /// Gets the primary application window reference cast as a <see cref="Microsoft.UI.Xaml.Window"/>.
        /// </summary>
        private Microsoft.UI.Xaml.Window? MainWindow => _appWindowContext.MainWindow as Microsoft.UI.Xaml.Window;

        /// <summary>
        /// Opens the Settings window with optional completion callback and tab action.
        /// </summary>
        /// <param name="onClosed">Optional action invoked when the Settings window is closed.</param>
        /// <param name="openAction">The initial action to perform upon opening Settings.</param>
        public void ShowSettingsWindow(Action? onClosed = null, SettingsOpenAction openAction = SettingsOpenAction.None)
        {
            // Resolve a fresh transient SettingsViewModel instance via typed factory delegate (no service locator)
            SettingsViewModel viewModel = _settingsViewModelFactory();
            // Pass the primary application window context to modal dialogs as the owner window
            SettingsWindow settingsWindow = new(viewModel, MainWindow!, openAction, _settingsNavigationService);
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
            // Resolve a fresh transient HistoryViewModel instance via typed factory delegate
            HistoryViewModel viewModel = _historyViewModelFactory();
            // Pass the primary application window context to modal dialogs as the owner window
            HistoryWindow historyWindow = new(viewModel, MainWindow!);
            historyWindow.Activate();
        }

        /// <summary>
        /// Opens the Smart Adder History window.
        /// </summary>
        public void ShowSmartAdderHistoryWindow()
        {
            // Resolve a fresh transient SmartAdderHistoryViewModel instance via typed factory delegate
            SmartAdderHistoryViewModel viewModel = _smartAdderHistoryViewModelFactory();
            // Pass the primary application window context to modal dialogs as the owner window
            SmartAdderHistoryWindow smartAdderHistoryWindow = new(viewModel, MainWindow!);
            smartAdderHistoryWindow.Activate();
        }

        /// <summary>
        /// Opens the About application information window.
        /// </summary>
        public void ShowAboutWindow()
        {
            // Resolve a fresh transient AboutViewModel instance via typed factory delegate
            AboutViewModel viewModel = _aboutViewModelFactory();
            // Pass the primary application window context to modal dialogs as the owner window
            AboutWindow aboutWindow = new(viewModel, MainWindow!);
            aboutWindow.Activate();
        }

        /// <summary>
        /// Opens the Duplicate Manager window.
        /// </summary>
        public void ShowDuplicateManagerWindow()
        {
            // Resolve a fresh transient DuplicateManagerViewModel instance via typed factory delegate
            DuplicateManagerViewModel viewModel = _duplicateManagerViewModelFactory();
            // Pass the primary application window context to modal dialogs as the owner window
            DuplicateManagerWindow duplicateManagerWindow = new(viewModel, MainWindow!);
            duplicateManagerWindow.Activate();
        }
    }
}
