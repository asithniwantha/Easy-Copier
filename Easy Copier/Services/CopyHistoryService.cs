using Easy_Copier.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface ICopyHistoryService
    {
        Task LogAsync(CopyHistoryEntry entry);
        Task<IReadOnlyList<CopyHistoryEntry>> GetByMonthAsync(int year, int month);
        Task<IReadOnlyList<CopyHistoryEntry>> GetAllAsync();
        Task<IReadOnlyList<(int Year, int Month)>> GetAvailableMonthsAsync();
        Task DeleteAllAsync();
    }

    public class CopyHistoryService : ICopyHistoryService
    {
        private readonly string _dbPath;

        public CopyHistoryService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "EasyCopier");
            Directory.CreateDirectory(dir);
            _dbPath = Path.Combine(dir, "history.db");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS CopyHistory (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    ItemName     TEXT    NOT NULL,
                    Category     INTEGER NOT NULL,
                    SourcePath   TEXT    NOT NULL,
                    DestPath     TEXT    NOT NULL,
                    TargetDrive  TEXT    NOT NULL,
                    BytesCopied  INTEGER NOT NULL,
                    Success      INTEGER NOT NULL,
                    ErrorMessage TEXT,
                    CopiedAt     TEXT    NOT NULL
                )
                """;
            cmd.ExecuteNonQuery();
        }

        public async Task LogAsync(CopyHistoryEntry entry)
        {
            await Task.Run(() =>
            {
                using var conn = CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO CopyHistory
                        (ItemName, Category, SourcePath, DestPath, TargetDrive, BytesCopied, Success, ErrorMessage, CopiedAt)
                    VALUES
                        ($name, $cat, $src, $dest, $drive, $bytes, $ok, $err, $at)
                    """;
                cmd.Parameters.AddWithValue("$name",  entry.ItemName);
                cmd.Parameters.AddWithValue("$cat",   (int)entry.Category);
                cmd.Parameters.AddWithValue("$src",   entry.SourcePath);
                cmd.Parameters.AddWithValue("$dest",  entry.DestinationPath);
                cmd.Parameters.AddWithValue("$drive", entry.TargetDrive);
                cmd.Parameters.AddWithValue("$bytes", entry.BytesCopied);
                cmd.Parameters.AddWithValue("$ok",    entry.Success ? 1 : 0);
                cmd.Parameters.AddWithValue("$err",   (object?)entry.ErrorMessage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$at",    entry.CopiedAt.ToString("o"));
                cmd.ExecuteNonQuery();
            });
        }

        public async Task<IReadOnlyList<CopyHistoryEntry>> GetByMonthAsync(int year, int month)
        {
            var from = new DateTime(year, month, 1).ToString("o");
            var to   = new DateTime(year, month, 1).AddMonths(1).ToString("o");
            return await QueryAsync($"WHERE CopiedAt >= '{from}' AND CopiedAt < '{to}'");
        }

        public async Task<IReadOnlyList<CopyHistoryEntry>> GetAllAsync() =>
            await QueryAsync(string.Empty);

        public async Task<IReadOnlyList<(int Year, int Month)>> GetAvailableMonthsAsync()
        {
            return await Task.Run(() =>
            {
                var months = new List<(int, int)>();
                using var conn = CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    SELECT DISTINCT
                        CAST(strftime('%Y', CopiedAt) AS INTEGER) AS Y,
                        CAST(strftime('%m', CopiedAt) AS INTEGER) AS M
                    FROM CopyHistory
                    ORDER BY Y DESC, M DESC
                    """;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    months.Add((reader.GetInt32(0), reader.GetInt32(1)));
                return (IReadOnlyList<(int, int)>)months;
            });
        }

        public async Task DeleteAllAsync()
        {
            await Task.Run(() =>
            {
                using var conn = CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM CopyHistory";
                cmd.ExecuteNonQuery();
            });
        }

        private async Task<IReadOnlyList<CopyHistoryEntry>> QueryAsync(string whereClause)
        {
            return await Task.Run(() =>
            {
                var results = new List<CopyHistoryEntry>();
                using var conn = CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT Id, ItemName, Category, SourcePath, DestPath, TargetDrive, BytesCopied, Success, ErrorMessage, CopiedAt FROM CopyHistory {whereClause} ORDER BY CopiedAt DESC";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new CopyHistoryEntry
                    {
                        Id              = reader.GetInt32(0),
                        ItemName        = reader.GetString(1),
                        Category        = (LibraryCategory)reader.GetInt32(2),
                        SourcePath      = reader.GetString(3),
                        DestinationPath = reader.GetString(4),
                        TargetDrive     = reader.GetString(5),
                        BytesCopied     = reader.GetInt64(6),
                        Success         = reader.GetInt32(7) == 1,
                        ErrorMessage    = reader.IsDBNull(8) ? null : reader.GetString(8),
                        CopiedAt        = DateTime.Parse(reader.GetString(9))
                    });
                }
                return (IReadOnlyList<CopyHistoryEntry>)results;
            });
        }

        private SqliteConnection CreateConnection() => new($"Data Source={_dbPath}");
    }
}
