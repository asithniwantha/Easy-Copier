using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Represents the settings dialog window for configuring source folders and application preferences.
    /// </summary>
    public sealed partial class SettingsWindow : Window
    {
        /// <summary>
        /// Gets the view model managing the settings state and operations.
        /// </summary>
        public SettingsViewModel ViewModel { get; }

        /// <summary>
        /// Occurs when the settings window requested closure.
        /// </summary>
        public event EventHandler? SettingsClosed;

        private readonly Window _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The view model instance associated with settings.</param>
        /// <param name="owner">The owner window for modal positioning.</param>
        /// <param name="openAction">The action to execute immediately upon opening settings.</param>
        public SettingsWindow(SettingsViewModel viewModel, Window owner, SettingsOpenAction openAction = SettingsOpenAction.None)
        {
            ViewModel = viewModel;
            _owner = owner;
            InitializeComponent();
            SettingsRoot.DataContext = ViewModel;
            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 960, 720);

            ViewModel.CloseRequested += (s, e) =>
            {
                SettingsClosed?.Invoke(this, EventArgs.Empty);
                Close();
            };

            _ = LoadAsync(openAction);


            // Adjust size to content dynamically when layout updates
            if (Content is FrameworkElement rootElement)
            {
                NativeWindowHelper.EnableDynamicResizing(this, rootElement, 960, 640);
            }
        }

        /// <summary>
        /// Asynchronously loads settings and executes any initial action specified when opening settings.
        /// </summary>
        /// <param name="openAction">The open action indicating if a specific tab/dialog should be triggered.</param>
        /// <returns>A task representing the asynchronous load operation.</returns>
        private async Task LoadAsync(SettingsOpenAction openAction)
        {
            await ViewModel.LoadSettingsAsync();
            if (openAction == SettingsOpenAction.AddAppFolder)
            {
                await ViewModel.AddNewSourceFolderCommand.ExecuteAsync("App");
            }
            else if (openAction == SettingsOpenAction.AddGameFolder)
            {
                await ViewModel.AddNewSourceFolderCommand.ExecuteAsync("Game");
            }
            else if (openAction == SettingsOpenAction.AddTvAndFilmFolder)
            {
                await ViewModel.AddNewSourceFolderCommand.ExecuteAsync("TvAndFilm");
            }
            else if (openAction == SettingsOpenAction.AddOsImageFolder)
            {
                await ViewModel.AddNewSourceFolderCommand.ExecuteAsync("OsImage");
            }
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> event of the navigation view to select the default menu item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SettingsNav_Loaded(object sender, RoutedEventArgs e)
        {
            if (SettingsNav.MenuItems.Count > 0)
            {
                SettingsNav.SelectedItem = SettingsNav.MenuItems[0];
            }
        }

        /// <summary>
        /// Handles navigation selection changes to switch active setting sections.
        /// </summary>
        /// <param name="sender">The navigation view control.</param>
        /// <param name="args">Event arguments containing the newly selected item.</param>
        private void SettingsNav_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is Microsoft.UI.Xaml.Controls.NavigationViewItem selectedItem)
            {
                string tag = selectedItem.Tag?.ToString() ?? string.Empty;
                ViewModel.SelectedNavTag = tag;
            }
        }
    }
}
