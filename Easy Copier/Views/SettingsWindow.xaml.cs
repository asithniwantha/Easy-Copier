using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Easy_Copier.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
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
        private readonly SettingsOpenAction _openAction;

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
            _openAction = openAction;
            InitializeComponent();
            SettingsRoot.DataContext = ViewModel;
            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 1200, 720);

            ViewModel.CloseRequested += (s, e) =>
            {
                SettingsClosed?.Invoke(this, EventArgs.Empty);
                Close();
            };

            _ = LoadAsync(openAction);

            // Adjust size to content dynamically when layout updates
            if (Content is FrameworkElement rootElement)
            {
                NativeWindowHelper.EnableDynamicResizing(this, rootElement, 1400, 840);
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
            if (openAction.ToFolderCategoryString() is string category)
            {
                await ViewModel.AddNewSourceFolderCommand.ExecuteAsync(category);
            }
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> event of the navigation view to select the target menu item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SettingsNav_Loaded(object sender, RoutedEventArgs e)
        {
            // If openAction specified a category, select the appropriate tab
            string targetTag = _openAction switch
            {
                SettingsOpenAction.AddGameFolder => "Games",
                SettingsOpenAction.AddAppFolder => "Apps",
                SettingsOpenAction.AddTvAndFilmFolder => "FilmAndTv",
                SettingsOpenAction.AddOsImageFolder => "OsImages",
                _ => "General"
            };

            foreach (var item in SettingsNav.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == targetTag)
                {
                    SettingsNav.SelectedItem = navItem;
                    return;
                }
            }

            if (SettingsNav.MenuItems.Count > 0)
            {
                SettingsNav.SelectedItem = SettingsNav.MenuItems[0];
            }
        }

        /// <summary>
        /// Handles navigation selection changes to navigate the content frame to the appropriate settings page.
        /// Applies a smooth slide navigation transition for visual fluid feedback.
        /// </summary>
        /// <param name="sender">The navigation view control.</param>
        /// <param name="args">Event arguments containing the newly selected item.</param>
        private void SettingsNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                string tag = selectedItem.Tag?.ToString() ?? string.Empty;
                ViewModel.SelectedNavTag = tag;

                Type pageType = tag switch
                {
                    "General" => typeof(GeneralSettingsPage),
                    "Games" => typeof(GamesSettingsPage),
                    "Apps" => typeof(AppsSettingsPage),
                    "FilmAndTv" => typeof(FilmAndTvSettingsPage),
                    "OsImages" => typeof(OsImagesSettingsPage),
                    "Logs" => typeof(LogsSettingsPage),
                    _ => typeof(GeneralSettingsPage)
                };

                if (ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType, ViewModel, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
                }
            }
        }
    }
}
