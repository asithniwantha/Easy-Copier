using Easy_Copier.Models;
using System;
using System.Collections.Generic;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Coordinates item selection across library tabs, enforcing tab exclusivity rules and computing selection aggregates.
    /// </summary>
    public interface ILibrarySelectionCoordinator
    {
        /// <summary>
        /// Gets the current list of selected entries respecting active tab exclusivity rules.
        /// </summary>
        IReadOnlyList<GameEntry> SelectedEntries { get; }

        /// <summary>
        /// Gets the count of currently selected library items.
        /// </summary>
        int SelectedCount { get; }

        /// <summary>
        /// Gets the aggregate byte size of currently selected library items.
        /// </summary>
        long SelectedTotalBytes { get; }

        /// <summary>
        /// Gets the calculated monetary price of currently selected library items.
        /// </summary>
        int SelectedTotalPrice { get; }

        /// <summary>
        /// Gets a value indicating whether any library items are currently selected.
        /// </summary>
        bool HasSelection { get; }

        /// <summary>
        /// Updates the selection for a specified library category.
        /// </summary>
        /// <param name="category">The target library category.</param>
        /// <param name="entries">The collection of selected entries for this category.</param>
        void SetCategorySelection(LibraryCategory category, IEnumerable<GameEntry> entries);

        /// <summary>
        /// Updates the active tab state to enforce exclusivity between OS images and other media items.
        /// </summary>
        /// <param name="isOsImagesTabActive"><c>true</c> if the OS Images tab is currently selected; otherwise, <c>false</c>.</param>
        void SetActiveTab(bool isOsImagesTabActive);

        /// <summary>
        /// Clears selections across all categories and signals child views to reset their selection state.
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Raised when the active selection, count, byte size, or price summary changes.
        /// </summary>
        event EventHandler? SelectionChanged;

        /// <summary>
        /// Raised when all selections have been requested to be cleared.
        /// </summary>
        event EventHandler? ClearRequested;
    }
}

