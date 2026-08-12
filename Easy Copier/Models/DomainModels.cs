using System;
using System.Collections.Generic;

namespace Easy_Copier.Models
{
    public record SourceFolder(string FolderPath, bool IsValid, DateTime LastScanned);

    public enum LibraryCategory
    {
        Game,
        App,
        TvAndFilm
    }

    public record GameEntry(
        string Name,
        string FolderPath,
        long TotalBytes,
        string? CoverImagePath,
        DateTime DateAdded,
        bool HasLargeFiles,
        LibraryCategory Category = LibraryCategory.Game)
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
        public List<string> GameSourceFolders { get; set; } = [];
        public List<string> AppSourceFolders { get; set; } = [];
        public List<string> TvAndFilmSourceFolders { get; set; } = [];
        public string VideoFileExtensions { get; set; } = ".mp4,.mkv,.avi";
        public bool AutoScanOnStartup { get; set; } = true;
        public string LastSelectedDrive { get; set; } = string.Empty;
        public DateTime LastScanTime { get; set; } = DateTime.MinValue;
    }

    public record ItemFingerprint(
        string RelativePath,
        long TotalBytes,
        DateTime LastWriteTimeUtc);

    public record LibraryCacheSnapshot(
        int SchemaVersion,
        List<GameEntry> Games,
        List<GameEntry> Apps,
        List<GameEntry> TvAndFilms,
        List<string> GameSourceFolders,
        List<string> AppSourceFolders,
        List<string> TvAndFilmSourceFolders,
        DateTime CachedAt,
        Dictionary<string, ItemFingerprint> ItemFingerprints)
    {
        public const int CurrentSchemaVersion = 1;
    }

    public enum CacheValidationResult
    {
        Valid,
        ConfigurationMismatch,
        SourcesUnavailable,
        ItemsChanged,
        CacheNotFound,
        CorruptOrInvalid
    }

    public record CacheValidationOutcome(
        CacheValidationResult Result,
        List<string> ChangedItems);

    public record CopyHistoryRecord(
        int Id,
        DateTime Timestamp,
        string GameName,
        string TargetDriveLetter,
        string TargetDriveLabel,
        long BytesTransferred,
        bool IsSuccess);
}
