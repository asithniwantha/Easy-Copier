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
    }

    public class CopyHistoryService : ICopyHistoryService
    {
        private readonly ILogger<CopyHistoryService> _logger;
        private readonly string _dbPath;
        private readonly string _connectionString;

        public CopyHistoryService(ILogger<CopyHistoryService> logger)
        {
            _logger = logger;
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "EasyCopier");
            Directory.CreateDirectory(appFolder);
            _dbPath = Path.Combine(appFolder, "history.db");
            _connectionString = $"Data Source={_dbPath}";
        }

        public async Task InitializeAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
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

                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("CopyHistory DB initialized at {Path}", _dbPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize CopyHistory DB at {Path}", _dbPath);
            }
        }

        public async Task AddRecordAsync(CopyHistoryRecord record)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO CopyHistory (Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess)
                    VALUES ($timestamp, $gameName, $targetDriveLetter, $targetDriveLabel, $bytesTransferred, $isSuccess)";

                // Use ISO 8601 string for reliable SQLite sorting/filtering
                command.Parameters.AddWithValue("$timestamp", record.Timestamp.ToString("O"));
                command.Parameters.AddWithValue("$gameName", record.GameName);
                command.Parameters.AddWithValue("$targetDriveLetter", record.TargetDriveLetter);
                command.Parameters.AddWithValue("$targetDriveLabel", record.TargetDriveLabel);
                command.Parameters.AddWithValue("$bytesTransferred", record.BytesTransferred);
                command.Parameters.AddWithValue("$isSuccess", record.IsSuccess ? 1 : 0);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add copy history record for {GameName}", record.GameName);
            }
        }

        public async Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month)
        {
            var records = new List<CopyHistoryRecord>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Format as YYYY-MM
                var prefix = $"{year:D4}-{month:D2}";

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess
                    FROM CopyHistory
                    WHERE Timestamp LIKE $prefix
                    ORDER BY Timestamp DESC";
                command.Parameters.AddWithValue("$prefix", prefix + "%");

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new CopyHistoryRecord(
                        reader.GetInt32(0),
                        DateTime.Parse(reader.GetString(1)),
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
            var months = new HashSet<(int Year, int Month)>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                // Extract just the YYYY-MM part from the ISO8601 string
                command.CommandText = "SELECT DISTINCT substr(Timestamp, 1, 7) FROM CopyHistory ORDER BY Timestamp DESC";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var yyyyMm = reader.GetString(0);
                    if (yyyyMm.Length == 7 && int.TryParse(yyyyMm.Substring(0, 4), out int year) && int.TryParse(yyyyMm.Substring(5, 2), out int month))
                    {
                        months.Add((year, month));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available months from copy history");
            }
            return new List<(int Year, int Month)>(months);
        }
    }
}
