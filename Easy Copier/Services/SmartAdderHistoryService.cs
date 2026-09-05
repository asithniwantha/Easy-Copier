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
    public interface ISmartAdderHistoryService
    {
        Task InitializeAsync();
        Task AddRecordAsync(SmartAdderHistoryRecord record);
        Task<List<SmartAdderHistoryRecord>> GetRecentRecordsAsync(int count);
    }

    public class SmartAdderHistoryService : ISmartAdderHistoryService
    {
        private readonly ILogger<SmartAdderHistoryService> _logger;
        private readonly string _dbPath;
        private readonly string _connectionString;

        public SmartAdderHistoryService(ILogger<SmartAdderHistoryService> logger)
        {
            _logger = logger;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "EasyCopier");
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
                    CREATE TABLE IF NOT EXISTS SmartAdderHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        EntriesJson TEXT NOT NULL,
                        Total REAL NOT NULL
                    )";

                _ = await command.ExecuteNonQueryAsync();

                _logger.LogInformation("SmartAdderHistory table initialized at {Path}", _dbPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SmartAdderHistory table at {Path}", _dbPath);
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
                    INSERT INTO SmartAdderHistory (Timestamp, EntriesJson, Total)
                    VALUES ($timestamp, $entriesJson, $total)";

                _ = command.Parameters.AddWithValue("$timestamp", record.Timestamp.ToString("O"));
                _ = command.Parameters.AddWithValue("$entriesJson", record.EntriesJson);
                _ = command.Parameters.AddWithValue("$total", record.Total);

                _ = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add SmartAdder history record with total {Total}", record.Total);
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
                    SELECT Id, Timestamp, EntriesJson, Total
                    FROM SmartAdderHistory
                    ORDER BY Id DESC
                    LIMIT $count";
                _ = command.Parameters.AddWithValue("$count", count);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string timestampStr = reader.GetString(1);
                    if (!DateTime.TryParse(timestampStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime timestamp))
                    {
                        _logger.LogWarning("Failed to parse Timestamp '{TimestampStr}' in SmartAdderHistory", timestampStr);
                        continue;
                    }

                    records.Add(new SmartAdderHistoryRecord
                    {
                        Id = reader.GetInt32(0),
                        Timestamp = timestamp,
                        EntriesJson = await reader.IsDBNullAsync(2) ? "[]" : reader.GetString(2),
                        Total = await reader.IsDBNullAsync(3) ? 0 : reader.GetDouble(3)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent SmartAdder history records");
            }

            return records;
        }
    }
}
