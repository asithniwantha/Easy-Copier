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
        Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month);
        Task<List<(int Year, int Month)>> GetAvailableMonthsAsync();
        Task<(int TotalItems, int SuccessfulItems, long TotalBytes)> GetStatsAsync(DateTime startDate, DateTime endDate);
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
                        IsSuccess INTEGER NOT NULL
                    )";

                _ = await command.ExecuteNonQueryAsync();
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
                    INSERT INTO CopyHistory (Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess)
                    VALUES ($timestamp, $gameName, $targetDriveLetter, $targetDriveLabel, $bytesTransferred, $isSuccess)";

                // Use ISO 8601 string for reliable SQLite sorting/filtering
                _ = command.Parameters.AddWithValue("$timestamp", record.Timestamp.ToString("O"));
                _ = command.Parameters.AddWithValue("$gameName", record.GameName);
                _ = command.Parameters.AddWithValue("$targetDriveLetter", record.TargetDriveLetter);
                _ = command.Parameters.AddWithValue("$targetDriveLabel", record.TargetDriveLabel);
                _ = command.Parameters.AddWithValue("$bytesTransferred", record.BytesTransferred);
                _ = command.Parameters.AddWithValue("$isSuccess", record.IsSuccess ? 1 : 0);

                _ = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add copy history record for {GameName}", record.GameName);
            }
        }

        public async Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month)
        {
            List<CopyHistoryRecord> records = [];
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                // Format as YYYY-MM
                string prefix = $"{year:D4}-{month:D2}";

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess
                    FROM CopyHistory
                    WHERE Timestamp LIKE $prefix
                    ORDER BY Timestamp DESC";
                _ = command.Parameters.AddWithValue("$prefix", prefix + "%");

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
                        reader.GetInt32(6) == 1
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get copy history records for {Year}-{Month}", year, month);
            }
            return records;
        }

        public async Task<List<(int Year, int Month)>> GetAvailableMonthsAsync()
        {
            HashSet<(int Year, int Month)> months = [];
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                // Extract just the YYYY-MM part from the ISO8601 string
                command.CommandText = "SELECT DISTINCT substr(Timestamp, 1, 7) FROM CopyHistory ORDER BY substr(Timestamp, 1, 7) DESC";

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string yyyyMm = reader.GetString(0);
                    if (yyyyMm.Length == 7 && int.TryParse(yyyyMm.AsSpan(0, 4), out int year) && int.TryParse(yyyyMm.AsSpan(5, 2), out int month))
                    {
                        _ = months.Add((year, month));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available months from copy history");
            }
            return [.. months];
        }

        public async Task<(int TotalItems, int SuccessfulItems, long TotalBytes)> GetStatsAsync(DateTime startDate, DateTime endDate)
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
                        SUM(BytesTransferred)
                    FROM CopyHistory
                    WHERE Timestamp >= $startDate AND Timestamp < $endDate";

                _ = command.Parameters.AddWithValue("$startDate", startDate);
                _ = command.Parameters.AddWithValue("$endDate", endDate);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int totalItems = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    int successfulItems = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    long totalBytes = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);
                    return (totalItems, successfulItems, totalBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get copy history stats");
            }
            return (0, 0, 0);
        }
    }
}
