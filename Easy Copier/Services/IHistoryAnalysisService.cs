using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Encapsulates summary statistics for daily, weekly, and monthly transfer metrics.
    /// </summary>
    /// <param name="TodayStats">Statistics for the current day.</param>
    /// <param name="WeekStats">Statistics for the current week starting Sunday.</param>
    /// <param name="MonthStats">Statistics for the current month.</param>
    public record HistoryStatsSummary(
        HistoryStats TodayStats,
        HistoryStats WeekStats,
        HistoryStats MonthStats);

    /// <summary>
    /// Provides record clustering (15-minute drive window grouping) and statistic calculation services for copy history data.
    /// Encapsulates analytics logic into a testable domain service.
    /// </summary>
    public interface IHistoryAnalysisService
    {
        /// <summary>
        /// Processes a collection of raw history records by sorting them descending by timestamp,
        /// clustering transfers to the same drive within a 15-minute window to compute batch totals,
        /// and calculating aggregate summary statistics.
        /// </summary>
        /// <param name="records">The raw copy history records to process.</param>
        /// <returns>A tuple containing the sorted and clustered record list and aggregate <see cref="HistoryStats"/>.</returns>
        (List<CopyHistoryRecord> ProcessedRecords, HistoryStats Stats) AnalyzeAndClusterRecords(IEnumerable<CopyHistoryRecord> records);

        /// <summary>
        /// Asynchronously calculates Today, Week, and Month history statistics relative to the specified reference date.
        /// </summary>
        /// <param name="copyHistoryService">The copy history repository service.</param>
        /// <param name="referenceDate">The reference date to base calculations upon (typically <see cref="DateTime.Today"/>).</param>
        /// <returns>A task returning the <see cref="HistoryStatsSummary"/> containing Today, Week, and Month statistics.</returns>
        Task<HistoryStatsSummary> CalculateSummaryStatsAsync(ICopyHistoryService copyHistoryService, DateTime referenceDate);
    }
}
