using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    public class MonthOption
    {
        public int Year { get; }
        public int Month { get; }
        public string Label { get; }

        public MonthOption(int year, int month)
        {
            Year = year;
            Month = month;
            Label = $"{new DateTime(year, month, 1):MMMM yyyy}";
        }
    }

    public class HistoryViewModel : ObservableObject
    {
        private readonly ICopyHistoryService _historyService;
        private readonly IReportService _reportService;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            private set => SetProperty(ref _selectedYear, value);
        }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            private set => SetProperty(ref _selectedMonth, value);
        }

        private bool _isAllSelected = true;
        public bool IsAllSelected
        {
            get => _isAllSelected;
            private set => SetProperty(ref _isAllSelected, value);
        }

        public ObservableCollection<CopyHistoryEntry> Entries { get; } = new();
        public ObservableCollection<MonthOption> AvailableMonths { get; } = new();

        public IAsyncRelayCommand LoadAllCommand { get; }
        public IAsyncRelayCommand ExportCurrentCommand { get; }
        public IAsyncRelayCommand ClearAllHistoryCommand { get; }

        public HistoryViewModel(ICopyHistoryService historyService, IReportService reportService)
        {
            _historyService = historyService;
            _reportService = reportService;

            LoadAllCommand = new AsyncRelayCommand(LoadAllAsync);
            ExportCurrentCommand = new AsyncRelayCommand(ExportCurrentAsync);
            ClearAllHistoryCommand = new AsyncRelayCommand(ClearAllHistoryAsync);
        }

        public async Task InitializeAsync()
        {
            await LoadMonthsAsync();
            await LoadAllAsync();
        }

        private async Task LoadMonthsAsync()
        {
            var months = await _historyService.GetAvailableMonthsAsync();
            AvailableMonths.Clear();
            foreach (var (year, month) in months)
                AvailableMonths.Add(new MonthOption(year, month));
        }

        private async Task LoadAllAsync()
        {
            IsLoading = true;
            IsAllSelected = true;
            StatusMessage = "Loading history...";
            try
            {
                var entries = await _historyService.GetAllAsync();
                Entries.Clear();
                foreach (var e in entries)
                    Entries.Add(e);
                UpdateStatus();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadMonthAsync(MonthOption option)
        {
            IsLoading = true;
            IsAllSelected = false;
            SelectedYear = option.Year;
            SelectedMonth = option.Month;
            StatusMessage = $"Loading {option.Label}...";
            try
            {
                var entries = await _historyService.GetByMonthAsync(option.Year, option.Month);
                Entries.Clear();
                foreach (var e in entries)
                    Entries.Add(e);
                UpdateStatus();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportCurrentAsync()
        {
            if (!Entries.Any())
            {
                StatusMessage = "Nothing to export.";
                return;
            }

            string path;
            if (IsAllSelected)
            {
                var dir = Path.GetDirectoryName(_reportService.GetDefaultExportPath(DateTime.Now.Year, 1))!;
                path = Path.Combine(dir, $"CopyReport_All_{DateTime.Now:yyyy-MM-dd}.csv");
            }
            else
            {
                path = _reportService.GetDefaultExportPath(SelectedYear, SelectedMonth);
            }

            StatusMessage = "Exporting...";
            await _reportService.ExportToCsvAsync(Entries.ToList(), path);
            StatusMessage = $"Exported to {path}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.GetDirectoryName(path),
                    UseShellExecute = true
                });
            }
            catch { /* ignore */ }
        }

        private async Task ClearAllHistoryAsync()
        {
            await _historyService.DeleteAllAsync();
            Entries.Clear();
            AvailableMonths.Clear();
            StatusMessage = "All history cleared.";
        }

        public async Task RefreshAsync()
        {
            await LoadMonthsAsync();
            if (IsAllSelected)
                await LoadAllAsync();
            else
                await LoadMonthAsync(new MonthOption(SelectedYear, SelectedMonth));
        }

        private void UpdateStatus()
        {
            int total = Entries.Count;
            int success = Entries.Count(e => e.Success);
            string label = IsAllSelected ? "All time" : new MonthOption(SelectedYear, SelectedMonth).Label;
            StatusMessage = total == 0
                ? $"{label}: No records found."
                : $"{label}: {total} item(s) — {success} succeeded, {total - success} failed.";
        }
    }
}
