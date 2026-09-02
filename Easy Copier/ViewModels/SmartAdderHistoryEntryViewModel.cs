using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Presents a single persisted <see cref="SmartAdderHistoryRecord"/> for display in the
    /// SmartAdder Calculation History window, including the individually entered values.
    /// </summary>
    public sealed class SmartAdderHistoryEntryViewModel
    {
        public DateTime Timestamp { get; }

        public double Total { get; }

        public IReadOnlyList<double> Entries { get; }

        public string TimestampDisplay => Timestamp.ToString("M/d/yyyy h:mm:ss tt", CultureInfo.CurrentCulture);

        public string TotalDisplay => $"Total:{Total.ToString("0.####", CultureInfo.CurrentCulture)}";

        public SmartAdderHistoryEntryViewModel(SmartAdderHistoryRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            Timestamp = record.Timestamp;
            Total = record.Total;

            try
            {
                Entries = JsonSerializer.Deserialize<List<double>>(record.EntriesJson) ?? [];
            }
            catch (JsonException)
            {
                Entries = [];
            }
        }
    }
}
