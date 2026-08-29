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
        public HistoryStats TodayStats { get; private set; } = new HistoryStats(0, 0, 0, 0);
        public HistoryStats WeekStats { get; private set; } = new HistoryStats(0, 0, 0, 0);
        public HistoryStats MonthStats { get; private set; } = new HistoryStats(0, 0, 0, 0);

        private readonly ICopyHistoryService _copyHistoryService;
        private readonly IReportService _reportService;
        private readonly Infrastructure.IFilePickerService _filePickerService;

        [ObservableProperty]
        public partial ObservableCollection<CopyHistoryRecord> Records { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<HistoryGroup> GroupedRecords { get; set; } = [];

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
                GroupedRecords.Clear();
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
                GroupedRecords.Clear();
            }
        }

        private async Task LoadRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek)
        {
            StatusMessage = "Loading records...";
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByWeekAsync(startOfWeek, endOfWeek);
            ProcessLoadedRecords(records);
        }

        private async Task LoadRecordsByMonthAsync(int year, int month)
        {
            StatusMessage = "Loading records...";
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByMonthAsync(year, month);
            ProcessLoadedRecords(records);
        }

        private void ProcessLoadedRecords(List<CopyHistoryRecord> records)
        {
            Records.UpdateFrom(records);

            // Group records by drive and approximate time (e.g., within 15 minutes of each other)
            List<HistoryGroup> groupedList = [];
            foreach (CopyHistoryRecord record in records.OrderByDescending(r => r.Timestamp))
            {
                // Find a group where the drive matches and the time difference is less than 15 minutes
                HistoryGroup? group = groupedList.FirstOrDefault(g =>
                    g.TargetDriveLetter == record.TargetDriveLetter &&
                    Math.Abs((g.GroupTimestamp - record.Timestamp).TotalMinutes) < 15);

                if (group == null)
                {
                    group = new HistoryGroup(record.TargetDriveLetter, record.Timestamp);
                    groupedList.Add(group);
                }

                group.Add(record);
            }

            GroupedRecords.UpdateFrom(groupedList);
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

    public class HistoryGroup : ObservableCollection<CopyHistoryRecord>, IGrouping<string, CopyHistoryRecord>
    {
        public string TargetDriveLetter { get; }
        public DateTime GroupTimestamp { get; }
        public string Key => $"Drive {TargetDriveLetter} \u2022 {GroupTimestamp:MMM dd, yyyy h:mm tt} (Total: {TotalAmount} \u2022 {FormattedTotalBytes})";
        public int TotalAmount => this.Sum(r => r.Amount);
        public string FormattedTotalBytes => Infrastructure.FormattingHelpers.FormatBytes(this.Sum(r => r.BytesTransferred));

        public HistoryGroup(string targetDriveLetter, DateTime groupTimestamp)
        {
            TargetDriveLetter = targetDriveLetter;
            GroupTimestamp = groupTimestamp;
        }
    }
}
