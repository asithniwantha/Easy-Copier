using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Views
{
    public sealed partial class SettingsWindow : Window
    {
        public SettingsViewModel ViewModel { get; }
        public event EventHandler? SettingsClosed;
        private readonly Window _owner;

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

        private void SettingsNav_Loaded(object sender, RoutedEventArgs e)
        {
            if (SettingsNav.MenuItems.Count > 0)
            {
                SettingsNav.SelectedItem = SettingsNav.MenuItems[0];
            }
        }

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

