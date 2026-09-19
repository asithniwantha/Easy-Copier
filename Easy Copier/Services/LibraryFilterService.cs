using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service that handles filtering and sorting operations for library entries.
    /// </summary>
    public class LibraryFilterService : ILibraryFilterService
    {
        /// <inheritdoc />
        public IEnumerable<GameEntry> FilterEntries(IEnumerable<GameEntry> source, string? query, GameCategory categoryFilter)
        {
            ArgumentNullException.ThrowIfNull(source);

            string searchText = query?.Trim() ?? string.Empty;

            IEnumerable<GameEntry> filtered = string.IsNullOrEmpty(searchText)
                ? source
                : source.Where(g => g.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            if (categoryFilter != GameCategory.All)
            {
                filtered = filtered.Where(g => g.Categories != null && g.Categories.Contains(categoryFilter));
            }

            return filtered;
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> SortOsImages(IEnumerable<GameEntry> entries, OsImageSortOption sortOption, bool isAscending)
        {
            ArgumentNullException.ThrowIfNull(entries);

            return sortOption switch
            {
                OsImageSortOption.Name => isAscending
                    ? entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    : entries.OrderByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
                OsImageSortOption.DateCreated => isAscending
                    ? entries.OrderBy(e => e.DateCreated).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    : entries.OrderByDescending(e => e.DateCreated).ThenByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
                OsImageSortOption.Size => isAscending
                    ? entries.OrderBy(e => e.TotalBytes).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    : entries.OrderByDescending(e => e.TotalBytes).ThenByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
                _ => entries
            };
        }
    }
}
