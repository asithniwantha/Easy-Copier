using CommunityToolkit.Mvvm.Input;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Partial class implementation of <see cref="MainViewModel"/> providing software update application routines.
    /// </summary>
    public sealed partial class MainViewModel
    {
        /// <summary>
        /// Restarts the application and applies any pending software update via the update service.
        /// </summary>
        [RelayCommand]
        private void RestartAndApplyUpdate()
        {
            _updateService.RestartAndApplyUpdate();
        }
    }
}
