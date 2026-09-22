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
        /// <summary>
        /// Gets the timestamp when the calculation session was logged.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the calculated total sum of the recorded session.
        /// </summary>
        public double Total { get; }

        /// <summary>
        /// Gets the list of individual numeric values entered during the calculation session.
        /// </summary>
        public IReadOnlyList<double> Entries { get; }

        /// <summary>
        /// Gets the formatted timestamp string using current culture settings.
        /// </summary>
        public string TimestampDisplay => Timestamp.ToString("M/d/yyyy h:mm:ss tt", CultureInfo.CurrentCulture);

        /// <summary>
        /// Gets the formatted total string for display.
        /// </summary>
        public string TotalDisplay => $"Total:{Total.ToString("0.####", CultureInfo.CurrentCulture)}";

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartAdderHistoryEntryViewModel"/> class from a persisted record.
        /// </summary>
        /// <param name="record">The persisted <see cref="SmartAdderHistoryRecord"/> to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is <c>null</c>.</exception>
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
