using Easy_Copier.Models;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides extension methods for WinUI <see cref="GridView"/> controls used in library tab views.
    /// </summary>
    public static class LibraryViewExtensions
    {
        /// <summary>
        /// Retrieves selected <see cref="GameEntry"/> items from a <see cref="GridView"/>.
        /// </summary>
        /// <param name="gridView">The grid view instance.</param>
        /// <returns>An enumerable of selected game entries.</returns>
        public static IEnumerable<GameEntry> GetSelectedEntries(this GridView? gridView)
        {
            return gridView?.SelectedItems?.OfType<GameEntry>() ?? Enumerable.Empty<GameEntry>();
        }

        /// <summary>
        /// Safely clears active item selections from a multi-select <see cref="GridView"/>.
        /// </summary>
        /// <param name="gridView">The grid view instance.</param>
        public static void ClearMultiSelection(this GridView? gridView)
        {
            if (gridView?.SelectedItems?.Count > 0)
            {
                gridView.SelectedItems.Clear();
            }
        }
    }
}
