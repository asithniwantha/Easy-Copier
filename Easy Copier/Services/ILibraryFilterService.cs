using Easy_Copier.Models;
using System.Collections.Generic;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for filtering and sorting library items (games, apps, films/TV, OS images).
    /// </summary>
    public interface ILibraryFilterService
    {
        /// <summary>
        /// Filters a collection of entries based on a search text query and category filter.
        /// </summary>
        /// <param name="source">The source collection of entries.</param>
        /// <param name="query">The text search query.</param>
        /// <param name="categoryFilter">The category filter to match.</param>
        /// <returns>An enumerable of filtered entries.</returns>
        IEnumerable<GameEntry> FilterEntries(IEnumerable<GameEntry> source, string? query, GameCategory categoryFilter);

        /// <summary>
        /// Sorts OS image entries based on the specified sort option and direction.
        /// </summary>
        /// <param name="entries">The OS image entries to sort.</param>
        /// <param name="sortOption">The sorting option to apply.</param>
        /// <param name="isAscending">Whether sorting should be in ascending order.</param>
        /// <returns>An enumerable of sorted OS image entries.</returns>
        IEnumerable<GameEntry> SortOsImages(IEnumerable<GameEntry> entries, OsImageSortOption sortOption, bool isAscending);
    }
}
