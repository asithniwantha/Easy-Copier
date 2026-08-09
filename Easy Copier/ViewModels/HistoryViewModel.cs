using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        public HistoryStats TodayStats { get; private set; }
        public HistoryStats WeekStats { get; private set; }
        public HistoryStats MonthStats { get; private set; }

        private readonly ICopyHistoryService _copyHistoryService;
        private readonly IReportService _reportService;
        private readonly Infrastructure.IFilePickerService _filePickerService;

        [ObservableProperty]
        private ObservableCollection<CopyHistoryRecord> _records = [];

        [ObservableProperty]
        private ObservableCollection<MonthOption> _availableMonths = [];

        [ObservableProperty]
        private MonthOption? _selectedMonth;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

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

            List<(int Year, int Month)> months = await _copyHistoryService.GetAvailableMonthsAsync();
            AvailableMonths.Clear();

            foreach ((int Year, int Month) in months)
            {
                DateTime date = new(Year, Month, 1);
                AvailableMonths.Add(new MonthOption(Year, Month, date.ToString("MMMM yyyy")));
            }

            if (AvailableMonths.Any())
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
            (int todayTotal, int todaySuccess, long todayBytes) = await _copyHistoryService.GetStatsAsync(todayStart, todayEnd);
            TodayStats = new HistoryStats(todayTotal, todaySuccess, todayBytes);

            // Week (Starting Sunday)
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
            DateTime weekStart = today.AddDays(-1 * diff).Date;
            DateTime weekEnd = weekStart.AddDays(7);
            (int weekTotal, int weekSuccess, long weekBytes) = await _copyHistoryService.GetStatsAsync(weekStart, weekEnd);
            WeekStats = new HistoryStats(weekTotal, weekSuccess, weekBytes);

            // Month
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1);
            (int monthTotal, int monthSuccess, long monthBytes) = await _copyHistoryService.GetStatsAsync(monthStart, monthEnd);
            MonthStats = new HistoryStats(monthTotal, monthSuccess, monthBytes);
        }

        partial void OnSelectedMonthChanged(MonthOption? value)
        {
            if (value != null)
            {
                _ = LoadRecordsAsync(value.Year, value.Month);
            }
            else
            {
                Records.Clear();
            }
        }

        private async Task LoadRecordsAsync(int year, int month)
        {
            StatusMessage = "Loading records...";
            List<CopyHistoryRecord> records = await _copyHistoryService.GetRecordsByMonthAsync(year, month);
            Records.Clear();
            foreach (CopyHistoryRecord r in records)
            {
                Records.Add(r);
            }
            StatusMessage = $"Loaded {records.Count} records.";
        }

        [RelayCommand]
        private async Task ExportToCsvAsync()
        {
            if (SelectedMonth == null || !Records.Any())
            {
                StatusMessage = "No records to export.";
                return;
            }

            string fileName = $"EasyCopier_History_{SelectedMonth.Year}_{SelectedMonth.Month:D2}.csv";
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

    public record MonthOption(int Year, int Month, string DisplayName);

    public record HistoryStats(int TotalItems, int SuccessfulItems, long TotalBytes);
}
