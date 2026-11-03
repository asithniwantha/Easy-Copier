using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Implements <see cref="ILibrarySelectionCoordinator"/> managing category selection dictionaries and pricing calculations.
    /// </summary>
    public class LibrarySelectionCoordinator : ILibrarySelectionCoordinator
    {
        /// <summary>
        /// Service used to retrieve application settings for pricing calculation.
        /// </summary>
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// Internal dictionary maintaining selected items per library category.
        /// </summary>
        private readonly Dictionary<LibraryCategory, List<GameEntry>> _selections = new()
        {
            [LibraryCategory.Game] = [],
            [LibraryCategory.App] = [],
            [LibraryCategory.TvAndFilm] = [],
            [LibraryCategory.OsImage] = []
        };

        /// <summary>
        /// Indicates whether the OS Images tab is currently active.
        /// </summary>
        private bool _isOsImagesTabActive;

        /// <summary>
        /// Cached application settings used for calculating item copy prices.
        /// </summary>
        private AppSettings? _cachedSettings;

        /// <inheritdoc />
        public event EventHandler? SelectionChanged;

        /// <inheritdoc />
        public event EventHandler? ClearRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibrarySelectionCoordinator"/> class.
        /// </summary>
        /// <param name="settingsService">The settings service for loading price tier configurations.</param>
        public LibrarySelectionCoordinator(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        /// <inheritdoc />
        public IReadOnlyList<GameEntry> SelectedEntries => GetEffectiveSelectedEntries();

        /// <inheritdoc />
        public int SelectedCount => SelectedEntries.Count;

        /// <inheritdoc />
        public long SelectedTotalBytes => SelectedEntries.Sum(e => e.TotalBytes);

        /// <inheritdoc />
        public int SelectedTotalPrice
        {
            get
            {
                _cachedSettings ??= _settingsService.LoadSettingsSync();
                return SelectedEntries.Sum(e => FormattingHelpers.CalculatePrice(e.TotalBytes, _cachedSettings));
            }
        }

        /// <inheritdoc />
        public bool HasSelection => SelectedCount > 0;

        /// <inheritdoc />
        public void SetCategorySelection(LibraryCategory category, IEnumerable<GameEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            _selections[category] = [.. entries];
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void SetActiveTab(bool isOsImagesTabActive)
        {
            if (_isOsImagesTabActive != isOsImagesTabActive)
            {
                _isOsImagesTabActive = isOsImagesTabActive;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            foreach (LibraryCategory key in _selections.Keys.ToList())
            {
                _selections[key].Clear();
            }

            ClearRequested?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Resolves the currently active selected entries based on tab exclusivity rules.
        /// When the OS Images tab is active, only OS image selections are returned.
        /// When any other tab is active, Game, App, and Film/TV selections are aggregated.
        /// </summary>
        private List<GameEntry> GetEffectiveSelectedEntries()
        {
            if (_isOsImagesTabActive)
            {
                return _selections[LibraryCategory.OsImage];
            }

            return [
                .. _selections[LibraryCategory.Game],
                .. _selections[LibraryCategory.App],
                .. _selections[LibraryCategory.TvAndFilm]
            ];
        }
    }
}

