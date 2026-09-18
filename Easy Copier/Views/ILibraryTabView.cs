using Easy_Copier.Models;
using System.Collections.Generic;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Contract implemented by library tab user controls for unified selection management.
    /// </summary>
    public interface ILibraryTabView
    {
        /// <summary>
        /// Retrieves the collection of currently selected <see cref="GameEntry"/> items in this view.
        /// </summary>
        /// <returns>An enumerable of selected game entries.</returns>
        IEnumerable<GameEntry> GetSelectedEntries();

        /// <summary>
        /// Clears all active item selections in this view.
        /// </summary>
        void ClearSelection();
    }
}
