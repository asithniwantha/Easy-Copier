using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing copy history records, filter periods, statistical metrics, and history exports.
    /// Delegates record clustering and statistical analysis to <see cref="IHistoryAnalysisService"/>.
    /// </summary>
    public partial class HistoryViewModel(
        ICopyHistoryService copyHistoryService,
        IHistoryAnalysisService historyAnalysisService,
        IReportService reportService,
        IFilePickerService filePickerService) : ObservableObject
    {
        private readonly ICopyHistoryService _copyHistoryService = copyHistoryService ?? throw new ArgumentNullException(nameof(copyHistoryService));
        private readonly IHistoryAnalysisService _historyAnalysisService = historyAnalysisService ?? throw new ArgumentNullException(nameof(historyAnalysisService));
        private readonly IReportService _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        private readonly IFilePickerService _filePickerService = filePickerService ?? throw new ArgumentNullException(nameof(filePickerService));

        [ObservableProperty]
        public partial HistoryStats TodayStats { get; set; } = new HistoryStats(0, 0, 0, 0);

        [ObservableProperty]
        public partial HistoryStats WeekStats { get; set; } = new HistoryStats(0, 0, 0, 0);

        [ObservableProperty]
        public partial HistoryStats MonthStats { get; set; } = new HistoryStats(0, 0, 0, 0);

        [ObservableProperty]
        public partial HistoryStats SelectedFilterStats { get; set; } = new HistoryStats(0, 0, 0, 0);

        [ObservableProperty]
        public partial string SelectedFilterName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ObservableCollection<CopyHistoryRecord> Records { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<WeekOption> AvailableWeeks { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<MonthOption> AvailableMonths { get; set; } = [];

        [ObservableProperty]
        public partial WeekOption? SelectedWeek { get; set; }

        [ObservableProperty]
        public partial MonthOption? SelectedMonth { get; set; }

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        private bool _isClearingSelection;

        /// <summary>
        /// Asynchronously initializes summary stats, available weekly filter options, and monthly filter options.
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        public async Task InitializeAsync()
        {
            await LoadStatsAsync();

            List<DateTime> startOfWeeks = await _copyHistoryService.GetAvailableWeeksAsync();
            AvailableWeeks.UpdateFrom(startOfWeeks.Select(w => new WeekOption(w, w.AddDays(6), $"{w:MMM dd, yyyy} - {w.AddDays(6):MMM dd, yyyy}")));

            List<(int Year, int Month)> months = await _copyHistoryService.GetAvailableMonthsAsync();
            AvailableMonths.UpdateFrom(months.Select(m => new MonthOption(m.Year, m.Month, new DateTime(m.Year, m.Month, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture))));

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
            HistoryStatsSummary summary = await _historyAnalysisService.CalculateSummaryStatsAsync(_copyHistoryService, DateTime.Today);
            TodayStats = summary.TodayStats;
            WeekStats = summary.WeekStats;
            MonthStats = summary.MonthStats;
        }

        partial void OnSelectedWeekChanged(WeekOption? oldValue, WeekOption? newValue)
        {
            if (_isClearingSelection)
            {
                return;
            }

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
            if (_isClearingSelection)
            {
                return;
            }

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
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByWeekAsync(startOfWeek, endOfWeek);
            ProcessLoadedRecords(records);

            SelectedFilterName = $"Stats for {startOfWeek:MMM dd, yyyy} - {endOfWeek:MMM dd, yyyy}";
        }

        private async Task LoadRecordsByMonthAsync(int year, int month)
        {
            StatusMessage = "Loading records...";
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByMonthAsync(year, month);
            ProcessLoadedRecords(records);

            SelectedFilterName = $"Stats for {new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture)}";
        }

        private void ProcessLoadedRecords(List<CopyHistoryRecord> rawRecords)
        {
            (List<CopyHistoryRecord> processedRecords, HistoryStats stats) = _historyAnalysisService.AnalyzeAndClusterRecords(rawRecords);

            Records.UpdateFrom(processedRecords);
            SelectedFilterStats = stats;
            StatusMessage = $"Loaded {processedRecords.Count} records.";
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
    /// Represents a weekly history filter option.
    /// </summary>
    /// <param name="StartOfWeek">The start timestamp of the week.</param>
    /// <param name="EndOfWeek">The end timestamp of the week.</param>
    /// <param name="DisplayName">The formatted display string.</param>
    public record WeekOption(DateTime StartOfWeek, DateTime EndOfWeek, string DisplayName);

    /// <summary>
    /// Represents a monthly history filter option.
    /// </summary>
    /// <param name="Year">The target year.</param>
    /// <param name="Month">The target month index (1-12).</param>
    /// <param name="DisplayName">The formatted display string.</param>
    public record MonthOption(int Year, int Month, string DisplayName);

    /// <summary>
    /// Represents aggregate statistics for a specified history period.
    /// </summary>
    /// <param name="TotalItems">The total count of items in the period.</param>
    /// <param name="SuccessfulItems">The count of successfully transferred items.</param>
    /// <param name="TotalBytes">The total bytes transferred.</param>
    /// <param name="TotalAmount">The total financial/batch amount.</param>
    public record HistoryStats(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount);
}
