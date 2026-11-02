using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Base class for ViewModels associated with dialogs or modal windows, providing unified close request handling and commands.
    /// </summary>
    public abstract partial class CloseableViewModelBase : ObservableObject, ICloseableViewModel
    {
        /// <inheritdoc />
        public event EventHandler? CloseRequested;

        /// <inheritdoc />
        public void RequestClose()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Requests closure of the containing window or dialog.
        /// </summary>
        [RelayCommand]
        protected virtual void CloseWindow()
        {
            RequestClose();
        }
    }
}

