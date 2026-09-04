using CommunityToolkit.Mvvm.Input;

namespace Easy_Copier.ViewModels
{
    public sealed partial class MainViewModel
    {
        [RelayCommand]
        private void RestartAndApplyUpdate()
        {
            _updateService.RestartAndApplyUpdate();
        }
    }
}
