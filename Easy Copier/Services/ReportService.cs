using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for exporting operation history and reporting data to external formats such as CSV.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Asynchronously exports copy history records to a CSV file at the specified file path.
        /// </summary>
        /// <param name="filePath">The target file path where CSV data will be saved.</param>
        /// <param name="records">The collection of <see cref="CopyHistoryRecord"/> items to export.</param>
        /// <returns>A task returning <c>true</c> if export succeeded; otherwise, <c>false</c>.</returns>
        Task<bool> ExportHistoryToCsvAsync(string filePath, IEnumerable<CopyHistoryRecord> records);
    }

    /// <summary>
    /// Provides reporting and CSV export services for operation records.
    /// </summary>
    /// <param name="logger">The logger instance for operational output.</param>
    public class ReportService(ILogger<ReportService> logger) : IReportService
    {
        /// <summary>
        /// Logger instance used for diagnostic logging.
        /// </summary>
        private readonly ILogger<ReportService> _logger = logger;

        /// <summary>
        /// Asynchronously exports copy history records to a UTF-8 encoded CSV file.
        /// </summary>
        /// <param name="filePath">Destination file path.</param>
        /// <param name="records">Collection of history records to write.</param>
        /// <returns>A task returning <c>true</c> if successful; otherwise, <c>false</c>.</returns>
        public async Task<bool> ExportHistoryToCsvAsync(string filePath, IEnumerable<CopyHistoryRecord> records)
        {
            ArgumentNullException.ThrowIfNull(records);
            try
            {
                using StreamWriter writer = new(filePath, false, Encoding.UTF8);
                await writer.WriteLineAsync("Id,Timestamp,GameName,TargetDriveLetter,TargetDriveLabel,BytesTransferred,IsSuccess,Amount,SourcePath,DestinationPath,ErrorLog,SubFilesJson");

                foreach (CopyHistoryRecord record in records)
                {
                    string id = record.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string timestamp = record.Timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                    string gameName = EscapeCsv(record.GameName);
                    string targetDriveLetter = EscapeCsv(record.TargetDriveLetter);
                    string targetDriveLabel = EscapeCsv(record.TargetDriveLabel);
                    string bytesTransferred = record.BytesTransferred.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string isSuccess = record.IsSuccess.ToString();
                    string amount = record.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string sourcePath = EscapeCsv(record.SourcePath);
                    string destinationPath = EscapeCsv(record.DestinationPath);
                    string errorLog = EscapeCsv(record.ErrorLog);
                    string subFilesJson = EscapeCsv(record.SubFilesJson);

                    await writer.WriteLineAsync($"{id},{timestamp},{gameName},{targetDriveLetter},{targetDriveLabel},{bytesTransferred},{isSuccess},{amount},{sourcePath},{destinationPath},{errorLog},{subFilesJson}");
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

        /// <summary>
        /// Escapes a CSV field value according to RFC 4180 rules if it contains commas, quotes, or newlines.
        /// </summary>
        /// <param name="value">The raw string value.</param>
        /// <returns>An escaped CSV column string value.</returns>
        private static string EscapeCsv(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal)
                ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
                : value;
        }
    }
}
