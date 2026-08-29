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
    public partial class HistoryViewModel : ObservableObject
    {
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

        private readonly ICopyHistoryService _copyHistoryService;
        private readonly IReportService _reportService;
        private readonly Infrastructure.IFilePickerService _filePickerService;

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

        public HistoryViewModel(ICopyHistoryService copyHistoryService, IReportService reportService,
                                Infrastructure.IFilePickerService filePickerService)
        {
            _copyHistoryService = copyHistoryService;
            _reportService = reportService;
            _filePickerService = filePickerService;
        }

        public async Task InitializeAsync()
        {
            await LoadStatsAsync();

            List<DateTime> startOfWeeks = await _copyHistoryService.GetAvailableWeeksAsync();
            AvailableWeeks.UpdateFrom(startOfWeeks.Select(w => new WeekOption(w, w.AddDays(6), $"{w:MMM dd, yyyy} - {w.AddDays(6):MMM dd, yyyy}")));

            List<(int Year, int Month)> months = await _copyHistoryService.GetAvailableMonthsAsync();
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
            (int todayTotal, int todaySuccess, long todayBytes, int todayAmount) = await _copyHistoryService.GetStatsAsync(todayStart, todayEnd);
            TodayStats = new HistoryStats(todayTotal, todaySuccess, todayBytes, todayAmount);

            // Week (Starting Sunday)
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
            DateTime weekStart = today.AddDays(-1 * diff).Date;
            DateTime weekEnd = weekStart.AddDays(7);
            (int weekTotal, int weekSuccess, long weekBytes, int weekAmount) = await _copyHistoryService.GetStatsAsync(weekStart, weekEnd);
            WeekStats = new HistoryStats(weekTotal, weekSuccess, weekBytes, weekAmount);

            // Month
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1);
            (int monthTotal, int monthSuccess, long monthBytes, int monthAmount) = await _copyHistoryService.GetStatsAsync(monthStart, monthEnd);
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
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByWeekAsync(startOfWeek, endOfWeek);
            ProcessLoadedRecords(records);

            SelectedFilterName = $"Stats for {startOfWeek:MMM dd, yyyy} - {endOfWeek:MMM dd, yyyy}";
        }

        private async Task LoadRecordsByMonthAsync(int year, int month)
        {
            StatusMessage = "Loading records...";
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByMonthAsync(year, month);
            ProcessLoadedRecords(records);

            SelectedFilterName = $"Stats for {new DateTime(year, month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture)}";
        }

        private void ProcessLoadedRecords(List<CopyHistoryRecord> records)
        {
            // First sort descending so latest are on top
            List<CopyHistoryRecord> sortedRecords = records.OrderByDescending(r => r.Timestamp).ToList();

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

            string? filePath = await _filePickerService.PickSaveFileAsync(fileName, choices);

            if (filePath != null)
            {
                StatusMessage = "Exporting...";
                bool success = await _reportService.ExportHistoryToCsvAsync(filePath, Records);
                StatusMessage = success ? $"Exported successfully to {System.IO.Path.GetFileName(filePath)}" : "Export failed. Check logs.";
            }
        }
    }

    public record WeekOption(DateTime StartOfWeek, DateTime EndOfWeek, string DisplayName);

    public record MonthOption(int Year, int Month, string DisplayName);

    public record HistoryStats(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount);
}
