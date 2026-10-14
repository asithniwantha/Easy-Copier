using System.Collections.Generic;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Contract for library tab ViewModels that support search text query binding and autocomplete suggestions.
    /// </summary>
    public interface ISearchableTabViewModel
    {
        /// <summary>
        /// Gets or sets the search filter text query.
        /// </summary>
        string SearchText { get; set; }

        /// <summary>
        /// Retrieves autocomplete suggestions matching the specified query string.
        /// </summary>
        /// <param name="query">The user input text query.</param>
        /// <returns>An enumeration of matching unique item names.</returns>
        IEnumerable<string> GetSuggestions(string query);
    }
}
