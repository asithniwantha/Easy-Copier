using Easy_Copier.Models;
using System.Collections.Generic;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for history record clustering and statistics summary calculations.
    /// </summary>
    public interface IHistoryAnalysisService
    {
        /// <summary>
        /// Clusters records targeting the same destination drive within a 15-minute time window and populates batch totals.
        /// </summary>
        /// <param name="records">The sequence of history records to cluster.</param>
        void ApplyBatchClustering(IEnumerable<CopyHistoryRecord> records);

        /// <summary>
        /// Calculates summary statistics (totals, bytes, amount, and success rate) for a set of history records.
        /// </summary>
        /// <param name="records">The sequence of history records to analyze.</param>
        /// <returns>A <see cref="HistoryStats"/> summary containing transfer metrics.</returns>
        HistoryStats CalculateStats(IEnumerable<CopyHistoryRecord> records);
    }
}
