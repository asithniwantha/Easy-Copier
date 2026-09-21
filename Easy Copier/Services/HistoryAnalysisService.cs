using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IHistoryAnalysisService"/> for copy history clustering and statistic aggregation.
    /// </summary>
    public class HistoryAnalysisService : IHistoryAnalysisService
    {
        /// <inheritdoc />
        public (List<CopyHistoryRecord> ProcessedRecords, HistoryStats Stats) AnalyzeAndClusterRecords(IEnumerable<CopyHistoryRecord> records)
        {
            if (records == null)
            {
                return ([], new HistoryStats(0, 0, 0, 0));
            }

            List<CopyHistoryRecord> sortedRecords = records
                .OrderByDescending(r => r.Timestamp)
                .ToList();

            // Group records by target drive letter and 15-minute time window to compute batch totals
            List<List<CopyHistoryRecord>> clusters = [];
            foreach (CopyHistoryRecord record in sortedRecords)
            {
                List<CopyHistoryRecord>? cluster = clusters.FirstOrDefault(c =>
                    c.First().TargetDriveLetter == record.TargetDriveLetter &&
                    Math.Abs((c.First().Timestamp - record.Timestamp).TotalMinutes) < 15);

                if (cluster == null)
                {
                    cluster = [];
                    clusters.Add(cluster);
                }

                cluster.Add(record);
            }

            // Assign the computed batch sum back to each record
            foreach (List<CopyHistoryRecord> cluster in clusters)
            {
                int clusterTotal = cluster.Sum(r => r.Amount);
                foreach (CopyHistoryRecord record in cluster)
                {
                    record.BatchAmount = clusterTotal;
                }
            }

            // Calculate aggregated filter statistics
            int totalItems = sortedRecords.Count;
            int successfulItems = sortedRecords.Count(r => r.IsSuccess);
            long totalBytes = sortedRecords.Sum(r => r.BytesTransferred);
            int totalAmount = sortedRecords.Sum(r => r.Amount);

            HistoryStats stats = new(totalItems, successfulItems, totalBytes, totalAmount);
            return (sortedRecords, stats);
        }

        /// <inheritdoc />
        public async Task<HistoryStatsSummary> CalculateSummaryStatsAsync(ICopyHistoryService copyHistoryService, DateTime referenceDate)
        {
            ArgumentNullException.ThrowIfNull(copyHistoryService);

            DateTime today = referenceDate.Date;

            // Today
            DateTime todayStart = today;
            DateTime todayEnd = today.AddDays(1);
            (int todayTotal, int todaySuccess, long todayBytes, int todayAmount) = await copyHistoryService.GetStatsAsync(todayStart, todayEnd);
            HistoryStats todayStats = new(todayTotal, todaySuccess, todayBytes, todayAmount);

            // Week (Starting Sunday)
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
            DateTime weekStart = today.AddDays(-1 * diff).Date;
            DateTime weekEnd = weekStart.AddDays(7);
            (int weekTotal, int weekSuccess, long weekBytes, int weekAmount) = await copyHistoryService.GetStatsAsync(weekStart, weekEnd);
            HistoryStats weekStats = new(weekTotal, weekSuccess, weekBytes, weekAmount);

            // Month
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1);
            (int monthTotal, int monthSuccess, long monthBytes, int monthAmount) = await copyHistoryService.GetStatsAsync(monthStart, monthEnd);
            HistoryStats monthStats = new(monthTotal, monthSuccess, monthBytes, monthAmount);

            return new HistoryStatsSummary(todayStats, weekStats, monthStats);
        }
    }
}
