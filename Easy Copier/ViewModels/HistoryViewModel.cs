using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Specifies predefined date filtering options for file copy history records.
    /// </summary>
    public enum DateFilter
    {
        /// <summary>Filter records logged today.</summary>
        Today,

        /// <summary>Filter records logged within the last 7 days.</summary>
        Last7Days,

        /// <summary>Filter records logged within the current calendar month.</summary>
        ThisMonth,

        /// <summary>Custom date range filter option.</summary>
        Custom
    }

    /// <summary>
    /// Specifies outcome status filtering options for file copy history records.
    /// </summary>
    public enum StatusFilter
    {
        /// <summary>Include all records regardless of completion status.</summary>
        All,

        /// <summary>Filter records that completed successfully.</summary>
        Completed,

        /// <summary>Filter records that failed or were canceled.</summary>
        Failed
    }

    /// <summary>
    /// ViewModel for managing file transfer history records, filtering options, aggregated statistics, and CSV exports.
    /// </summary>
    public partial class HistoryViewModel : ObservableObject
    {
        /// <summary>Service for querying copy operation history records from storage.</summary>
        private readonly ICopyHistoryService _copyHistoryService;

        /// <summary>Service for exporting operation records to external formats such as CSV.</summary>
        private readonly IReportService _reportService;

        /// <summary>Service for displaying save file picker dialogs.</summary>
        private readonly IFilePickerService _filePickerService;

        /// <summary>Service for analyzing and clustering history records.</summary>
        private readonly IHistoryAnalysisService _historyAnalysisService;

        /// <summary>Internal list holding all un-filtered copy history records loaded from storage.</summary>
        private List<CopyHistoryRecord> _allRecords = [];

        /// <summary>Flag indicating whether initial record loading has been executed.</summary>
        private bool _isInitialized;

        /// <summary>
        /// Gets or sets the aggregated summary statistics for the currently filtered history records.
        /// </summary>
        [ObservableProperty]
        public partial HistoryStats SelectedFilterStats { get; set; } = new HistoryStats(0, 0, 0, 0, "0%");

        /// <summary>
        /// Gets or sets the collection of copy history records matching the active search and filter criteria.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<CopyHistoryRecord> Records { get; set; } = [];

        /// <summary>
        /// Gets or sets the currently selected copy history record for detail inspection in the side drawer.
        /// </summary>
        [ObservableProperty]
        public partial CopyHistoryRecord? SelectedRecord { get; set; }

        /// <summary>
        /// Gets or sets the status message string presented in the history interface.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the search query string used to filter history entries by item or drive name.
        /// </summary>
        [ObservableProperty]
        public partial string SearchQuery { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of available date filter options for UI selection.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<string> AvailableDateFilters { get; set; }

        /// <summary>
        /// Gets or sets the collection of available status filter options for UI selection.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<string> AvailableStatusFilters { get; set; }

        /// <summary>
        /// Gets or sets the currently selected date filter option string.
        /// </summary>
        [ObservableProperty]
        public partial string SelectedDateFilterString { get; set; }

        /// <summary>
        /// Gets or sets the currently selected status filter option string.
        /// </summary>
        [ObservableProperty]
        public partial string SelectedStatusFilterString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the detail inspection drawer panel is open.
        /// </summary>
        [ObservableProperty]
        public partial bool IsDrawerOpen { get; set; }

        /// <summary>
        /// Gets or sets the list of sub-file paths associated with the <see cref="SelectedRecord"/>.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<string> SelectedRecordSubFiles { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryViewModel"/> class.
        /// </summary>
        /// <param name="copyHistoryService">The copy history service used to query past transfer records.</param>
        /// <param name="reportService">The report service used to export records.</param>
        /// <param name="filePickerService">The file picker service used to select export file destinations.</param>
        /// <param name="historyAnalysisService">The history analysis service used to cluster records and calculate stats.</param>
        public HistoryViewModel(
            ICopyHistoryService copyHistoryService,
            IReportService reportService,
            IFilePickerService filePickerService,
            IHistoryAnalysisService historyAnalysisService)
        {
            _copyHistoryService = copyHistoryService;
            _reportService = reportService;
            _filePickerService = filePickerService;
            _historyAnalysisService = historyAnalysisService;

            // Initialize default filter options
            AvailableDateFilters = new ObservableCollection<string> { "Today", "Last 7 Days", "This Month", "All Time" };
            AvailableStatusFilters = new ObservableCollection<string> { "All", "Completed", "Failed" };
            SelectedDateFilterString = "Last 7 Days";
            SelectedStatusFilterString = "All";
        }

        /// <summary>
        /// Asynchronously loads history records from storage and applies active filters.
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            StatusMessage = "Loading records...";
            DateTime minDate = new(2000, 1, 1);
            DateTime maxDate = new(2100, 1, 1);
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByWeekAsync(minDate, maxDate);
            _allRecords = records;
            _isInitialized = true;
            ApplyFilters();
        }

        /// <summary>
        /// Partial notification handler invoked when <see cref="SearchQuery"/> changes.
        /// </summary>
        /// <param name="value">The new search query value.</param>
        partial void OnSearchQueryChanged(string value) => ApplyFilters();

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedDateFilterString"/> changes.
        /// </summary>
        /// <param name="value">The new date filter string value.</param>
        partial void OnSelectedDateFilterStringChanged(string value) => ApplyFilters();

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedStatusFilterString"/> changes.
        /// </summary>
        /// <param name="value">The new status filter string value.</param>
        partial void OnSelectedStatusFilterStringChanged(string value) => ApplyFilters();

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedRecord"/> changes to toggle drawer visibility and populate sub-files.
        /// </summary>
        /// <param name="value">The newly selected <see cref="CopyHistoryRecord"/> instance, or <c>null</c>.</param>
        partial void OnSelectedRecordChanged(CopyHistoryRecord? value)
        {
            if (value != null)
            {
                IsDrawerOpen = true;
                SelectedRecordSubFiles.Clear();
                try
                {
                    if (!string.IsNullOrWhiteSpace(value.SubFilesJson) && value.SubFilesJson != "[]")
                    {
                        List<string>? files = JsonSerializer.Deserialize<List<string>>(value.SubFilesJson);
                        if (files != null)
                        {
                            SelectedRecordSubFiles.UpdateFrom(files);
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }
            else
            {
                IsDrawerOpen = false;
            }
        }

        /// <summary>
        /// Closes the detail inspection drawer by clearing the active selection.
        /// </summary>
        [RelayCommand]
        private void CloseDrawer()
        {
            SelectedRecord = null;
        }

        /// <summary>
        /// Filters and sorts in-memory records based on active date, status, and search query parameters.
        /// </summary>
        private void ApplyFilters()
        {
            if (!_isInitialized)
            {
                return;
            }

            IEnumerable<CopyHistoryRecord> filtered = _allRecords;

            // Apply Date Filter
            DateTime now = DateTime.Now;
            if (SelectedDateFilterString == "Today")
            {
                filtered = filtered.Where(r => r.Timestamp.Date == now.Date);
            }
            else if (SelectedDateFilterString == "Last 7 Days")
            {
                DateTime sevenDaysAgo = now.AddDays(-7).Date;
                filtered = filtered.Where(r => r.Timestamp.Date >= sevenDaysAgo);
            }
            else if (SelectedDateFilterString == "This Month")
            {
                filtered = filtered.Where(r => r.Timestamp.Year == now.Year && r.Timestamp.Month == now.Month);
            }

            // Apply Status Filter
            if (SelectedStatusFilterString == "Completed")
            {
                filtered = filtered.Where(r => r.IsSuccess);
            }
            else if (SelectedStatusFilterString == "Failed")
            {
                filtered = filtered.Where(r => !r.IsSuccess);
            }

            // Apply Search Query
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.Where(r =>
                    r.GameName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    r.TargetDriveLetter.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    r.TargetDriveLabel.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                );
            }

            List<CopyHistoryRecord> sortedRecords = filtered.OrderByDescending(r => r.Timestamp).ToList();

            _historyAnalysisService.ApplyBatchClustering(sortedRecords);
            Records.UpdateFrom(sortedRecords);

            SelectedFilterStats = _historyAnalysisService.CalculateStats(sortedRecords);
            StatusMessage = $"Showing {sortedRecords.Count} records.";
        }

        /// <summary>
        /// Asynchronously prompts the user for a destination file path and exports the active records list to CSV.
        /// </summary>
        /// <returns>A task representing the asynchronous export operation.</returns>
        [RelayCommand]
        private async Task ExportToCsvAsync()
        {
            if (Records.Count == 0)
            {
                StatusMessage = "No records to export.";
                return;
            }

            string fileName = $"EasyCopier_History_Export_{DateTime.Now:yyyy_MM_dd}.csv";
            Dictionary<string, IList<string>> choices = new()
            {
                { "CSV File", new List<string> { ".csv" } }
            };

            string? filePath = await _filePickerService.PickSaveFileAsync(fileName, choices);

            if (filePath != null)
            {
                StatusMessage = "Exporting...";
                bool success = await _reportService.ExportHistoryToCsvAsync(filePath, Records);
                StatusMessage = success ? $"Exported successfully to {System.IO.Path.GetFileName(filePath)}" : "Export failed. Check logs.";
            }
        }
    }

    /// <summary>
    /// Represents aggregated summary statistics calculated for a subset of copy history records.
    /// </summary>
    /// <param name="TotalItems">The total count of matching transfer records.</param>
    /// <param name="SuccessfulItems">The count of successfully completed transfer records.</param>
    /// <param name="TotalBytes">The cumulative data volume in bytes.</param>
    /// <param name="TotalAmount">The cumulative monetary amount charged.</param>
    /// <param name="SuccessRate">The calculated success rate percentage string.</param>
    public record HistoryStats(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount, string SuccessRate);
}
