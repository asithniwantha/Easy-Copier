using Easy_Copier.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task AddRecordAsync(SmartAdderHistoryRecord record);
        Task<List<SmartAdderHistoryRecord>> GetRecentRecordsAsync(int count);
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly ILogger<DatabaseService> _logger;
        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localAppData, "EasyCopier");
            _ = Directory.CreateDirectory(appFolder);
            _dbPath = Path.Combine(appFolder, "history.db"); // Use the same database, or another one if preferred.
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
                    CREATE TABLE IF NOT EXISTS History (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        Entries TEXT NOT NULL,
                        TotalSum REAL NOT NULL
                    )";

                _ = await command.ExecuteNonQueryAsync();

                _logger.LogInformation("History table initialized at {Path}", _dbPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize History table at {Path}", _dbPath);
            }
        }

        public async Task AddRecordAsync(SmartAdderHistoryRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO History (Timestamp, Entries, TotalSum)
                    VALUES ($timestamp, $entries, $totalSum)";

                _ = command.Parameters.AddWithValue("$timestamp", record.Timestamp.ToString("O"));
                _ = command.Parameters.AddWithValue("$entries", record.EntriesJson);
                _ = command.Parameters.AddWithValue("$totalSum", record.TotalSum);

                _ = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add History record with total {Total}", record.TotalSum);
            }
        }

        public async Task<List<SmartAdderHistoryRecord>> GetRecentRecordsAsync(int count)
        {
            List<SmartAdderHistoryRecord> records = [];
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, Timestamp, Entries, TotalSum
                    FROM History
                    ORDER BY Id DESC
                    LIMIT $count";
                _ = command.Parameters.AddWithValue("$count", count);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string timestampStr = reader.GetString(1);
                    if (!DateTime.TryParse(timestampStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime timestamp))
                    {
                        _logger.LogWarning("Failed to parse Timestamp '{TimestampStr}' in History", timestampStr);
                        continue;
                    }

                    records.Add(new SmartAdderHistoryRecord
                    {
                        Id = reader.GetInt32(0),
                        Timestamp = timestamp,
                        EntriesJson = await reader.IsDBNullAsync(2) ? "[]" : reader.GetString(2),
                        TotalSum = await reader.IsDBNullAsync(3) ? 0 : reader.GetDouble(3)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent History records");
            }

            return records;
        }
    }
}
