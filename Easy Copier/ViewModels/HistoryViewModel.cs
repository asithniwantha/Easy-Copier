using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Easy_Copier.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly ICopyHistoryService _copyHistoryService;
        private readonly IReportService _reportService;

        [ObservableProperty]
        private ObservableCollection<CopyHistoryRecord> _records = new();

        [ObservableProperty]
        private ObservableCollection<MonthOption> _availableMonths = new();

        [ObservableProperty]
        private MonthOption? _selectedMonth;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public HistoryViewModel(ICopyHistoryService copyHistoryService, IReportService reportService)
        {
            _copyHistoryService = copyHistoryService;
            _reportService = reportService;
        }

        public async Task InitializeAsync()
        {
            var months = await _copyHistoryService.GetAvailableMonthsAsync();
            AvailableMonths.Clear();

            foreach (var m in months)
            {
                var date = new DateTime(m.Year, m.Month, 1);
                AvailableMonths.Add(new MonthOption(m.Year, m.Month, date.ToString("MMMM yyyy")));
            }

            if (AvailableMonths.Any())
            {
                SelectedMonth = AvailableMonths.First();
            }
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
            var records = await _copyHistoryService.GetRecordsByMonthAsync(year, month);
            Records.Clear();
            foreach (var r in records)
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

            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"EasyCopier_History_{SelectedMonth.Year}_{SelectedMonth.Month:D2}.csv"
            };
            savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });

            // Using App.MainWindow for interop
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                StatusMessage = "Exporting...";
                var success = await _reportService.ExportHistoryToCsvAsync(file.Path, Records);
                if (success)
                {
                    StatusMessage = $"Exported successfully to {file.Name}";
                }
                else
                {
                    StatusMessage = "Export failed. Check logs.";
                }
            }
        }
    }

    public record MonthOption(int Year, int Month, string DisplayName);
}
