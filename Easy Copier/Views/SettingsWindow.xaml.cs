using Easy_Copier.Infrastructure;
using Easy_Copier.Services;
using Easy_Copier.ViewModels;
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

        /// <summary>
        /// The owner window used for centering and modal presentation.
        /// </summary>
        private readonly Window _owner;

        /// <summary>
        /// The startup action indicating a target section to activate upon display.
        /// </summary>
        private readonly SettingsOpenAction _openAction;

        /// <summary>
        /// The settings navigation service used to map actions and tags to Page types.
        /// </summary>
        private readonly ISettingsNavigationService _navigationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The view model instance associated with settings.</param>
        /// <param name="owner">The owner window for modal positioning.</param>
        /// <param name="openAction">The action to execute immediately upon opening settings.</param>
        /// <param name="navigationService">The navigation service for resolving pages.</param>
        public SettingsWindow(
            SettingsViewModel viewModel,
            Window owner,
            SettingsOpenAction openAction = SettingsOpenAction.None,
            ISettingsNavigationService? navigationService = null)
        {
            ViewModel = viewModel;
            _owner = owner;
            _openAction = openAction;
            _navigationService = navigationService ?? new SettingsNavigationService();

            InitializeComponent();
            SettingsRoot.DataContext = ViewModel;
            NativeWindowHelper.InitializeModalWindow(this, _owner, ViewModel, 1200, 720);

            Closed += (s, e) => SettingsClosed?.Invoke(this, EventArgs.Empty);

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
            string targetTag = _navigationService.GetTargetTagForAction(_openAction);

            foreach (object item in SettingsNav.MenuItems)
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

                Type pageType = _navigationService.GetPageTypeForTag(tag);

                if (ContentFrame.CurrentSourcePageType != pageType)
                {
                    _ = ContentFrame.Navigate(pageType, ViewModel, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
                }
            }
        }
    }
}
