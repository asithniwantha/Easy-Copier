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
using Microsoft.Extensions.Logging;

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

        /// <summary>Service for grouping history records and computing aggregate history statistics.</summary>
        private readonly IHistoryAnalysisService _historyAnalysisService;

        /// <summary>Logger instance for recording history operations and diagnostic warnings.</summary>
        private readonly Microsoft.Extensions.Logging.ILogger<HistoryViewModel>? _logger;

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
        public partial ObservableCollection<CopyHistoryRecord> Records { get; set; } = new BulkObservableCollection<CopyHistoryRecord>();

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

        /// <summary>Gets or sets the header subtitle summarizing results and filters.</summary>
        [ObservableProperty]
        public partial string HeaderSubtitle { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether the filtered list is empty.</summary>
        [ObservableProperty]
        public partial bool HasNoRecords { get; set; }

        /// <summary>
        /// Gets the available date filter options for UI selection.
        /// </summary>
        public IReadOnlyList<DateFilter> AvailableDateFilters { get; } = Enum.GetValues<DateFilter>();

        /// <summary>
        /// Gets the available status filter options for UI selection.
        /// </summary>
        public IReadOnlyList<StatusFilter> AvailableStatusFilters { get; } = Enum.GetValues<StatusFilter>();

        /// <summary>
        /// Gets or sets the currently selected date filter option.
        /// </summary>
        [ObservableProperty]
        public partial DateFilter SelectedDateFilter { get; set; } = DateFilter.Today;

        /// <summary>
        /// Gets or sets the currently selected status filter option.
        /// </summary>
        [ObservableProperty]
        public partial StatusFilter SelectedStatusFilter { get; set; } = StatusFilter.All;

        /// <summary>
        /// Gets or sets the collection of available date filter option strings for UI binding compatibility.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<string> AvailableDateFilterStrings { get; set; }

        /// <summary>
        /// Gets or sets the collection of available status filter option strings for UI binding compatibility.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<string> AvailableStatusFilterStrings { get; set; }

        /// <summary>
        /// Gets or sets the currently selected date filter option string for UI dropdown binding.
        /// </summary>
        [ObservableProperty]
        public partial string SelectedDateFilterString { get; set; }

        /// <summary>
        /// Gets or sets the currently selected status filter option string for UI dropdown binding.
        /// </summary>
        [ObservableProperty]
        public partial string SelectedStatusFilterString { get; set; }

        /// <summary>Gets or sets the current 1-based page number.</summary>
        [ObservableProperty]
        public partial int CurrentPage { get; set; } = 1;

        /// <summary>Gets or sets the total number of pages available.</summary>
        [ObservableProperty]
        public partial int TotalPages { get; set; } = 1;

        /// <summary>Gets or sets the total count of matching records across all pages.</summary>
        [ObservableProperty]
        public partial int TotalRecordCount { get; set; }

        /// <summary>Gets or sets the maximum number of records displayed per page.</summary>
        [ObservableProperty]
        public partial int PageSize { get; set; } = 50;

        /// <summary>Gets or sets a value indicating whether a subsequent page is available.</summary>
        [ObservableProperty]
        public partial bool HasNextPage { get; set; }

        /// <summary>Gets or sets a value indicating whether a preceding page is available.</summary>
        [ObservableProperty]
        public partial bool HasPreviousPage { get; set; }

        /// <summary>Gets or sets the formatted text summarizing current page and total pages.</summary>
        [ObservableProperty]
        public partial string PaginationSummaryText { get; set; } = "Page 1 of 1";

        /// <summary>Gets or sets the formatted text summarizing total matching records found.</summary>
        [ObservableProperty]
        public partial string TotalRecordSummaryText { get; set; } = "0 transfers found";

        /// <summary>Gets the list of available page size options.</summary>
        public IReadOnlyList<int> AvailablePageSizes { get; } = [25, 50, 100, 200];

        /// <summary>Gets or sets the selected page size from UI dropdown.</summary>
        [ObservableProperty]
        public partial int SelectedPageSize { get; set; } = 50;

        /// <summary>Notification handler invoked when selected page size changes.</summary>
        partial void OnSelectedPageSizeChanged(int value)
        {
            if (value > 0 && PageSize != value)
            {
                PageSize = value;
                CurrentPage = 1;
                _ = LoadPageDataAsync();
            }
        }

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
        /// Display string for filtering history records to today.
        /// </summary>
        public const string FilterToday = "Today";

        /// <summary>
        /// Display string for filtering history records to the last 7 days.
        /// </summary>
        public const string FilterLast7Days = "Last 7 Days";

        /// <summary>
        /// Display string for filtering history records to the current month.
        /// </summary>
        public const string FilterThisMonth = "This Month";

        /// <summary>
        /// Display string for filtering history records across all time.
        /// </summary>
        public const string FilterAllTime = "All Time";

        /// <summary>
        /// Display string for showing all status outcomes.
        /// </summary>
        public const string StatusAll = "All";

        /// <summary>
        /// Display string for filtering history records to completed operations.
        /// </summary>
        public const string StatusCompleted = "Completed";

        /// <summary>
        /// Display string for filtering history records to failed operations.
        /// </summary>
        public const string StatusFailed = "Failed";

        /// <summary>
        /// The default set of available date filter display strings.
        /// </summary>
        private static readonly string[] DefaultDateFilterStrings = [FilterToday, FilterLast7Days, FilterThisMonth, FilterAllTime];

        /// <summary>
        /// The default set of available status filter display strings.
        /// </summary>
        private static readonly string[] DefaultStatusFilterStrings = [StatusAll, StatusCompleted, StatusFailed];

        /// <summary>
        /// Gets the available dashboard range options.
        /// </summary>
        public IReadOnlyList<string> DashboardRangeOptions { get; } = ["Last 7 Days", "Last 30 Days", "Last 90 Days"];

        /// <summary>
        /// Gets or sets the selected dashboard range option.
        /// </summary>
        [ObservableProperty]
        public partial string SelectedDashboardRange { get; set; } = "Last 7 Days";

        /// <summary>
        /// Gets or sets the dashboard reporting data for the selected range.
        /// </summary>
        [ObservableProperty]
        public partial DashboardStats? Dashboard { get; set; }

        /// <summary>
        /// Gets or sets the daily totals shown in the dashboard chart.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<PeriodTotal> DailyTotals { get; set; } = [];

        /// <summary>
        /// Gets or sets the weekly totals shown in the dashboard.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<PeriodTotal> WeeklyTotals { get; set; } = [];

        /// <summary>
        /// Gets or sets the most-copied items shown in the dashboard.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<TopCopiedItem> TopItems { get; set; } = [];

        /// <summary>
        /// Gets or sets the overall failure rate text for the dashboard range.
        /// </summary>
        [ObservableProperty]
        public partial string FailureRateText { get; set; } = "0%";

        /// <summary>
        /// Gets or sets a value indicating whether the dashboard panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsDashboardVisible { get; set; }

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedDashboardRange"/> changes.
        /// </summary>
        partial void OnSelectedDashboardRangeChanged(string value) => _ = RefreshDashboardAsync();

        /// <summary>
        /// Toggles dashboard visibility.
        /// </summary>
        [RelayCommand]
        private void ToggleDashboard() => IsDashboardVisible = !IsDashboardVisible;

        /// <summary>
        /// Asynchronously recomputes dashboard analytics from recent historical data.
        /// </summary>
        private async Task RefreshDashboardAsync()
        {
            if (!_isInitialized)
            {
                return;
            }

            int days = SelectedDashboardRange switch
            {
                "Last 30 Days" => 30,
                "Last 90 Days" => 90,
                _ => 7
            };

            DateTime minDate = DateTime.Today.AddDays(-days);
            DateTime maxDate = DateTime.Today.AddDays(1);
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByWeekAsync(minDate, maxDate);

            DashboardStats stats = _historyAnalysisService.BuildDashboard(records, days);
            Dashboard = stats;
            DailyTotals.UpdateFrom(stats.DailyTotals);
            WeeklyTotals.UpdateFrom(stats.WeeklyTotals);
            TopItems.UpdateFrom(stats.TopItems);
            FailureRateText = $"{Math.Round(stats.OverallFailureRate, 1)}%";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryViewModel"/> class.
        /// </summary>
        /// <param name="copyHistoryService">The copy history service used to query past transfer records.</param>
        /// <param name="reportService">The report service used to export records.</param>
        /// <param name="filePickerService">The file picker service used to select export file destinations.</param>
        /// <param name="historyAnalysisService">The history analysis service used to group records and compute summary statistics.</param>
        public HistoryViewModel(
            ICopyHistoryService copyHistoryService,
            IReportService reportService,
            IFilePickerService filePickerService,
            IHistoryAnalysisService historyAnalysisService,
            Microsoft.Extensions.Logging.ILogger<HistoryViewModel>? logger = null)
        {
            _copyHistoryService = copyHistoryService;
            _reportService = reportService;
            _filePickerService = filePickerService;
            _historyAnalysisService = historyAnalysisService;
            _logger = logger;

            AvailableDateFilterStrings = [.. DefaultDateFilterStrings];
            AvailableStatusFilterStrings = [.. DefaultStatusFilterStrings];
            SelectedDateFilterString = FilterToday;
            SelectedStatusFilterString = StatusAll;
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

            _isInitialized = true;
            await LoadPageDataAsync();
            await RefreshDashboardAsync();
        }

        /// <summary>
        /// Partial notification handler invoked when <see cref="SearchQuery"/> changes.
        /// </summary>
        /// <param name="value">The new search query value.</param>
        partial void OnSearchQueryChanged(string value) => ApplyFilters();

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedDateFilter"/> changes.
        /// </summary>
        partial void OnSelectedDateFilterChanged(DateFilter value) => ApplyFilters();

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedStatusFilter"/> changes.
        /// </summary>
        partial void OnSelectedStatusFilterChanged(StatusFilter value) => ApplyFilters();

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedDateFilterString"/> changes.
        /// </summary>
        /// <param name="value">The new date filter string value.</param>
        partial void OnSelectedDateFilterStringChanged(string value)
        {
            // Setting SelectedDateFilter automatically triggers OnSelectedDateFilterChanged, which invokes ApplyFilters().
            SelectedDateFilter = value switch
            {
                FilterToday => DateFilter.Today,
                FilterLast7Days => DateFilter.Last7Days,
                FilterThisMonth => DateFilter.ThisMonth,
                _ => DateFilter.Custom
            };
        }

        /// <summary>
        /// Partial notification handler invoked when <see cref="SelectedStatusFilterString"/> changes.
        /// </summary>
        /// <param name="value">The new status filter string value.</param>
        partial void OnSelectedStatusFilterStringChanged(string value)
        {
            // Setting SelectedStatusFilter automatically triggers OnSelectedStatusFilterChanged, which invokes ApplyFilters().
            SelectedStatusFilter = value switch
            {
                StatusCompleted => StatusFilter.Completed,
                StatusFailed => StatusFilter.Failed,
                _ => StatusFilter.All
            };
        }

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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to deserialize sub-files JSON for history record ID {RecordId}", value.Id);
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
        /// Resets page to 1 and asynchronously loads the paged slice from SQLite storage.
        /// </summary>
        private void ApplyFilters()
        {
            if (!_isInitialized)
            {
                return;
            }

            CurrentPage = 1;
            _ = LoadPageDataAsync();
        }

        /// <summary>
        /// Asynchronously queries SQLite for the active page records and summary metrics.
        /// </summary>
        private async Task LoadPageDataAsync()
        {
            if (!_isInitialized)
            {
                return;
            }

            StatusMessage = "Loading page...";

            DateTime now = DateTime.Now;
            DateTime? startDate = SelectedDateFilter switch
            {
                DateFilter.Today => now.Date,
                DateFilter.Last7Days => now.AddDays(-7).Date,
                DateFilter.ThisMonth => new DateTime(now.Year, now.Month, 1),
                _ => null
            };

            DateTime? endDate = SelectedDateFilter switch
            {
                DateFilter.Today => now.Date.AddDays(1),
                DateFilter.Last7Days => now.Date.AddDays(1),
                DateFilter.ThisMonth => new DateTime(now.Year, now.Month, 1).AddMonths(1),
                _ => null
            };

            bool? isSuccess = SelectedStatusFilter switch
            {
                StatusFilter.Completed => true,
                StatusFilter.Failed => false,
                _ => null
            };

            string? search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();

            Task<PagedResult<CopyHistoryRecord>> pageTask = _copyHistoryService.GetPagedRecordsAsync(
                startDate, endDate, isSuccess, search, CurrentPage, PageSize);
            Task<(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount)> statsTask = _copyHistoryService.GetFilteredStatsAsync(
                startDate, endDate, isSuccess, search);

            await Task.WhenAll(pageTask, statsTask);

            PagedResult<CopyHistoryRecord> pagedResult = await pageTask;
            var (totalItems, successfulItems, totalBytes, totalAmount) = await statsTask;

            TotalRecordCount = pagedResult.TotalCount;
            TotalPages = Math.Max(1, pagedResult.TotalPages);
            HasNextPage = pagedResult.HasNextPage;
            HasPreviousPage = pagedResult.HasPreviousPage;

            PaginationSummaryText = $"Page {CurrentPage} of {TotalPages}";
            TotalRecordSummaryText = $"{TotalRecordCount:N0} transfer(s) found";

            List<CopyHistoryRecord> pageItems = [.. pagedResult.Items];
            _historyAnalysisService.ApplyBatchClustering(pageItems);
            Records.UpdateFrom(pageItems);

            string successRate = totalItems > 0 ? $"{Math.Round((double)successfulItems / totalItems * 100, 1)}%" : "0%";
            SelectedFilterStats = new HistoryStats(totalItems, successfulItems, totalBytes, totalAmount, successRate);

            HasNoRecords = TotalRecordCount == 0;
            HeaderSubtitle = $"{TotalRecordCount:N0} transfer(s) • {SelectedDateFilterString} • {SelectedStatusFilterString}";
            StatusMessage = $"Showing page {CurrentPage} of {TotalPages} ({Records.Count} records).";
        }

        /// <summary>Advances to the next page of history records.</summary>
        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (HasNextPage)
            {
                CurrentPage++;
                await LoadPageDataAsync();
            }
        }

        /// <summary>Navigates back to the previous page of history records.</summary>
        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (HasPreviousPage)
            {
                CurrentPage--;
                await LoadPageDataAsync();
            }
        }

        /// <summary>Navigates directly to the first page of history records.</summary>
        [RelayCommand]
        private async Task FirstPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
                await LoadPageDataAsync();
            }
        }

        /// <summary>Navigates directly to the last page of history records.</summary>
        [RelayCommand]
        private async Task LastPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = TotalPages;
                await LoadPageDataAsync();
            }
        }

        /// <summary>Resets the date, status and search filters to defaults.</summary>
        [RelayCommand]
        private void ClearFilters()
        {
            SearchQuery = string.Empty;
            SelectedStatusFilterString = StatusAll;
            SelectedDateFilterString = FilterAllTime;
        }

        /// <summary>
        /// Asynchronously prompts the user for a destination file path and exports the active records list to CSV.
        /// </summary>
        /// <returns>A task representing the asynchronous export operation.</returns>
        [RelayCommand]
        private async Task ExportToCsvAsync()
        {
            if (TotalRecordCount == 0)
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
                StatusMessage = "Exporting records...";

                DateTime now = DateTime.Now;
                DateTime? startDate = SelectedDateFilter switch
                {
                    DateFilter.Today => now.Date,
                    DateFilter.Last7Days => now.AddDays(-7).Date,
                    DateFilter.ThisMonth => new DateTime(now.Year, now.Month, 1),
                    _ => null
                };

                DateTime? endDate = SelectedDateFilter switch
                {
                    DateFilter.Today => now.Date.AddDays(1),
                    DateFilter.Last7Days => now.Date.AddDays(1),
                    DateFilter.ThisMonth => new DateTime(now.Year, now.Month, 1).AddMonths(1),
                    _ => null
                };

                bool? isSuccess = SelectedStatusFilter switch
                {
                    StatusFilter.Completed => true,
                    StatusFilter.Failed => false,
                    _ => null
                };

                string? search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();

                List<CopyHistoryRecord> allFiltered = await _copyHistoryService.GetAllFilteredRecordsAsync(startDate, endDate, isSuccess, search);
                bool success = await _reportService.ExportHistoryToCsvAsync(filePath, allFiltered);
                StatusMessage = success ? $"Exported {allFiltered.Count} record(s) to {System.IO.Path.GetFileName(filePath)}" : "Export failed. Check logs.";
            }
        }
    }
}
