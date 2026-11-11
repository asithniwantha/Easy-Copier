using Easy_Copier.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of matching <see cref="CopyHistoryRecord"/> instances ordered descending by timestamp.</returns>
        Task<List<CopyHistoryRecord>> GetRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek);

        /// <summary>
        /// Retrieves distinct start dates of weeks for which history records exist.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of Sunday start dates ordered descending.</returns>
        Task<List<DateTime>> GetAvailableWeeksAsync();

        /// <summary>
        /// Retrieves all copy history records for a specified year and month.
        /// </summary>
        /// <param name="year">The 4-digit year to filter by.</param>
        /// <param name="month">The month (1–12) to filter by.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of matching <see cref="CopyHistoryRecord"/> instances ordered descending by timestamp.</returns>
        Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month);

        /// <summary>
        /// Asynchronously retrieves distinct year and month pairs for which history records exist.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of tuples containing (<c>Year</c>, <c>Month</c>) ordered descending.</returns>
        Task<List<(int Year, int Month)>> GetAvailableMonthsAsync();

        /// <summary>
        /// Calculates aggregated statistics for history records within a specified date range.
        /// </summary>
        /// <param name="startDate">The start boundary of the date range (inclusive).</param>
        /// <param name="endDate">The end boundary of the date range (exclusive).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple containing total items (<c>TotalItems</c>), successful items (<c>SuccessfulItems</c>), total bytes (<c>TotalBytes</c>), and total monetary amount (<c>TotalAmount</c>).</returns>
        Task<(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount)> GetStatsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves a paginated slice of copy history records matching the specified filters, ordered by timestamp descending.
        /// Intent: Enables scalable UI history browsing without loading hundreds of thousands of records into memory at once.
        /// </summary>
        Task<PagedResult<CopyHistoryRecord>> GetPagedRecordsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isSuccess = null,
            string? searchPattern = null,
            int page = 1,
            int pageSize = 50,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates aggregated summary statistics for history records matching the specified filters directly in SQLite.
        /// Intent: Avoids pulling records into memory solely to compute totals and success rates.
        /// </summary>
        Task<(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount)> GetFilteredStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isSuccess = null,
            string? searchPattern = null,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all copy history records matching the specified filters without pagination, used for CSV export.
        /// </summary>
        Task<List<CopyHistoryRecord>> GetAllFilteredRecordsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isSuccess = null,
            string? searchPattern = null,
            System.Threading.CancellationToken cancellationToken = default);
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
                        Amount INTEGER NOT NULL DEFAULT 0,
                        SourcePath TEXT NOT NULL DEFAULT '',
                        DestinationPath TEXT NOT NULL DEFAULT '',
                        ErrorLog TEXT NOT NULL DEFAULT '',
                        SubFilesJson TEXT NOT NULL DEFAULT ''
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

                // Schema migration for details
                command.CommandText = "PRAGMA table_info(CopyHistory)";
                bool hasDetails = false;
                using (SqliteDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.GetString(1).Equals("SourcePath", StringComparison.OrdinalIgnoreCase))
                        {
                            hasDetails = true;
                            break;
                        }
                    }
                }
                if (!hasDetails)
                {
                    command.CommandText = "ALTER TABLE CopyHistory ADD COLUMN SourcePath TEXT NOT NULL DEFAULT ''";
                    _ = await command.ExecuteNonQueryAsync();
                    command.CommandText = "ALTER TABLE CopyHistory ADD COLUMN DestinationPath TEXT NOT NULL DEFAULT ''";
                    _ = await command.ExecuteNonQueryAsync();
                    command.CommandText = "ALTER TABLE CopyHistory ADD COLUMN ErrorLog TEXT NOT NULL DEFAULT ''";
                    _ = await command.ExecuteNonQueryAsync();
                    command.CommandText = "ALTER TABLE CopyHistory ADD COLUMN SubFilesJson TEXT NOT NULL DEFAULT ''";
                    _ = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Added details columns to CopyHistory table via schema migration.");
                }

                // Create index on Timestamp for fast range queries and dashboard aggregation.
                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_copyhistory_timestamp ON CopyHistory (Timestamp DESC);";
                _ = await command.ExecuteNonQueryAsync();

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
                    INSERT INTO CopyHistory (Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess, Amount, SourcePath, DestinationPath, ErrorLog, SubFilesJson)
                    VALUES ($timestamp, $gameName, $targetDriveLetter, $targetDriveLabel, $bytesTransferred, $isSuccess, $amount, $sourcePath, $destinationPath, $errorLog, $subFilesJson)";

                // Use ISO 8601 string for reliable SQLite sorting/filtering
                _ = command.Parameters.AddWithValue("$timestamp", record.Timestamp.ToString("O"));
                _ = command.Parameters.AddWithValue("$gameName", record.GameName);
                _ = command.Parameters.AddWithValue("$targetDriveLetter", record.TargetDriveLetter);
                _ = command.Parameters.AddWithValue("$targetDriveLabel", record.TargetDriveLabel);
                _ = command.Parameters.AddWithValue("$bytesTransferred", record.BytesTransferred);
                _ = command.Parameters.AddWithValue("$isSuccess", record.IsSuccess ? 1 : 0);
                _ = command.Parameters.AddWithValue("$amount", record.Amount);
                _ = command.Parameters.AddWithValue("$sourcePath", record.SourcePath);
                _ = command.Parameters.AddWithValue("$destinationPath", record.DestinationPath);
                _ = command.Parameters.AddWithValue("$errorLog", record.ErrorLog);
                _ = command.Parameters.AddWithValue("$subFilesJson", record.SubFilesJson);

                _ = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add copy history record for {GameName}", record.GameName);
            }
        }

        /// <summary>
        /// Executes a parameterized query against CopyHistory, populating records using an index-optimized SQL query.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Internal helper receiving compile-time constant where-clause fragments with parameterized values.")]
        private async Task<List<CopyHistoryRecord>> QueryRecordsInternalAsync(string? whereClause = null, Action<SqliteCommand>? parameterBinder = null)
        {
            List<CopyHistoryRecord> records = [];
            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = whereClause != null
                    ? $"SELECT Id, Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess, Amount, SourcePath, DestinationPath, ErrorLog, SubFilesJson FROM CopyHistory WHERE {whereClause} ORDER BY Timestamp DESC"
                    : "SELECT Id, Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess, Amount, SourcePath, DestinationPath, ErrorLog, SubFilesJson FROM CopyHistory ORDER BY Timestamp DESC";

                parameterBinder?.Invoke(command);

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
                            await reader.IsDBNullAsync(7) ? 0 : reader.GetInt32(7),
                            await reader.IsDBNullAsync(8) ? string.Empty : reader.GetString(8),
                            await reader.IsDBNullAsync(9) ? string.Empty : reader.GetString(9),
                            await reader.IsDBNullAsync(10) ? string.Empty : reader.GetString(10),
                            await reader.IsDBNullAsync(11) ? string.Empty : reader.GetString(11)
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
                _logger.LogError(ex, "Failed to execute query on copy history records");
            }
            return records;
        }

        /// <summary>
        /// Helper method that reads all history records stored in the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of all <see cref="CopyHistoryRecord"/> items.</returns>
        private async Task<List<CopyHistoryRecord>> GetAllRecordsAsync()
        {
            return await QueryRecordsInternalAsync();
        }

        /// <summary>
        /// Retrieves records matching the designated week date range, sorted by timestamp descending using SQL filtering.
        /// </summary>
        /// <param name="startOfWeek">The start date of the week.</param>
        /// <param name="endOfWeek">The end date of the week.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of matching <see cref="CopyHistoryRecord"/> items.</returns>
        public async Task<List<CopyHistoryRecord>> GetRecordsByWeekAsync(DateTime startOfWeek, DateTime endOfWeek)
        {
            DateTime start = startOfWeek.Date;
            DateTime end = endOfWeek.Date.AddDays(1); // Make it exclusive

            return await QueryRecordsInternalAsync(
                "Timestamp >= $start AND Timestamp < $end",
                cmd =>
                {
                    _ = cmd.Parameters.AddWithValue("$start", start.ToString("O"));
                    _ = cmd.Parameters.AddWithValue("$end", end.ToString("O"));
                });
        }

        /// <summary>
        /// Asynchronously determines all distinct week start dates present in the history database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of week start dates ordered descending.</returns>
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
        /// Retrieves history records matching the specified year and month using SQL range query.
        /// </summary>
        /// <param name="year">The target year.</param>
        /// <param name="month">The target month (1–12).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of matching <see cref="CopyHistoryRecord"/> items.</returns>
        public async Task<List<CopyHistoryRecord>> GetRecordsByMonthAsync(int year, int month)
        {
            DateTime start = new(year, month, 1);
            DateTime end = start.AddMonths(1);

            return await QueryRecordsInternalAsync(
                "Timestamp >= $start AND Timestamp < $end",
                cmd =>
                {
                    _ = cmd.Parameters.AddWithValue("$start", start.ToString("O"));
                    _ = cmd.Parameters.AddWithValue("$end", end.ToString("O"));
                });
        }

        /// <summary>
        /// Asynchronously determines all distinct year/month combinations present in the history database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of (Year, Month) tuples ordered descending.</returns>
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
        /// Asynchronously computes summary statistics for copy operations within the specified date range using SQL aggregation.
        /// </summary>
        /// <param name="startDate">Start boundary date (inclusive).</param>
        /// <param name="endDate">End boundary date (exclusive).</param>
        /// <returns>A tuple containing total items, successful items, total bytes, and total amount.</returns>
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
                        COALESCE(SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END), 0),
                        COALESCE(SUM(BytesTransferred), 0),
                        COALESCE(SUM(Amount), 0)
                    FROM CopyHistory 
                    WHERE Timestamp >= $start AND Timestamp < $end";

                _ = command.Parameters.AddWithValue("$start", startDate.ToString("O"));
                _ = command.Parameters.AddWithValue("$end", endDate.ToString("O"));

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int totalItems = reader.GetInt32(0);
                    int successfulItems = reader.GetInt32(1);
                    long totalBytes = reader.GetInt64(2);
                    int totalAmount = reader.GetInt32(3);
                    return (totalItems, successfulItems, totalBytes, totalAmount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute stats for date range {Start} - {End}", startDate, endDate);
            }

            return (0, 0, 0L, 0);
        }

        /// <summary>
        /// Builds parameterized WHERE clauses and parameters from filter criteria.
        /// </summary>
        private static (string WhereClause, List<SqliteParameter> Parameters) BuildWhereClause(
            DateTime? startDate,
            DateTime? endDate,
            bool? isSuccess,
            string? searchPattern)
        {
            List<string> conditions = [];
            List<SqliteParameter> parameters = [];

            if (startDate.HasValue)
            {
                conditions.Add("Timestamp >= $startDate");
                parameters.Add(new SqliteParameter("$startDate", startDate.Value.ToString("O")));
            }

            if (endDate.HasValue)
            {
                conditions.Add("Timestamp < $endDate");
                parameters.Add(new SqliteParameter("$endDate", endDate.Value.ToString("O")));
            }

            if (isSuccess.HasValue)
            {
                conditions.Add("IsSuccess = $isSuccess");
                parameters.Add(new SqliteParameter("$isSuccess", isSuccess.Value ? 1 : 0));
            }

            if (!string.IsNullOrWhiteSpace(searchPattern))
            {
                conditions.Add("(GameName LIKE $search OR TargetDriveLetter LIKE $search OR TargetDriveLabel LIKE $search)");
                parameters.Add(new SqliteParameter("$search", $"%{searchPattern.Trim()}%"));
            }

            string whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (whereClause, parameters);
        }

        /// <summary>
        /// Parses a <see cref="CopyHistoryRecord"/> from the current row of a data reader.
        /// </summary>
        private static CopyHistoryRecord? ParseRecord(SqliteDataReader reader)
        {
            string timestampStr = reader.GetString(1);
            return DateTime.TryParse(timestampStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp)
                ? new CopyHistoryRecord(
                    reader.GetInt32(0),
                    timestamp,
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetInt64(5),
                    reader.GetInt32(6) == 1,
                    reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                )
                : null;
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Parameterized query using internally constructed WHERE clauses with parameter objects.")]
        public async Task<PagedResult<CopyHistoryRecord>> GetPagedRecordsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isSuccess = null,
            string? searchPattern = null,
            int page = 1,
            int pageSize = 50,
            System.Threading.CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);
            int offset = (page - 1) * pageSize;

            (string? whereClause, List<SqliteParameter>? parameters) = BuildWhereClause(startDate, endDate, isSuccess, searchPattern);

            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync(cancellationToken);

                int totalCount = 0;
                using (SqliteCommand countCmd = connection.CreateCommand())
                {
                    countCmd.CommandText = $"SELECT COUNT(*) FROM CopyHistory {whereClause}";
                    foreach (SqliteParameter p in parameters)
                    {
                        _ = countCmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                    }
                    object? scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
                    if (scalar is long countLong)
                    {
                        totalCount = (int)countLong;
                    }
                    else if (scalar is int countInt)
                    {
                        totalCount = countInt;
                    }
                }

                List<CopyHistoryRecord> records = [];
                using (SqliteCommand queryCmd = connection.CreateCommand())
                {
                    queryCmd.CommandText = $@"
                        SELECT Id, Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess, Amount, SourcePath, DestinationPath, ErrorLog, SubFilesJson
                        FROM CopyHistory
                        {whereClause}
                        ORDER BY Timestamp DESC
                        LIMIT $pageSize OFFSET $offset";

                    foreach (SqliteParameter p in parameters)
                    {
                        _ = queryCmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                    }
                    _ = queryCmd.Parameters.AddWithValue("$pageSize", pageSize);
                    _ = queryCmd.Parameters.AddWithValue("$offset", offset);

                    using SqliteDataReader reader = await queryCmd.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        CopyHistoryRecord? rec = ParseRecord(reader);
                        if (rec != null)
                        {
                            records.Add(rec);
                        }
                    }
                }

                return new PagedResult<CopyHistoryRecord>(records, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch paginated copy history records (page {Page}, size {Size})", page, pageSize);
                return new PagedResult<CopyHistoryRecord>([], 0, page, pageSize);
            }
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Parameterized query using internally constructed WHERE clauses with parameter objects.")]
        public async Task<(int TotalItems, int SuccessfulItems, long TotalBytes, int TotalAmount)> GetFilteredStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isSuccess = null,
            string? searchPattern = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            (string? whereClause, List<SqliteParameter>? parameters) = BuildWhereClause(startDate, endDate, isSuccess, searchPattern);

            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT 
                        COUNT(*), 
                        COALESCE(SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END), 0),
                        COALESCE(SUM(CASE WHEN IsSuccess = 1 THEN BytesTransferred ELSE 0 END), 0),
                        COALESCE(SUM(CASE WHEN IsSuccess = 1 THEN Amount ELSE 0 END), 0)
                    FROM CopyHistory
                    {whereClause}";

                foreach (SqliteParameter p in parameters)
                {
                    _ = command.Parameters.AddWithValue(p.ParameterName, p.Value);
                }

                using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    return (
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetInt64(2),
                        reader.GetInt32(3));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute filtered history stats");
            }

            return (0, 0, 0L, 0);
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Parameterized query using internally constructed WHERE clauses with parameter objects.")]
        public async Task<List<CopyHistoryRecord>> GetAllFilteredRecordsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isSuccess = null,
            string? searchPattern = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            (string? whereClause, List<SqliteParameter>? parameters) = BuildWhereClause(startDate, endDate, isSuccess, searchPattern);
            List<CopyHistoryRecord> records = [];

            try
            {
                using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT Id, Timestamp, GameName, TargetDriveLetter, TargetDriveLabel, BytesTransferred, IsSuccess, Amount, SourcePath, DestinationPath, ErrorLog, SubFilesJson
                    FROM CopyHistory
                    {whereClause}
                    ORDER BY Timestamp DESC";

                foreach (SqliteParameter p in parameters)
                {
                    _ = command.Parameters.AddWithValue(p.ParameterName, p.Value);
                }

                using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    CopyHistoryRecord? rec = ParseRecord(reader);
                    if (rec != null)
                    {
                        records.Add(rec);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch all filtered history records");
            }

            return records;
        }
    }
}
