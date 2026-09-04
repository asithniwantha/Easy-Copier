using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    public sealed partial class SmartAdderViewModel : ObservableObject
    {
        private readonly IHistoryDialogService _historyDialogService;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<SmartAdderViewModel> _logger;

        [ObservableProperty]
        private double _totalSum;

        [ObservableProperty]
        private bool _isHovering;

        [ObservableProperty]
        private bool _isListFocused;

        public ObservableCollection<NumberCell> Cells { get; } = [];

        public SmartAdderViewModel(
            IHistoryDialogService historyDialogService,
            IDatabaseService databaseService,
            ILogger<SmartAdderViewModel> logger)
        {
            _historyDialogService = historyDialogService;
            _databaseService = databaseService;
            _logger = logger;

            AddNewCell();
        }

        private void AddNewCell()
        {
            NumberCell cell = new();
            cell.PropertyChanged += Cell_PropertyChanged;
            Cells.Add(cell);
        }

        private void Cell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(NumberCell.InputValue))
            {
                return;
            }

            EnsureOneEmptyBottomCell();
            RecalculateTotal();
        }

        private void EnsureOneEmptyBottomCell()
        {
            // Remove extra empty trailing cells
            while (Cells.Count > 1 &&
                   string.IsNullOrWhiteSpace(Cells[^1].InputValue) &&
                   string.IsNullOrWhiteSpace(Cells[^2].InputValue))
            {
                NumberCell last = Cells[^1];
                last.PropertyChanged -= Cell_PropertyChanged;
                Cells.RemoveAt(Cells.Count - 1);
            }

            // Ensure there is an empty trailing cell
            if (Cells.Count == 0 || !string.IsNullOrWhiteSpace(Cells[^1].InputValue))
            {
                AddNewCell();
            }
        }

        private void RecalculateTotal()
        {
            double sum = 0;
            foreach (var cell in Cells)
            {
                if (double.TryParse(cell.InputValue, out double val))
                {
                    sum += val;
                }
            }
            TotalSum = sum;
        }

        [RelayCommand]
        public void DeleteCell(NumberCell cell)
        {
            ArgumentNullException.ThrowIfNull(cell);

            int index = Cells.IndexOf(cell);
            if (index < 0)
            {
                return;
            }

            if (Cells.Count == 1)
            {
                cell.InputValue = string.Empty;
                return;
            }

            cell.PropertyChanged -= Cell_PropertyChanged;
            Cells.RemoveAt(index);

            EnsureOneEmptyBottomCell();
            RecalculateTotal();
        }

        [RelayCommand]
        private async Task OpenHistoryAsync()
        {
            await _historyDialogService.ShowHistoryDialogAsync();
        }

        [RelayCommand]
        private async Task ClearAllAsync()
        {
            try
            {
                var values = Cells
                    .Where(c => double.TryParse(c.InputValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                    .Select(c => double.Parse(c.InputValue, CultureInfo.InvariantCulture))
                    .ToArray();

                if (values.Length > 0)
                {
                    var record = new SmartAdderHistoryRecord
                    {
                        Timestamp = DateTime.Now,
                        EntriesJson = JsonSerializer.Serialize(values),
                        TotalSum = TotalSum
                    };
                    await _databaseService.AddRecordAsync(record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log SmartAdder history before clearing.");
            }

            foreach (var cell in Cells)
            {
                cell.PropertyChanged -= Cell_PropertyChanged;
            }
            Cells.Clear();
            TotalSum = 0;

            AddNewCell();
        }
    }
}
