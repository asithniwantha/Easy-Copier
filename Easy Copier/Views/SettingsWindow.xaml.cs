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

        public SettingsWindow()
        {
            ViewModel = AppServiceLocator.GetService<SettingsViewModel>();
            InitializeComponent();

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            await ViewModel.LoadSettingsAsync();
        }

        private async void RemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string folderPath)
            {
                await ViewModel.RemoveSourceFolderCommand.ExecuteAsync(folderPath);
            }
        }

        private async void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveSettingsCommand.ExecuteAsync(null);
            SettingsClosed?.Invoke(this, EventArgs.Empty);
            Close();
        }
    }
}
