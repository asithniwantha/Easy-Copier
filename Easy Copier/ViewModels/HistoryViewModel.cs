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
using System.Text.Json;

namespace Easy_Copier.ViewModels
{
    public enum DateFilter
    {
        Today,
        Last7Days,
        ThisMonth,
        Custom
    }

    public enum StatusFilter
    {
        All,
        Completed,
        Failed
    }

    public partial class HistoryViewModel : ObservableObject
    {
        private readonly ICopyHistoryService _copyHistoryService;
        private readonly IReportService _reportService;
        private readonly Infrastructure.IFilePickerService _filePickerService;
        private List<CopyHistoryRecord> _allRecords = [];

        public HistoryViewModel(ICopyHistoryService copyHistoryService, IReportService reportService, Infrastructure.IFilePickerService filePickerService)
        {
            _copyHistoryService = copyHistoryService;
            _reportService = reportService;
            _filePickerService = filePickerService;

            // Initialize default filter options
            AvailableDateFilters = new ObservableCollection<string> { "Today", "Last 7 Days", "This Month", "All Time" };
            AvailableStatusFilters = new ObservableCollection<string> { "All", "Completed", "Failed" };
            SelectedDateFilterString = "Last 7 Days";
            SelectedStatusFilterString = "All";
        }

        [ObservableProperty]
        public partial HistoryStats SelectedFilterStats { get; set; } = new HistoryStats(0, 0, 0, 0, "0%");

        [ObservableProperty]
        public partial ObservableCollection<CopyHistoryRecord> Records { get; set; } = [];

        [ObservableProperty]
        public partial CopyHistoryRecord? SelectedRecord { get; set; }

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string SearchQuery { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ObservableCollection<string> AvailableDateFilters { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string> AvailableStatusFilters { get; set; }

        [ObservableProperty]
        public partial string SelectedDateFilterString { get; set; }

        [ObservableProperty]
        public partial string SelectedStatusFilterString { get; set; }

        [ObservableProperty]
        public partial bool IsDrawerOpen { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string> SelectedRecordSubFiles { get; set; } = [];

        private bool _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            StatusMessage = "Loading records...";
            // We load all records here and then filter in memory for responsive UI
            // Depending on dataset size, this could be refactored to fetch only the required date range.
            List<CopyHistoryRecord> records = [];

            // Workaround for SQLite / C# DateTime.MaxValue parsing errors in SQLite queries.
            // Some databases struggle with Date <= DateTime.MaxValue. Let's use a safe ceiling and floor.
            DateTime minDate = new DateTime(2000, 1, 1);
            DateTime maxDate = new DateTime(2100, 1, 1);
            records = await _copyHistoryService.GetRecordsByWeekAsync(minDate, maxDate);
            _allRecords = records;
            _isInitialized = true;
            ApplyFilters();
        }

        partial void OnSearchQueryChanged(string value) => ApplyFilters();
        partial void OnSelectedDateFilterStringChanged(string value) => ApplyFilters();
        partial void OnSelectedStatusFilterStringChanged(string value) => ApplyFilters();
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
                        var files = JsonSerializer.Deserialize<List<string>>(value.SubFilesJson);
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

        [RelayCommand]
        private void CloseDrawer()
        {
            SelectedRecord = null;
        }

        private void ApplyFilters()
        {
            if (!_isInitialized) return;

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
                string query = SearchQuery.ToLowerInvariant();
                filtered = filtered.Where(r =>
                    r.GameName.ToLowerInvariant().Contains(query) ||
                    r.TargetDriveLetter.ToLowerInvariant().Contains(query) ||
                    r.TargetDriveLabel.ToLowerInvariant().Contains(query)
                );
            }

            List<CopyHistoryRecord> sortedRecords = filtered.OrderByDescending(r => r.Timestamp).ToList();

            // Compute batch amount (same as before)
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

            foreach (List<CopyHistoryRecord> cluster in clusters)
            {
                int clusterTotal = cluster.Sum(r => r.Amount);
                foreach (CopyHistoryRecord record in cluster)
                {
                    record.BatchAmount = clusterTotal;
                }
            }

            Records.UpdateFrom(sortedRecords);

            // Compute Stats
            int totalItems = sortedRecords.Count;
            int successfulItems = sortedRecords.Count(r => r.IsSuccess);
            long totalBytes = sortedRecords.Sum(r => r.BytesTransferred);
            int totalAmount = sortedRecords.Sum(r => r.Amount);
            string successRate = totalItems == 0 ? "0%" : $"{Math.Round((double)successfulItems / totalItems * 100)}%";

            SelectedFilterStats = new HistoryStats(totalItems, successfulItems, totalBytes, totalAmount, successRate);
            StatusMessage = $"Showing {totalItems} records.";
        }

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

    public record HistoryStats(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount, string SuccessRate);
}