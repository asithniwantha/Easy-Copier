using System;

namespace Easy_Copier.Models
{
    /// <summary>
    /// Represents a single logged SmartAdder calculation session, persisted to SQLite.
    /// </summary>
    public sealed class SmartAdderHistoryRecord
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        /// <summary>
        /// JSON-serialized array of the numeric values entered for this session.
        /// </summary>
        public string EntriesJson { get; set; } = "[]";

        public double Total { get; set; }
    }
}
