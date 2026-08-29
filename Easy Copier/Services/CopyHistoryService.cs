using Easy_Copier.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface ICopyHistoryService
    {
        Task InitializeAsync();
        Task AddRecordAsync(CopyHistoryRecord record);
        Task<List<CopyHistoryRecord>> GetRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek);
        Task<List<DateTime>> GetAvailableWeeksAsync();
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

        public async Task<List<CopyHistoryRecord>> GetRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek)
        {
            List<CopyHistoryRecord> records = [];
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess, Amount
                    FROM CopyHistory
                    WHERE substr(Timestamp, 1, 10) >= $startDate AND substr(Timestamp, 1, 10) <= $endDate
                    ORDER BY Timestamp DESC";
                _ = command.Parameters.AddWithValue("$startDate", startOfWeek.ToString("yyyy-MM-dd"));
                _ = command.Parameters.AddWithValue("$endDate", endOfWeek.ToString("yyyy-MM-dd"));

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new CopyHistoryRecord(
                        reader.GetInt32(0),
                        DateTime.Parse(reader.GetString(1), System.Globalization.CultureInfo.InvariantCulture),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.GetInt64(5),
                        reader.GetInt32(6) == 1,
                        reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get copy history records for week {Start}-{End}", startOfWeek, endOfWeek);
            }
            return records;
        }

        public async Task<List<DateTime>> GetAvailableWeeksAsync()
        {
            HashSet<DateTime> startOfWeeks = [];
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                // Extract the YYYY-MM-DD part to group distinct dates
                command.CommandText = "SELECT DISTINCT substr(Timestamp, 1, 10) FROM CopyHistory ORDER BY substr(Timestamp, 1, 10) DESC";

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string dateStr = reader.GetString(0);
                    if (DateTime.TryParse(dateStr, out DateTime date))
                    {
                        // Calculate start of week (Sunday)
                        int diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
                        DateTime startOfWeek = date.AddDays(-1 * diff).Date;
                        _ = startOfWeeks.Add(startOfWeek);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available weeks from copy history");
            }

            List<DateTime> sortedWeeks = [.. startOfWeeks];
            sortedWeeks.Sort((a, b) => b.CompareTo(a)); // Descending order
            return sortedWeeks;
        }

        public async Task<(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount)> GetStatsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        COUNT(*),
                        SUM(IsSuccess),
                        SUM(BytesTransferred),
                        SUM(Amount)
                    FROM CopyHistory
                    WHERE substr(Timestamp, 1, 10) >= $startDate AND substr(Timestamp, 1, 10) < $endDate";

                _ = command.Parameters.AddWithValue("$startDate", startDate.ToString("yyyy-MM-dd"));
                _ = command.Parameters.AddWithValue("$endDate", endDate.ToString("yyyy-MM-dd"));

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int totalItems = await reader.IsDBNullAsync(0) ? 0 : reader.GetInt32(0);
                    int successfulItems = await reader.IsDBNullAsync(1) ? 0 : reader.GetInt32(1);
                    long totalBytes = await reader.IsDBNullAsync(2) ? 0 : reader.GetInt64(2);
                    int totalAmount = await reader.IsDBNullAsync(3) ? 0 : reader.GetInt32(3);
                    return (totalItems, successfulItems, totalBytes, totalAmount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get copy history stats");
            }
            return (0, 0, 0, 0);
        }
    }
}
