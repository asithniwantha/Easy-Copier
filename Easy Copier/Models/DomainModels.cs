using System;
using System.Collections.Generic;

namespace Easy_Copier.Models
{
    public record SourceFolder(string FolderPath, bool IsValid, DateTime LastScanned);

    public record GameEntry(
        string Name,
        string FolderPath,
        long TotalBytes,
        string? CoverImagePath,
        DateTime DateAdded,
        bool HasLargeFiles)
    {
        public bool HasCover => !string.IsNullOrEmpty(CoverImagePath);
        public bool HasNoCover => !HasCover;
    }

    public record RemovableDrive(
        string DriveLetter,
        string DriveLabel,
        string FileSystem,
        long TotalBytes,
        long FreeBytes,
        double UsedPercentage,
        string Brand = "Unknown")
    {
        public bool IsFat32 => FileSystem.Equals("FAT32", StringComparison.OrdinalIgnoreCase);
        public const long Fat32MaxFileSize = 4L * 1024 * 1024 * 1024; // 4GB
    }

    public record ValidationResult(
        bool IsValid,
        ValidationSeverity Severity,
        string Message);

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public record TransferRequest(
        IReadOnlyList<GameEntry> Games,
        RemovableDrive TargetDrive,
        string DestinationPath);

    public record TransferOutcome(
        bool Success,
        string Message,
        int FilesTransferred,
        long BytesTransferred,
        DateTime CompletedAt);

    public class AppSettings
    {
        public List<string> SourceFolders { get; set; } = new();
        public bool AutoScanOnStartup { get; set; } = true;
        public string LastSelectedDrive { get; set; } = string.Empty;
        public DateTime LastScanTime { get; set; } = DateTime.MinValue;
    }
}
