using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface IReportService
    {
        Task<string> ExportToCsvAsync(IReadOnlyList<CopyHistoryEntry> entries, string filePath);
        string GetDefaultExportPath(int year, int month);
    }

    public class ReportService : IReportService
    {
        public async Task<string> ExportToCsvAsync(IReadOnlyList<CopyHistoryEntry> entries, string filePath)
        {
            await Task.Run(() =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,Name,Category,Source Path,Destination Path,Target Drive,Size,Success,Error,Copied At");

                foreach (var e in entries)
                {
                    sb.AppendLine(string.Join(",",
                        e.Id,
                        CsvEscape(e.ItemName),
                        e.CategoryLabel,
                        CsvEscape(e.SourcePath),
                        CsvEscape(e.DestinationPath),
                        CsvEscape(e.TargetDrive),
                        CsvEscape(e.SizeLabel),
                        e.Success ? "Yes" : "No",
                        CsvEscape(e.ErrorMessage ?? string.Empty),
                        e.CopiedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });

            return filePath;
        }

        public string GetDefaultExportPath(int year, int month)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dir = Path.Combine(docs, "EasyCopier", "Reports");
            return Path.Combine(dir, $"CopyReport_{year:D4}-{month:D2}.csv");
        }

        private static string CsvEscape(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
