using System;

namespace Easy_Copier.Models
{
    /// <summary>
    /// Represents a stored SmartAdder calculation entry in SQLite history.
    /// </summary>
    public class SmartAdderHistoryRecord
    {
        /// <summary>
        /// Gets or sets the database auto-increment primary key ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the record was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the JSON representation of the entry list.
        /// </summary>
        public string EntriesJson { get; set; } = "[]";

        /// <summary>
        /// Gets or sets the calculated total sum of the entries.
        /// </summary>
        public double TotalSum { get; set; }

        /// <summary>
        /// Gets or sets the total sum, acting as an alias for <see cref="TotalSum"/>.
        /// </summary>
        public double Total
        {
            get => TotalSum;
            set => TotalSum = value;
        }
    }
}
