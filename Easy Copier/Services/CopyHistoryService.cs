using Easy_Copier.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service interface for recording and querying copy operation history in a local SQLite database.
    /// </summary>
    public interface ICopyHistoryService
    {
        /// <summary>
        /// Initializes the underlying SQLite database schema and applies any pending migrations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Inserts a new copy operation history record into the database.
        /// </summary>
        /// <param name="record">The <see cref="CopyHistoryRecord"/> containing execution details to store.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddRecordAsync(CopyHistoryRecord record);

        /// <summary>
        /// Retrieves all copy history records that occurred within a specific week range.
        /// </summary>
        /// <param name="startOfWeek">The start boundary of the week (inclusive).</param>
        /// <param name="endOfWeek">The end boundary of the week.</param>
        /// <returns>A task that returns a list of matching <see cref="CopyHistoryRecord"/> instances ordered descending by timestamp.</returns>
        Task<List<CopyHistoryRecord>> GetRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek);

        /// <summary>
        /// Retrieves distinct start dates of weeks for which history records exist.
        /// </summary>
        /// <returns>A task that returns a list of Sunday start dates ordered descending.</returns>
        Task<List<DateTime>> GetAvailableWeeksAsync();

        /// <summary>
        /// Retrieves all copy history records for a specified year and month.
        /// </summary>
        /// <param name="year">The 4-digit year to filter by.</param>
        /// <param name="month">The month (1–12) to filter by.</param>
        /// <returns>A task that returns a list of matching <see cref="CopyHistoryRecord"/> instances ordered descending by timestamp.</returns>
        Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month);

        /// <summary>
        /// Retrieves distinct year and month pairs for which history records exist.
        /// </summary>
        /// <returns>A task that returns a list of tuples containing (Year, Month) ordered descending.</returns>
        Task<List<(int Year, int Month)>> GetAvailableMonthsAsync();

        /// <summary>
        /// Calculates aggregated statistics for history records within a specified date range.
        /// </summary>
        /// <param name="startDate">The start boundary of the date range (inclusive).</param>
        /// <param name="endDate">The end boundary of the date range (exclusive).</param>
        /// <returns>A task that returns a tuple containing total items, successful items, total bytes, and total monetary amount.</returns>
        Task<(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount)> GetStatsAsync(DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Provides functionality for persisting and querying file copy operation history stored in a local SQLite database file.
    /// </summary>
    public class CopyHistoryService : ICopyHistoryService
    {
        /// <summary>
        /// Logger instance used for recording operational logs and errors.
        /// </summary>
        private readonly ILogger<CopyHistoryService> _logger;

        /// <summary>
        /// Full file path to the local SQLite database file (`history.db`).
        /// </summary>
        private readonly string _dbPath;

        /// <summary>
        /// SQLite connection string used for establishing database connections.
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyHistoryService"/> class and configures database storage paths.
        /// </summary>
        /// <param name="logger">The logger instance for operational diagnostic output.</param>
        public CopyHistoryService(ILogger<CopyHistoryService> logger)
        {
            _logger = logger;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "EasyCopier");
            _ = Directory.CreateDirectory(appFolder);
            _dbPath = Path.Combine(appFolder, "history.db");
            _connectionString = $"Data Source={_dbPath}";
        }

        /// <summary>
        /// Asynchronously initializes the database schema for copy history and executes any required schema migrations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CopyHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        GameName TEXT NOT NULL,
                        TargetDriveLetter TEXT NOT NULL,
                        TargetDriveLabel TEXT NOT NULL,
                        BytesTransferred INTEGER NOT NULL,
                        IsSuccess INTEGER NOT NULL,
                        Amount INTEGER NOT NULL DEFAULT 0
                    )";

                _ = await command.ExecuteNonQueryAsync();

                // Schema Migration for older databases: check if Amount column exists
                command.CommandText = "PRAGMA table_info(CopyHistory)";
                bool hasAmount = false;
                using (SqliteDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.GetString(1).Equals("Amount", StringComparison.OrdinalIgnoreCase))
                        {
                            hasAmount = true;
                            break;
                        }
                    }
                }

                if (!hasAmount)
                {
                    command.CommandText = "ALTER TABLE CopyHistory ADD COLUMN Amount INTEGER NOT NULL DEFAULT 0";
                    _ = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Added 'Amount' column to CopyHistory table via schema migration.");
                }

                _logger.LogInformation("CopyHistory DB initialized at {Path}", _dbPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize CopyHistory DB at {Path}", _dbPath);
            }
        }

        /// <summary>
        /// Asynchronously inserts a new copy operation record into the SQLite database.
        /// </summary>
        /// <param name="record">The record detailing the outcome and parameters of a copy operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is <c>null</c>.</exception>
        public async Task AddRecordAsync(CopyHistoryRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO CopyHistory (Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess, Amount)
                    VALUES ($timestamp, $gameName, $targetDriveLetter, $targetDriveLabel, $bytesTransferred, $isSuccess, $amount)";

                // Use ISO 8601 string for reliable SQLite sorting/filtering
                _ = command.Parameters.AddWithValue("$timestamp", record.Timestamp.ToString("O"));
                _ = command.Parameters.AddWithValue("$gameName", record.GameName);
                _ = command.Parameters.AddWithValue("$targetDriveLetter", record.TargetDriveLetter);
                _ = command.Parameters.AddWithValue("$targetDriveLabel", record.TargetDriveLabel);
                _ = command.Parameters.AddWithValue("$bytesTransferred", record.BytesTransferred);
                _ = command.Parameters.AddWithValue("$isSuccess", record.IsSuccess ? 1 : 0);
                _ = command.Parameters.AddWithValue("$amount", record.Amount);

                _ = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add copy history record for {GameName}", record.GameName);
            }
        }

        /// <summary>
        /// Helper method that reads all history records stored in the database.
        /// </summary>
        /// <returns>A task that returns a list of all <see cref="CopyHistoryRecord"/> items.</returns>
        private async Task<List<CopyHistoryRecord>> GetAllRecordsAsync()
        {
            List<CopyHistoryRecord> records = [];
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess, Amount FROM CopyHistory";

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string timestampStr = reader.GetString(1);
                    if (DateTime.TryParse(timestampStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp))
                    {
                        records.Add(new CopyHistoryRecord(
                            reader.GetInt32(0),
                            timestamp,
                            reader.GetString(2),
                            reader.GetString(3),
                            reader.GetString(4),
                            reader.GetInt64(5),
                            reader.GetInt32(6) == 1,
                            await reader.IsDBNullAsync(7) ? 0 : reader.GetInt32(7)
                        ));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse Timestamp '{TimestampStr}' in CopyHistory", timestampStr);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all copy history records");
            }
            return records;
        }

        /// <summary>
        /// Retrieves records matching the designated week date range, sorted by timestamp descending.
        /// </summary>
        /// <param name="startOfWeek">The start date of the week.</param>
        /// <param name="endOfWeek">The end date of the week.</param>
        /// <returns>A task that returns a list of matching <see cref="CopyHistoryRecord"/> items.</returns>
        public async Task<List<CopyHistoryRecord>> GetRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek)
        {
            List<CopyHistoryRecord> allRecords = await GetAllRecordsAsync();

            // Adjust endOfWeek to be exclusive for < comparison, since endOfWeek is currently the date (e.g. 23:59:59 implied, or start of next day)
            // But since startOfWeek and endOfWeek logic usually uses >= and <=, let's just do inclusive if it's the exact day,
            // but normally endOfWeek is startOfWeek.AddDays(6).
            DateTime start = startOfWeek.Date;
            DateTime end = endOfWeek.Date.AddDays(1); // Make it exclusive

            return allRecords
                .Where(r => r.Timestamp >= start && r.Timestamp < end)
                .OrderByDescending(r => r.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Asynchronously determines all distinct week start dates present in the history database.
        /// </summary>
        /// <returns>A task that returns a list of week start dates ordered descending.</returns>
        public async Task<List<DateTime>> GetAvailableWeeksAsync()
        {
            List<CopyHistoryRecord> allRecords = await GetAllRecordsAsync();
            HashSet<DateTime> startOfWeeks = [];

            foreach (CopyHistoryRecord record in allRecords)
            {
                DateTime date = record.Timestamp.Date;
                int diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
                DateTime startOfWeek = date.AddDays(-1 * diff).Date;
                _ = startOfWeeks.Add(startOfWeek);
            }

            List<DateTime> sortedWeeks = [.. startOfWeeks];
            sortedWeeks.Sort((a, b) => b.CompareTo(a)); // Descending order
            return sortedWeeks;
        }

        /// <summary>
        /// Retrieves history records matching the specified year and month.
        /// </summary>
        /// <param name="year">The target year.</param>
        /// <param name="month">The target month (1–12).</param>
        /// <returns>A task that returns a list of matching <see cref="CopyHistoryRecord"/> items.</returns>
        public async Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month)
        {
            List<CopyHistoryRecord> allRecords = await GetAllRecordsAsync();
            return allRecords
                .Where(r => r.Timestamp.Year == year && r.Timestamp.Month == month)
                .OrderByDescending(r => r.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Asynchronously determines all distinct year/month combinations present in the history database.
        /// </summary>
        /// <returns>A task that returns a list of (Year, Month) tuples ordered descending.</returns>
        public async Task<List<(int Year, int Month)>> GetAvailableMonthsAsync()
        {
            List<CopyHistoryRecord> allRecords = await GetAllRecordsAsync();
            HashSet<(int Year, int Month)> months = [];

            foreach (CopyHistoryRecord record in allRecords)
            {
                _ = months.Add((record.Timestamp.Year, record.Timestamp.Month));
            }

            List<(int Year, int Month)> sortedMonths = [.. months];
            sortedMonths.Sort((a, b) => b.CompareTo(a)); // Descending order
            return sortedMonths;
        }

        /// <summary>
        /// Asynchronously computes summary statistics for copy operations within the specified date range.
        /// </summary>
        /// <param name="startDate">Start boundary date (inclusive).</param>
        /// <param name="endDate">End boundary date (exclusive).</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        ///   <item><description><c>TotalItems</c>: Total number of recorded copy operations.</description></item>
        ///   <item><description><c>SuccessfulItems</c>: Count of operations that completed successfully.</description></item>
        ///   <item><description><c>TotalBytes</c>: Total number of bytes transferred across all operations.</description></item>
        ///   <item><description><c>TotalAmount</c>: Total monetary value calculated across all operations.</description></item>
        /// </list>
        /// </returns>
        public async Task<(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount)> GetStatsAsync(DateTime startDate, DateTime endDate)
        {
            List<CopyHistoryRecord> allRecords = await GetAllRecordsAsync();

            List<CopyHistoryRecord> filteredRecords = allRecords
                .Where(r => r.Timestamp >= startDate && r.Timestamp < endDate)
                .ToList();

            int totalItems = filteredRecords.Count;
            int successfulItems = filteredRecords.Count(r => r.IsSuccess);
            long totalBytes = filteredRecords.Sum(r => r.BytesTransferred);
            int totalAmount = filteredRecords.Sum(r => r.Amount);

            return (totalItems, successfulItems, totalBytes, totalAmount);
        }
    }
}
