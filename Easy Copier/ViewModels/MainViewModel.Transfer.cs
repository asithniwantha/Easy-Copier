using CommunityToolkit.Mvvm.Input;

namespace Easy_Copier.ViewModels
{
    public sealed partial class MainViewModel
    {
        /// <summary>
        /// Restarts the application and applies the pending downloaded update.
        /// </summary>
        [RelayCommand]
        private void RestartAndApplyUpdate()
        {
            _updateService.RestartAndApplyUpdate();
        }
    }
}
