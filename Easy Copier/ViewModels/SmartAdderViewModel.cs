using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Backs the floating SmartAdder overlay: a dynamic list of number entries that
    /// automatically grows as the user types and shows a continuously updated running total.
    /// </summary>
    public sealed partial class SmartAdderViewModel : ObservableObject
    {
        private readonly ISmartAdderHistoryService _smartAdderHistoryService;
        private readonly IWindowService _windowService;
        private readonly ILogger<SmartAdderViewModel> _logger;

        [ObservableProperty]
        public partial double Total { get; set; }

        [ObservableProperty]
        public partial bool IsExpanded { get; set; }

        public ObservableCollection<SmartAdderEntry> Entries { get; } = [];

        public SmartAdderViewModel(
            ISmartAdderHistoryService smartAdderHistoryService,
            IWindowService windowService,
            ILogger<SmartAdderViewModel> logger)
        {
            _smartAdderHistoryService = smartAdderHistoryService;
            _windowService = windowService;
            _logger = logger;

            AddNewEntry();
        }

        private void AddNewEntry()
        {
            SmartAdderEntry entry = new() { Index = Entries.Count };
            entry.PropertyChanged += Entry_PropertyChanged;
            Entries.Add(entry);
        }

        private void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SmartAdderEntry.Text) || sender is not SmartAdderEntry entry)
            {
                return;
            }

            entry.Value = double.TryParse(entry.Text, out double parsed) ? parsed : null;

            AddEntryIfNeeded();
            RemoveEmptyTrailingEntries();
            RecalculateTotal();
        }

        [RelayCommand]
        private void AddEntryIfNeeded()
        {
            if (Entries.Count == 0 || !Entries[^1].IsEmpty)
            {
                AddNewEntry();
            }
        }

        [RelayCommand]
        private void RemoveEmptyTrailingEntries()
        {
            while (Entries.Count > 1 && Entries[^1].IsEmpty && Entries[^2].IsEmpty)
            {
                SmartAdderEntry last = Entries[^1];
                last.PropertyChanged -= Entry_PropertyChanged;
                Entries.RemoveAt(Entries.Count - 1);
            }

            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Index = i;
            }
        }

        private void RecalculateTotal()
        {
            Total = Entries.Where(e => e.Value.HasValue).Sum(e => e.Value!.Value);
        }

        public void EnsureNextEntry()
        {
            if (Entries.Count == 0 || Entries[^1].IsEmpty)
            {
                AddNewEntry();
            }
        }

        public void RemoveEntry(SmartAdderEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            int entryIndex = Entries.IndexOf(entry);
            if (entryIndex < 0)
            {
                return;
            }

            if (Entries.Count == 1)
            {
                entry.Text = string.Empty;
                return;
            }

            entry.PropertyChanged -= Entry_PropertyChanged;
            Entries.RemoveAt(entryIndex);
            RemoveEmptyTrailingEntries();
            RecalculateTotal();
        }

        [RelayCommand]
        private void OpenHistory()
        {
            _windowService.ShowSmartAdderHistoryWindow();
        }

        [RelayCommand]
        private async Task ClearAndLogHistoryAsync()
        {
            try
            {
                double[] values = [.. Entries.Where(e => e.Value.HasValue).Select(e => e.Value!.Value)];

                if (values.Length > 0)
                {
                    SmartAdderHistoryRecord record = new()
                    {
                        Timestamp = DateTime.Now,
                        EntriesJson = JsonSerializer.Serialize(values),
                        Total = Total
                    };

                    await _smartAdderHistoryService.AddRecordAsync(record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log SmartAdder history before clearing.");
            }

            foreach (SmartAdderEntry entry in Entries)
            {
                entry.PropertyChanged -= Entry_PropertyChanged;
            }
            Entries.Clear();
            Total = 0;

            AddNewEntry();
        }
    }
}
