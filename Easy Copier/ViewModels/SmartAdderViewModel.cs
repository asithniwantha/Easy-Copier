using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel that drives the SmartAdder calculator panel for computing sequential totals and recording history.
    /// </summary>
    public sealed partial class SmartAdderViewModel : ObservableObject
    {
        private readonly IWindowService _windowService;
        private readonly ISmartAdderHistoryService _smartAdderHistoryService;
        private readonly ILogger<SmartAdderViewModel> _logger;

        /// <summary>
        /// Gets or sets the total sum computed from all numeric input cells.
        /// </summary>
        [ObservableProperty]
        public partial double TotalSum { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pointer is hovering over the SmartAdder UI.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPanelVisible))]
        public partial bool IsHovering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an input field in the cell list currently holds focus.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPanelVisible))]
        public partial bool IsListFocused { get; set; }

        /// <summary>
        /// Gets a value indicating whether the SmartAdder panel should remain visible based on hover or focus state.
        /// </summary>
        public bool IsPanelVisible => IsHovering || IsListFocused;

        /// <summary>
        /// Gets the collection of dynamic numeric input cells.
        /// </summary>
        public ObservableCollection<NumberCell> Cells { get; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartAdderViewModel"/> class with necessary infrastructure services.
        /// </summary>
        /// <param name="windowService">The service for managing application window presentation.</param>
        /// <param name="smartAdderHistoryService">The service for persisting calculation history records.</param>
        /// <param name="logger">The logger instance for logging operation warnings and errors.</param>
        public SmartAdderViewModel(
            IWindowService windowService,
            ISmartAdderHistoryService smartAdderHistoryService,
            ILogger<SmartAdderViewModel> logger)
        {
            _windowService = windowService;
            _smartAdderHistoryService = smartAdderHistoryService;
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
            foreach (NumberCell cell in Cells)
            {
                if (double.TryParse(cell.InputValue, out double val))
                {
                    sum += val;
                }
            }
            TotalSum = sum;
        }

        /// <summary>
        /// Deletes the specified cell from the collection and updates the calculation total.
        /// </summary>
        /// <param name="cell">The <see cref="NumberCell"/> instance to remove.</param>
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
        private void OpenHistory()
        {
            _windowService.ShowSmartAdderHistoryWindow();
        }

        /// <summary>
        /// Asynchronously logs the current valid non-zero entry list to history storage and resets all cells.
        /// </summary>
        /// <returns>A task representing the asynchronous clear and history recording operation.</returns>
        [RelayCommand]
        private async Task ClearAllAsync()
        {
            try
            {
                double[] values = Cells
                    .Where(c => double.TryParse(c.InputValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                    .Select(c => double.Parse(c.InputValue, CultureInfo.InvariantCulture))
                    .ToArray();

                if (values.Length > 0)
                {
                    SmartAdderHistoryRecord record = new()
                    {
                        Timestamp = DateTime.Now,
                        EntriesJson = JsonSerializer.Serialize(values),
                        TotalSum = TotalSum
                    };
                    await _smartAdderHistoryService.AddRecordAsync(record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log SmartAdder history before clearing.");
            }

            foreach (NumberCell cell in Cells)
            {
                cell.PropertyChanged -= Cell_PropertyChanged;
            }
            Cells.Clear();
            TotalSum = 0;

            AddNewCell();
        }
    }
}
