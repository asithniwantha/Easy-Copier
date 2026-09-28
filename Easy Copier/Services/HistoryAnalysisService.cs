using Easy_Copier.Models;
using Easy_Copier.ViewModels;
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
    }
}
