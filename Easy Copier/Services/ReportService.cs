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
            ArgumentNullException.ThrowIfNull(records);
            try
            {
                using StreamWriter writer = new(filePath, false, Encoding.UTF8);
                await writer.WriteLineAsync("Id,Timestamp,GameName,TargetDriveLetter,TargetDriveLabel,BytesTransferred,IsSuccess");

                foreach (CopyHistoryRecord record in records)
                {
                    string id = record.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string timestamp = record.Timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                    string gameName = EscapeCsv(record.GameName);
                    string targetDriveLetter = EscapeCsv(record.TargetDriveLetter);
                    string targetDriveLabel = EscapeCsv(record.TargetDriveLabel);
                    string bytesTransferred = record.BytesTransferred.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string isSuccess = record.IsSuccess.ToString();

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

        private static string EscapeCsv(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
                : value;
        }
    }
}
