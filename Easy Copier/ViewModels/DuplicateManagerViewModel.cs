using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Represents an observable view model for a single duplicate file or folder candidate.
    /// </summary>
    public sealed partial class DuplicateItemEntryViewModel : ObservableObject
    {
        /// <summary>
        /// Callback action invoked whenever <see cref="IsSelectedForDeletion"/> changes to update parent selection state.
        /// </summary>
        private readonly Action _onSelectionChanged;

        /// <summary>
        /// Gets the name of the duplicate item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the full directory or file path of the duplicate.
        /// </summary>
        public string FolderPath { get; }

        /// <summary>
        /// Gets the library content category.
        /// </summary>
        public LibraryCategory Category { get; }

        /// <summary>
        /// Gets the size of the item in bytes.
        /// </summary>
        public long TotalBytes { get; }

        /// <summary>
        /// Gets the creation or last modified date.
        /// </summary>
        public DateTime DateCreated { get; }

        /// <summary>
        /// Gets the formatted size string (e.g. "45.2 GB").
        /// </summary>
        public string FormattedSize => FormattingHelpers.FormatBytes(TotalBytes);

        /// <summary>
        /// Gets the formatted date string.
        /// </summary>
        public string FormattedDate => DateCreated.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets the category icon glyph for visual indication.
        /// </summary>
        public string CategoryGlyph => Category switch
        {
            LibraryCategory.Game => "\uE7FC",
            LibraryCategory.App => "\uE71D",
            LibraryCategory.TvAndFilm => "\uE714",
            LibraryCategory.OsImage => "\uE7C5",
            _ => "\uE8B7"
        };

        /// <summary>
        /// Gets or sets a value indicating whether this duplicate copy is checked for recycling.
        /// </summary>
        [ObservableProperty]
        public partial bool IsMarkedForDeletion { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateItemEntryViewModel"/> class.
        /// </summary>
        public DuplicateItemEntryViewModel(DuplicateItemEntry entry, Action onSelectionChanged)
        {
            ArgumentNullException.ThrowIfNull(entry);
            _onSelectionChanged = onSelectionChanged ?? throw new ArgumentNullException(nameof(onSelectionChanged));

            Name = entry.Name;
            FolderPath = entry.FolderPath;
            Category = entry.Category;
            TotalBytes = entry.TotalBytes;
            DateCreated = entry.DateCreated;
        }

        partial void OnIsMarkedForDeletionChanged(bool value)
        {
            _onSelectionChanged();
        }
    }

    /// <summary>
    /// Represents an observable grouping of duplicate item entries sharing the same name.
    /// </summary>
    public sealed partial class DuplicateGroupViewModel : ObservableObject
    {
        /// <summary>
        /// Gets the title shared by all items in this duplicate group.
        /// </summary>
        public string ItemName { get; }

        /// <summary>
        /// Gets the observable collection of copies found across source directories.
        /// </summary>
        public ObservableCollection<DuplicateItemEntryViewModel> Entries { get; }

        /// <summary>
        /// Gets the total byte size across all copies in this group.
        /// </summary>
        public long TotalBytes => Entries.Sum(e => e.TotalBytes);

        /// <summary>
        /// Gets the formatted size string for all copies combined.
        /// </summary>
        public string FormattedTotalBytes => FormattingHelpers.FormatBytes(TotalBytes);

        /// <summary>
        /// Gets the number of detected copies.
        /// </summary>
        public int CopiesCount => Entries.Count;

        /// <summary>
        /// Gets the formatted text displaying the number of copies.
        /// </summary>
        public string CopiesCountText => $"{CopiesCount} copies";

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateGroupViewModel"/> class.
        /// </summary>
        public DuplicateGroupViewModel(string itemName, IEnumerable<DuplicateItemEntryViewModel> entries)
        {
            ItemName = itemName;
            Entries = new ObservableCollection<DuplicateItemEntryViewModel>(entries);
        }

        /// <summary>
        /// Marks all copies in this group for deletion except the newest created one.
        /// </summary>
        public void SelectOlderCopies()
        {
            // Intent: Preserve the newest file version while marking historical or secondary duplicates for cleanup.
            DuplicateItemEntryViewModel? newest = Entries.OrderByDescending(e => e.DateCreated).FirstOrDefault();
            foreach (DuplicateItemEntryViewModel entry in Entries)
            {
                entry.IsMarkedForDeletion = entry != newest;
            }
        }

        /// <summary>
        /// Marks all copies in this group for deletion except the oldest created one.
        /// </summary>
        public void SelectNewerCopies()
        {
            // Intent: Preserve the original or canonical installation while marking newer duplicates.
            DuplicateItemEntryViewModel? oldest = Entries.OrderBy(e => e.DateCreated).FirstOrDefault();
            foreach (DuplicateItemEntryViewModel entry in Entries)
            {
                entry.IsMarkedForDeletion = entry != oldest;
            }
        }

        /// <summary>
        /// Unmarks all copies in this group.
        /// </summary>
        public void DeselectAll()
        {
            foreach (DuplicateItemEntryViewModel entry in Entries)
            {
                entry.IsMarkedForDeletion = false;
            }
        }
    }

    /// <summary>
    /// ViewModel driving the interactive duplicate item cleanup dialog.
    /// </summary>
    public sealed partial class DuplicateManagerViewModel : CloseableViewModelBase, IDisposable
    {
        /// <summary>
        /// The library scanner service used to scan configured library paths for duplicate items.
        /// </summary>
        private readonly ILibraryScannerService _libraryScannerService;

        /// <summary>
        /// The safe deletion service used to safely recycle selected duplicate items into the Windows Recycle Bin.
        /// </summary>
        private readonly ISafeDeletionService _safeDeletionService;

        /// <summary>
        /// The settings service used to access configured source folders and user preferences.
        /// </summary>
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// The dialog service used to display confirmation prompts and error alerts to the user.
        /// </summary>
        private readonly IDialogService _dialogService;

        /// <summary>
        /// The dispatcher service for marshaling operations and UI updates to the main thread.
        /// </summary>
        private readonly IDispatcherService _dispatcherService;

        /// <summary>
        /// The logger instance for logging duplicate scanning and deletion events.
        /// </summary>
        private readonly ILogger<DuplicateManagerViewModel> _logger;

        /// <summary>
        /// The cancellation token source controlling ongoing duplicate scan operations.
        /// </summary>
        private CancellationTokenSource? _scanCts;

        /// <summary>
        /// Gets the collection of detected duplicate groups.
        /// </summary>
        public ObservableCollection<DuplicateGroupViewModel> DuplicateGroups { get; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether duplicate scanning is actively running.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoDuplicates))]
        [NotifyPropertyChangedFor(nameof(CanDelete))]
        public partial bool IsScanning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Recycle Bin deletion is currently processing.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDelete))]
        public partial bool IsDeleting { get; set; }

        /// <summary>
        /// Gets or sets the current deletion progress percentage (0.0 to 100.0).
        /// </summary>
        [ObservableProperty]
        public partial double DeletionProgress { get; set; }

        /// <summary>
        /// Gets or sets the informational status text displayed to the user.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = "Ready to scan for duplicates.";

        /// <summary>
        /// Gets or sets the total number of duplicate items currently marked for deletion.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelection))]
        [NotifyPropertyChangedFor(nameof(CanDelete))]
        public partial int MarkedForDeletionCount { get; set; }

        /// <summary>
        /// Gets or sets the total recoverable disk space in bytes for currently marked items.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedRecoverableBytes))]
        public partial long TotalRecoverableBytes { get; set; }

        /// <summary>
        /// Gets a formatted string of total recoverable space (e.g. "128.4 GB").
        /// </summary>
        public string FormattedRecoverableBytes => FormattingHelpers.FormatBytes(TotalRecoverableBytes);

        /// <summary>
        /// Gets a value indicating whether any item is currently selected for deletion.
        /// </summary>
        public bool HasSelection => MarkedForDeletionCount > 0;

        /// <summary>
        /// Gets a value indicating whether deletion can be initiated.
        /// </summary>
        public bool CanDelete => !IsDeleting && !IsScanning && HasSelection;

        /// <summary>
        /// Gets a value indicating whether duplicates exist in the current collection.
        /// </summary>
        public bool HasDuplicates => DuplicateGroups.Count > 0;

        /// <summary>
        /// Gets a value indicating whether no duplicates were found after scanning completed.
        /// </summary>
        public bool HasNoDuplicates => !IsScanning && DuplicateGroups.Count == 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateManagerViewModel"/> class.
        /// </summary>
        public DuplicateManagerViewModel(
            ILibraryScannerService libraryScannerService,
            ISafeDeletionService safeDeletionService,
            ISettingsService settingsService,
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            ILogger<DuplicateManagerViewModel> logger)
        {
            _libraryScannerService = libraryScannerService ?? throw new ArgumentNullException(nameof(libraryScannerService));
            _safeDeletionService = safeDeletionService ?? throw new ArgumentNullException(nameof(safeDeletionService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Asynchronously initiates a scan across all configured source library directories to locate duplicates.
        /// </summary>
        [RelayCommand]
        public async Task ScanDuplicatesAsync()
        {
            if (IsScanning || IsDeleting)
            {
                return;
            }

            if (_scanCts != null)
            {
                await _scanCts.CancelAsync().ConfigureAwait(false);
                _scanCts.Dispose();
            }
            _scanCts = new CancellationTokenSource();
            CancellationToken token = _scanCts.Token;

            IsScanning = true;
            StatusMessage = "Scanning library folders for duplicate entries...";
            DuplicateGroups.Clear();
            RecalculateSelectionMetrics();

            try
            {
                AppSettings settings = _settingsService.LoadSettingsSync();

                Progress<string> progress = new(msg =>
                {
                    _ = _dispatcherService.TryEnqueue(() => StatusMessage = msg);
                });

                IReadOnlyList<DuplicateGroup> duplicateGroups = await _libraryScannerService
                    .GetDuplicateGroupsAsync(settings, progress, token)
                    .ConfigureAwait(false);

                _ = _dispatcherService.TryEnqueue(() =>
                {
                    foreach (DuplicateGroup group in duplicateGroups)
                    {
                        List<DuplicateItemEntryViewModel> entries = group.Entries
                            .Select(e => new DuplicateItemEntryViewModel(e, RecalculateSelectionMetrics))
                            .ToList();

                        DuplicateGroups.Add(new DuplicateGroupViewModel(group.ItemName, entries));
                    }

                    OnPropertyChanged(nameof(HasDuplicates));
                    OnPropertyChanged(nameof(HasNoDuplicates));

                    StatusMessage = DuplicateGroups.Count > 0
                        ? $"Found {DuplicateGroups.Count} duplicate items ({DuplicateGroups.Sum(g => g.CopiesCount)} total copies)."
                        : "No duplicate files or folders found across monitored libraries.";

                    RecalculateSelectionMetrics();
                });
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Duplicate scan canceled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan for duplicate library entries.");
                StatusMessage = "An error occurred during duplicate scanning.";
            }
            finally
            {
                _ = _dispatcherService.TryEnqueue(() =>
                {
                    IsScanning = false;
                    OnPropertyChanged(nameof(HasNoDuplicates));
                });
            }
        }

        /// <summary>
        /// Selects all duplicate copies across all groups except the newest copy in each group.
        /// </summary>
        [RelayCommand]
        public void SelectOlderCopies()
        {
            foreach (DuplicateGroupViewModel group in DuplicateGroups)
            {
                group.SelectOlderCopies();
            }
            RecalculateSelectionMetrics();
        }

        /// <summary>
        /// Selects all duplicate copies across all groups except the oldest copy in each group.
        /// </summary>
        [RelayCommand]
        public void SelectNewerCopies()
        {
            foreach (DuplicateGroupViewModel group in DuplicateGroups)
            {
                group.SelectNewerCopies();
            }
            RecalculateSelectionMetrics();
        }

        /// <summary>
        /// Deselects all copies across all duplicate groups.
        /// </summary>
        [RelayCommand]
        public void DeselectAll()
        {
            foreach (DuplicateGroupViewModel group in DuplicateGroups)
            {
                group.DeselectAll();
            }
            RecalculateSelectionMetrics();
        }

        /// <summary>
        /// Asynchronously prompts for confirmation and safely deletes marked duplicate items to the Windows Recycle Bin.
        /// </summary>
        [RelayCommand]
        public async Task DeleteSelectedAsync()
        {
            if (!CanDelete)
            {
                return;
            }

            List<DuplicateItemEntryViewModel> itemsToDelete = DuplicateGroups
                .SelectMany(g => g.Entries)
                .Where(e => e.IsMarkedForDeletion)
                .ToList();

            if (itemsToDelete.Count == 0)
            {
                return;
            }

            string formattedBytes = FormattingHelpers.FormatBytes(itemsToDelete.Sum(i => i.TotalBytes));
            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Are you sure you want to move {0} duplicate item(s) ({1}) to the Windows Recycle Bin?\n\nYou can restore them from the Recycle Bin if needed.",
                itemsToDelete.Count,
                formattedBytes);

            bool confirmed = await _dialogService.ShowConfirmationDialogAsync(
                "Confirm Recycle Bin Cleanup",
                message,
                primaryButtonText: "Recycle Items",
                closeButtonText: "Cancel");

            if (!confirmed)
            {
                return;
            }

            IsDeleting = true;
            DeletionProgress = 0;
            StatusMessage = "Moving selected duplicates to the Windows Recycle Bin...";

            try
            {
                List<(string Path, long Bytes)> targets = itemsToDelete
                    .Select(e => (e.FolderPath, e.TotalBytes))
                    .ToList();

                Progress<double> progress = new(p =>
                {
                    _ = _dispatcherService.TryEnqueue(() => DeletionProgress = p);
                });

                SafeDeletionResult result = await _safeDeletionService
                    .BatchDeleteToRecycleBinAsync(targets, progress)
                    .ConfigureAwait(false);

                _ = _dispatcherService.TryEnqueue(() =>
                {
                    // Clean up processed items from collections
                    List<DuplicateGroupViewModel> groupsToRemove = [];

                    foreach (DuplicateGroupViewModel group in DuplicateGroups)
                    {
                        List<DuplicateItemEntryViewModel> entriesToRemove = group.Entries
                            .Where(e => e.IsMarkedForDeletion && targets.Any(t => string.Equals(t.Path, e.FolderPath, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

                        foreach (DuplicateItemEntryViewModel entry in entriesToRemove)
                        {
                            _ = group.Entries.Remove(entry);
                        }

                        // If group now only has 1 or 0 copies, it is no longer a duplicate group
                        if (group.Entries.Count <= 1)
                        {
                            groupsToRemove.Add(group);
                        }
                    }

                    foreach (DuplicateGroupViewModel emptyGroup in groupsToRemove)
                    {
                        _ = DuplicateGroups.Remove(emptyGroup);
                    }

                    OnPropertyChanged(nameof(HasDuplicates));
                    OnPropertyChanged(nameof(HasNoDuplicates));
                    RecalculateSelectionMetrics();

                    StatusMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "Recycled {0} item(s) successfully ({1} recovered).",
                        result.SucceededCount,
                        FormattingHelpers.FormatBytes(result.RecoveredBytes));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete batch Recycle Bin deletion.");
                StatusMessage = "An error occurred during cleanup.";
            }
            finally
            {
                _ = _dispatcherService.TryEnqueue(() =>
                {
                    IsDeleting = false;
                    DeletionProgress = 0;
                });
            }
        }

        /// <summary>
        /// Recalculates total marked items count and recoverable disk space.
        /// </summary>
        private void RecalculateSelectionMetrics()
        {
            List<DuplicateItemEntryViewModel> marked = DuplicateGroups
                .SelectMany(g => g.Entries)
                .Where(e => e.IsMarkedForDeletion)
                .ToList();

            MarkedForDeletionCount = marked.Count;
            TotalRecoverableBytes = marked.Sum(e => e.TotalBytes);

            OnPropertyChanged(nameof(FormattedRecoverableBytes));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanDelete));
        }

        /// <summary>
        /// Releases all managed resources used by the <see cref="DuplicateManagerViewModel"/> instance.
        /// </summary>
        public void Dispose()
        {
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }
}
