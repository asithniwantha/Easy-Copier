using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Implements <see cref="IHistoryAnalysisService"/> to handle history record drive-window clustering and stats calculations.
    /// </summary>
    public class HistoryAnalysisService : IHistoryAnalysisService
    {
        /// <inheritdoc />
        public void ApplyBatchClustering(IEnumerable<CopyHistoryRecord> records)
        {
            if (records == null)
            {
                return;
            }

            List<List<CopyHistoryRecord>> clusters = [];

            foreach (CopyHistoryRecord record in records)
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

            foreach (List<CopyHistoryRecord> cluster in clusters)
            {
                int clusterTotal = cluster.Sum(r => r.Amount);
                foreach (CopyHistoryRecord record in cluster)
                {
                    record.BatchAmount = clusterTotal;
                }
            }
        }

        /// <inheritdoc />
        public HistoryStats CalculateStats(IEnumerable<CopyHistoryRecord> records)
        {
            if (records == null)
            {
                return new HistoryStats(0, 0, 0, 0, "0%");
            }

            List<CopyHistoryRecord> recordList = records.ToList();
            int totalItems = recordList.Count;
            int successfulItems = recordList.Count(r => r.IsSuccess);
            long totalBytes = recordList.Sum(r => r.BytesTransferred);
            int totalAmount = recordList.Sum(r => r.Amount);
            string successRate = totalItems == 0 ? "0%" : $"{Math.Round((double)successfulItems / totalItems * 100)}%";

            return new HistoryStats(totalItems, successfulItems, totalBytes, totalAmount, successRate);
        }

        /// <inheritdoc />
        public DashboardStats BuildDashboard(IEnumerable<CopyHistoryRecord> records, int rangeDays, int topCount = 5)
        {
            ArgumentNullException.ThrowIfNull(records);

            DateTime today = DateTime.Today;
            DateTime start = today.AddDays(-(Math.Max(1, rangeDays) - 1));
            List<CopyHistoryRecord> inRange = records.Where(r => r.Timestamp.Date >= start && r.Timestamp.Date <= today).ToList();

            List<(string Label, int Total, int Failed, long Bytes)> days = [];
            for (DateTime day = start; day <= today; day = day.AddDays(1))
            {
                List<CopyHistoryRecord> dayRecords = inRange.Where(r => r.Timestamp.Date == day).ToList();
                days.Add((day.ToString("MMM d", System.Globalization.CultureInfo.CurrentCulture), dayRecords.Count, dayRecords.Count(r => !r.IsSuccess), dayRecords.Sum(r => r.BytesTransferred)));
            }

            List<(string Label, int Total, int Failed, long Bytes)> weeks = [];
            for (DateTime weekStart = start; weekStart <= today; weekStart = weekStart.AddDays(7))
            {
                DateTime weekEnd = weekStart.AddDays(7);
                List<CopyHistoryRecord> weekRecords = inRange.Where(r => r.Timestamp.Date >= weekStart && r.Timestamp.Date < weekEnd).ToList();
                weeks.Add(($"Wk of {weekStart:MMM d}", weekRecords.Count, weekRecords.Count(r => !r.IsSuccess), weekRecords.Sum(r => r.BytesTransferred)));
            }

            List<TopCopiedItem> top = inRange
                .GroupBy(r => r.GameName)
                .Select(g => new TopCopiedItem(g.Key, g.Count(), g.Sum(r => r.BytesTransferred)))
                .OrderByDescending(t => t.CopyCount)
                .ThenByDescending(t => t.TotalBytes)
                .Take(topCount)
                .ToList();

            double failureRate = inRange.Count == 0 ? 0 : (double)inRange.Count(r => !r.IsSuccess) / inRange.Count * 100;
            return new DashboardStats(ToPeriods(days), ToPeriods(weeks), top, failureRate);
        }

        private static List<PeriodTotal> ToPeriods(List<(string Label, int Total, int Failed, long Bytes)> source)
        {
            long max = source.Count == 0 ? 0 : source.Max(s => s.Bytes);
            return [.. source.Select(s => new PeriodTotal(s.Label, s.Total, s.Failed, s.Bytes, max == 0 ? 0 : (double)s.Bytes / max))];
        }
    }
}
