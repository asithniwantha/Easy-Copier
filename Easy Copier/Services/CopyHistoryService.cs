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
    public interface ICopyHistoryService
    {
        Task InitializeAsync();
        Task AddRecordAsync(CopyHistoryRecord record);
        Task<List<CopyHistoryRecord>> GetRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek);
        Task<List<DateTime>> GetAvailableWeeksAsync();
        Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month);
        Task<List<(int Year, int Month)>> GetAvailableMonthsAsync();
        Task<(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount)> GetStatsAsync(DateTime startDate, DateTime endDate);
    }

    public class CopyHistoryService : ICopyHistoryService
    {
        private readonly ILogger<CopyHistoryService> _logger;
        private readonly string _dbPath;
        private readonly string _connectionString;

        public CopyHistoryService(ILogger<CopyHistoryService> logger)
        {
            _logger = logger;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localAppData, "EasyCopier");
            _ = Directory.CreateDirectory(appFolder);
            _dbPath = Path.Combine(appFolder, "history.db");
            _connectionString = $"Data Source={_dbPath}";
        }

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

        public async Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month)
        {
            List<CopyHistoryRecord> allRecords = await GetAllRecordsAsync();
            return allRecords
                .Where(r => r.Timestamp.Year == year && r.Timestamp.Month == month)
                .OrderByDescending(r => r.Timestamp)
                .ToList();
        }

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
