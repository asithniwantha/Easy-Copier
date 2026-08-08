using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface IReportService
    {
        Task<bool> ExportHistoryToCsvAsync(string filePath, IEnumerable<CopyHistoryRecord> records);
    }

    public class ReportService(ILogger<ReportService> logger) : IReportService
    {
        private readonly ILogger<ReportService> _logger = logger;

        public async Task<bool> ExportHistoryToCsvAsync(string filePath, IEnumerable<CopyHistoryRecord> records)
        {
            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                await writer.WriteLineAsync("Id,Timestamp,GameName,TargetDriveLetter,TargetDriveLabel,BytesTransferred,IsSuccess");

                foreach (var record in records)
                {
                    var id = record.Id.ToString();
                    var timestamp = record.Timestamp.ToString("O");
                    var gameName = EscapeCsv(record.GameName);
                    var targetDriveLetter = EscapeCsv(record.TargetDriveLetter);
                    var targetDriveLabel = EscapeCsv(record.TargetDriveLabel);
                    var bytesTransferred = record.BytesTransferred.ToString();
                    var isSuccess = record.IsSuccess.ToString();

                    await writer.WriteLineAsync($"{id},{timestamp},{gameName},{targetDriveLetter},{targetDriveLabel},{bytesTransferred},{isSuccess}");
                }

                _logger.LogInformation("Successfully exported history to {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export history to CSV at {FilePath}", filePath);
                return false;
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
