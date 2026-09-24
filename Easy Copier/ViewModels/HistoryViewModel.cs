using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel managing historical copy operations, statistics summaries, time-window filtering, and report exports.
    /// </summary>
    /// <param name="copyHistoryService">The copy history data service.</param>
    /// <param name="reportService">The report export service.</param>
    /// <param name="filePickerService">The file picker dialog service.</param>
    public partial class HistoryViewModel(ICopyHistoryService copyHistoryService, IReportService reportService,
                            Infrastructure.IFilePickerService filePickerService) : ObservableObject
    {
        /// <summary>
        /// Gets or sets the transfer statistics for today.
        /// </summary>
        [ObservableProperty]
        public partial HistoryStats TodayStats { get; set; } = new HistoryStats(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the transfer statistics for the current week.
        /// </summary>
        [ObservableProperty]
        public partial HistoryStats WeekStats { get; set; } = new HistoryStats(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the transfer statistics for the current month.
        /// </summary>
        [ObservableProperty]
        public partial HistoryStats MonthStats { get; set; } = new HistoryStats(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the transfer statistics for the currently selected filter period.
        /// </summary>
        [ObservableProperty]
        public partial HistoryStats SelectedFilterStats { get; set; } = new HistoryStats(0, 0, 0, 0);

        /// <summary>
        /// Gets or sets the display title for the currently active filter range.
        /// </summary>
        [ObservableProperty]
        public partial string SelectedFilterName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of copy history records displayed in the UI.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<CopyHistoryRecord> Records { get; set; } = [];

        /// <summary>
        /// Gets or sets the collection of available week filter options.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<WeekOption> AvailableWeeks { get; set; } = [];

        /// <summary>
        /// Gets or sets the collection of available month filter options.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<MonthOption> AvailableMonths { get; set; } = [];

        /// <summary>
        /// Gets or sets the currently selected week filter option.
        /// </summary>
        [ObservableProperty]
        public partial WeekOption? SelectedWeek { get; set; }

        /// <summary>
        /// Gets or sets the currently selected month filter option.
        /// </summary>
        [ObservableProperty]
        public partial MonthOption? SelectedMonth { get; set; }

        /// <summary>
        /// Gets or sets the current status or progress message displayed to the user.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        private bool _isClearingSelection;

        /// <summary>
        /// Initializes history statistics and loads available filtering options asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            await LoadStatsAsync();

            List<DateTime> startOfWeeks = await copyHistoryService.GetAvailableWeeksAsync();
            AvailableWeeks.UpdateFrom(startOfWeeks.Select(w => new WeekOption(w, w.AddDays(6), $"{w:MMM dd, yyyy} - {w.AddDays(6):MMM dd, yyyy}")));

            List<(int Year, int Month)> months = await copyHistoryService.GetAvailableMonthsAsync();
            AvailableMonths.UpdateFrom(months.Select(m => new MonthOption(m.Year, m.Month, new DateTime(m.Year, m.Month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture))));

            if (AvailableWeeks.Count > 0)
            {
                SelectedWeek = AvailableWeeks.First();
            }
            else if (AvailableMonths.Count > 0)
            {
                SelectedMonth = AvailableMonths.First();
            }
        }

        private async Task LoadStatsAsync()
        {
            DateTime today = DateTime.Today;

            // Today
            DateTime todayStart = today;
            DateTime todayEnd = today.AddDays(1);
            (int todayTotal, int todaySuccess, long todayBytes, int todayAmount) = await copyHistoryService.GetStatsAsync(todayStart, todayEnd);
            TodayStats = new HistoryStats(todayTotal, todaySuccess, todayBytes, todayAmount);

            // Week (Starting Sunday)
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
            DateTime weekStart = today.AddDays(-1 * diff).Date;
            DateTime weekEnd = weekStart.AddDays(7);
            (int weekTotal, int weekSuccess, long weekBytes, int weekAmount) = await copyHistoryService.GetStatsAsync(weekStart, weekEnd);
            WeekStats = new HistoryStats(weekTotal, weekSuccess, weekBytes, weekAmount);

            // Month
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1);
            (int monthTotal, int monthSuccess, long monthBytes, int monthAmount) = await copyHistoryService.GetStatsAsync(monthStart, monthEnd);
            MonthStats = new HistoryStats(monthTotal, monthSuccess, monthBytes, monthAmount);
        }

        partial void OnSelectedWeekChanged(WeekOption? oldValue, WeekOption? newValue)
        {
            if (_isClearingSelection) return;

            if (newValue != null)
            {
                _isClearingSelection = true;
                SelectedMonth = null;
                _isClearingSelection = false;

                _ = LoadRecordsByWeekAsync(newValue.StartOfWeek, newValue.EndOfWeek);
            }
            else if (SelectedMonth == null)
            {
                Records.Clear();
            }
        }

        partial void OnSelectedMonthChanged(MonthOption? oldValue, MonthOption? newValue)
        {
            if (_isClearingSelection) return;

            if (newValue != null)
            {
                _isClearingSelection = true;
                SelectedWeek = null;
                _isClearingSelection = false;

                _ = LoadRecordsByMonthAsync(newValue.Year, newValue.Month);
            }
            else if (SelectedWeek == null)
            {
                Records.Clear();
            }
        }

        private async Task LoadRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek)
        {
            StatusMessage = "Loading records...";
            List<CopyHistoryRecord> records = await copyHistoryService.GetRecordsByWeekAsync(startOfWeek, endOfWeek);
            ProcessLoadedRecords(records);

            SelectedFilterName = $"Stats for {startOfWeek:MMM dd, yyyy} - {endOfWeek:MMM dd, yyyy}";
        }

        private async Task LoadRecordsByMonthAsync(int year, int month)
        {
            StatusMessage = "Loading records...";
            List<CopyHistoryRecord> records = await copyHistoryService.GetRecordsByMonthAsync(year, month);
            ProcessLoadedRecords(records);

            SelectedFilterName = $"Stats for {new DateTime(year, month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture)}";
        }

        private void ProcessLoadedRecords(List<CopyHistoryRecord> records)
        {
            // First sort descending so latest are on top
            List<CopyHistoryRecord> sortedRecords = [.. records.OrderByDescending(r => r.Timestamp)];

            // Group records by drive and approximate time to calculate batch amount
            List<List<CopyHistoryRecord>> clusters = [];
            foreach (CopyHistoryRecord record in sortedRecords)
            {
                List<CopyHistoryRecord>? cluster = clusters.FirstOrDefault(c =>
                    c.First().TargetDriveLetter == record.TargetDriveLetter &&
                    Math.Abs((c.First().Timestamp - record.Timestamp).TotalMinutes) < 15);

                if (cluster == null)
                {
                    cluster = [];
                    clusters.Add(cluster);
                }

                cluster.Add(record);
            }

            // Assign the computed sum back to each record
            foreach (List<CopyHistoryRecord> cluster in clusters)
            {
                int clusterTotal = cluster.Sum(r => r.Amount);
                foreach (CopyHistoryRecord record in cluster)
                {
                    record.BatchAmount = clusterTotal;
                }
            }

            Records.UpdateFrom(sortedRecords);

            // Calculate filtered stats based on loaded records
            int totalItems = records.Count;
            int successfulItems = records.Count(r => r.IsSuccess);
            long totalBytes = records.Sum(r => r.BytesTransferred);
            int totalAmount = records.Sum(r => r.Amount);
            SelectedFilterStats = new HistoryStats(totalItems, successfulItems, totalBytes, totalAmount);

            StatusMessage = $"Loaded {records.Count} records.";
        }

        [RelayCommand]
        private async Task ExportToCsvAsync()
        {
            if ((SelectedWeek == null && SelectedMonth == null) || Records.Count == 0)
            {
                StatusMessage = "No records to export.";
                return;
            }

            string fileName = SelectedWeek != null
                ? $"EasyCopier_History_{SelectedWeek.StartOfWeek:yyyy_MM_dd}.csv"
                : $"EasyCopier_History_{SelectedMonth!.Year}_{SelectedMonth.Month:D2}.csv";
            Dictionary<string, IList<string>> choices = new()
            { { "CSV File", new List<string> { ".csv" } } };

            string? filePath = await filePickerService.PickSaveFileAsync(fileName, choices);

            if (filePath != null)
            {
                StatusMessage = "Exporting...";
                bool success = await reportService.ExportHistoryToCsvAsync(filePath, Records);
                StatusMessage = success ? $"Exported successfully to {System.IO.Path.GetFileName(filePath)}" : "Export failed. Check logs.";
            }
        }
    }

    /// <summary>
    /// Represents a weekly filter option displaying start and end dates.
    /// </summary>
    /// <param name="StartOfWeek">The start date of the week.</param>
    /// <param name="EndOfWeek">The end date of the week.</param>
    /// <param name="DisplayName">The formatted display text for the week range.</param>
    public record WeekOption(DateTime StartOfWeek, DateTime EndOfWeek, string DisplayName);

    /// <summary>
    /// Represents a monthly filter option displaying year, month, and formatted display name.
    /// </summary>
    /// <param name="Year">The year component.</param>
    /// <param name="Month">The month component (1–12).</param>
    /// <param name="DisplayName">The formatted display text for the month.</param>
    public record MonthOption(int Year, int Month, string DisplayName);

    /// <summary>
    /// Encapsulates calculated summary metrics for a history period.
    /// </summary>
    /// <param name="TotalItems">Total number of transfer records.</param>
    /// <param name="SuccessfulItems">Number of successful transfer records.</param>
    /// <param name="TotalBytes">Total bytes transferred across all records.</param>
    /// <param name="TotalAmount">Total calculated monetary amount across all records.</param>
    public record HistoryStats(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount);
}
