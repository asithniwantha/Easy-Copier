using System;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Contract implemented by ViewModels that can request their containing dialog or window to close.
    /// </summary>
    public interface ICloseableViewModel
    {
        /// <summary>
        /// Occurs when the ViewModel requests the containing window or dialog to close.
        /// </summary>
        event EventHandler? CloseRequested;

        /// <summary>
        /// Requests closure of the containing window or dialog.
        /// </summary>
        void RequestClose();
    }
}

