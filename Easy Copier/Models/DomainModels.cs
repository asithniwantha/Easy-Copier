using System;
using System.Collections.Generic;

namespace Easy_Copier.Models
{
    /// <summary>
    /// Represents a monitored source folder for library content scanning.
    /// </summary>
    /// <param name="FolderPath">The full path to the source directory.</param>
    /// <param name="IsValid">Indicates whether the folder exists and is accessible.</param>
    /// <param name="LastScanned">The timestamp when the folder was last scanned for items.</param>
    public record SourceFolder(string FolderPath, bool IsValid, DateTime LastScanned);

    /// <summary>
    /// Represents genres/categories for classifying games.
    /// </summary>
    public enum GameCategory
    {
        /// <summary>All game categories or unfiltered view.</summary>
        All,

        /// <summary>Uncategorized games without a specific genre assignment.</summary>
        Uncategorized,

        /// <summary>Shooter games (first-person, third-person, etc.).</summary>
        Shooter,

        /// <summary>Racing games.</summary>
        Racing,

        /// <summary>Role-playing games (RPGs).</summary>
        RPG,

        /// <summary>Strategy games.</summary>
        Strategy,

        /// <summary>Action-adventure and exploration games.</summary>
        Adventure,

        /// <summary>Simulation games.</summary>
        Simulation,

        /// <summary>Sports and athletic games.</summary>
        Sports,

        /// <summary>Puzzle and brain-teaser games.</summary>
        Puzzle,

        /// <summary>Horror and survival horror games.</summary>
        Horror,

        /// <summary>Platformer and side-scroller games.</summary>
        Platformer
    }

    /// <summary>
    /// Represents the high-level library type for media and software items.
    /// </summary>
    public enum LibraryCategory
    {
        /// <summary>Game title items.</summary>
        Game,

        /// <summary>Application and software installation items.</summary>
        App,

        /// <summary>TV shows, movies, and video media items.</summary>
        TvAndFilm,

        /// <summary>OS disk images and ISO items.</summary>
        OsImage
    }

    /// <summary>
    /// Defines sort options available for OS image items.
    /// </summary>
    public enum OsImageSortOption
    {
        /// <summary>Sort by name alphabetically.</summary>
        Name,

        /// <summary>Sort by creation or release date.</summary>
        DateCreated,

        /// <summary>Sort by file size on disk.</summary>
        Size
    }

    /// <summary>
    /// Represents a single scanned library entry (game, app, TV/film, or OS image).
    /// </summary>
    /// <param name="Name">The display name of the item.</param>
    /// <param name="FolderPath">The full path to the item directory or file.</param>
    /// <param name="TotalBytes">The total size of the item in bytes.</param>
    /// <param name="CoverImagePath">The path to the local cover/thumbnail image, if available.</param>
    /// <param name="DateCreated">The creation timestamp of the item folder or file.</param>
    /// <param name="HasLargeFiles">Indicates whether the item contains files exceeding 4 GB (FAT32 limitation).</param>
    /// <param name="Category">The primary library category for this entry.</param>
    /// <param name="Categories">An optional list of associated game categories/genres.</param>
    public record GameEntry(
        string Name,
        string FolderPath,
        long TotalBytes,
        string? CoverImagePath,
        DateTime DateCreated,
        bool HasLargeFiles,
        LibraryCategory Category = LibraryCategory.Game,
        IReadOnlyList<GameCategory>? Categories = null)
    {
        /// <summary>
        /// Gets a value indicating whether this entry has an associated cover image path.
        /// </summary>
        public bool HasCover => !string.IsNullOrEmpty(CoverImagePath);

        /// <summary>
        /// Gets a value indicating whether this entry does not have an associated cover image.
        /// </summary>
        public bool HasNoCover => !HasCover;
    }

    /// <summary>
    /// Represents information about a detected removable drive or storage volume.
    /// </summary>
    /// <param name="DriveLetter">The drive letter (e.g., "E:").</param>
    /// <param name="DriveLabel">The volume label of the drive.</param>
    /// <param name="FileSystem">The file system name (e.g., "FAT32", "NTFS", "exFAT").</param>
    /// <param name="TotalBytes">The total capacity of the drive in bytes.</param>
    /// <param name="FreeBytes">The available free space on the drive in bytes.</param>
    /// <param name="UsedPercentage">The percentage of used drive space (0.0 to 100.0).</param>
    /// <param name="Brand">The detected brand or manufacturer name of the drive.</param>
    public record RemovableDrive(
        string DriveLetter,
        string DriveLabel,
        string FileSystem,
        long TotalBytes,
        long FreeBytes,
        double UsedPercentage,
        string Brand = "Unknown")
    {
        /// <summary>
        /// Gets a value indicating whether the drive uses the FAT32 file system.
        /// </summary>
        public bool IsFat32 => FileSystem.Equals("FAT32", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Represents the maximum single file size allowed on FAT32 file systems (4 GB).
        /// </summary>
        public const long Fat32MaxFileSize = 4L * 1024 * 1024 * 1024; // 4GB
    }

    /// <summary>
    /// Represents the result of a validation operation, including whether it is valid, the severity of the validation, and an associated message.
    /// </summary>
    /// <param name="IsValid">Indicates whether the validation passed without critical errors.</param>
    /// <param name="Severity">The severity level of the validation outcome.</param>
    /// <param name="Message">A descriptive validation message explaining the result.</param>
    public record ValidationResult(
        bool IsValid,
        ValidationSeverity Severity,
        string Message);

    /// <summary>
    /// Represents the severity level of a validation result.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Informational validation message.</summary>
        Info,

        /// <summary>Warning condition that may require user attention.</summary>
        Warning,

        /// <summary>Error condition that prevents the operation from proceeding.</summary>
        Error
    }

    /// <summary>
    /// Specifies the conflict resolution action for file transfer operations.
    /// </summary>
    public enum CopyAction
    {
        /// <summary>Use default conflict behavior.</summary>
        Default,

        /// <summary>Replace existing files in the target directory.</summary>
        Replace,

        /// <summary>Merge content with the existing target directory.</summary>
        Merge,

        /// <summary>Skip copying items that already exist in the target directory.</summary>
        Skip
    }

    /// <summary>
    /// Represents an individual item in a transfer batch along with its assigned copy action.
    /// </summary>
    /// <param name="Game">The library entry to be copied.</param>
    /// <param name="Action">The conflict resolution action to take if destination files exist.</param>
    public record TransferItem(
        GameEntry Game,
        CopyAction Action = CopyAction.Default);

    /// <summary>
    /// Represents the progress of an active file copy or transfer operation.
    /// </summary>
    /// <param name="Percentage">The overall percentage completed (0.0 to 100.0).</param>
    /// <param name="SpeedText">The formatted current transfer speed string (e.g., "45.2 MB/s").</param>
    /// <param name="RemainingTimeText">The formatted estimated remaining time string (e.g., "02:15").</param>
    public record TransferProgress(double Percentage, string SpeedText, string RemainingTimeText);

    /// <summary>
    /// Represents a request to transfer a batch of items to a designated removable drive.
    /// </summary>
    /// <param name="Items">The list of transfer items to be copied.</param>
    /// <param name="TargetDrive">The destination drive information.</param>
    /// <param name="DestinationPath">The target directory path on the target drive.</param>
    public record TransferRequest(
        IReadOnlyList<TransferItem> Items,
        RemovableDrive TargetDrive,
        string DestinationPath);

    /// <summary>
    /// Represents the final outcome of a completed transfer batch.
    /// </summary>
    /// <param name="Success">Indicates whether all requested items transferred successfully.</param>
    /// <param name="Message">A summary message describing the outcome.</param>
    /// <param name="FilesTransferred">The total number of files transferred.</param>
    /// <param name="BytesTransferred">The total number of bytes transferred.</param>
    /// <param name="CompletedAt">The completion timestamp.</param>
    public record TransferOutcome(
        bool Success,
        string Message,
        int FilesTransferred,
        long BytesTransferred,
        DateTime CompletedAt);

    /// <summary>
    /// Holds application configuration and user preference settings.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Gets or sets the list of source directory paths for game libraries.</summary>
        public List<string> GameSourceFolders { get; set; } = [];

        /// <summary>Gets or sets the list of source directory paths for app libraries.</summary>
        public List<string> AppSourceFolders { get; set; } = [];

        /// <summary>Gets or sets the list of source directory paths for TV and film libraries.</summary>
        public List<string> TvAndFilmSourceFolders { get; set; } = [];

        /// <summary>Gets or sets the list of source directory paths for OS image libraries.</summary>
        public List<string> OsImageSourceFolders { get; set; } = [];

        /// <summary>Gets or sets the file path to the Rufus executable for bootable USB creation.</summary>
        public string RufusExecutablePath { get; set; } = @"%USERPROFILE%\Downloads\Programs\rufus.exe";

        /// <summary>Gets or sets comma-separated video file extension filters.</summary>
        public string VideoFileExtensions { get; set; } = ".mp4,.mkv,.avi";

        /// <summary>Gets or sets a value indicating whether to auto-scan library folders at application startup.</summary>
        public bool AutoScanOnStartup { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether the application should launch automatically at Windows logon.</summary>
        public bool StartOnLogon { get; set; }

        /// <summary>Gets or sets a value indicating whether updates should be downloaded automatically when available.</summary>
        public bool AutoDownloadUpdates { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether sound effects play on completion/failure events.</summary>
        public bool PlayNotificationSounds { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether Windows toast notifications are enabled.</summary>
        public bool ShowDesktopNotifications { get; set; } = true;

        /// <summary>Gets or sets the drive letter of the last selected destination drive.</summary>
        public string LastSelectedDrive { get; set; } = string.Empty;

        /// <summary>Gets or sets the timestamp of the last library scan.</summary>
        public DateTime LastScanTime { get; set; } = DateTime.MinValue;

        /// <summary>Gets or sets the base price for tier 1 content (e.g., &lt; 5 GB).</summary>
        public int PriceTier1 { get; set; } = 100;

        /// <summary>Gets or sets the price for tier 2 content (e.g., 5-10 GB).</summary>
        public int PriceTier2 { get; set; } = 200;

        /// <summary>Gets or sets the price for tier 3 content (e.g., 10-16 GB).</summary>
        public int PriceTier3 { get; set; } = 300;

        /// <summary>Gets or sets the price for tier 4 content (e.g., &gt; 16 GB).</summary>
        public int PriceTier4 { get; set; } = 400;
    }

    /// <summary>
    /// Represents a unique file system item state fingerprint used for cache validity checks.
    /// </summary>
    /// <param name="RelativePath">The relative path of the item from the source root.</param>
    /// <param name="TotalBytes">The size of the item in bytes.</param>
    /// <param name="LastWriteTimeUtc">The UTC timestamp of the last modification.</param>
    public record ItemFingerprint(
        string RelativePath,
        long TotalBytes,
        DateTime LastWriteTimeUtc);

    /// <summary>
    /// Represents a serialized snapshot of cached library entries and metadata.
    /// </summary>
    /// <param name="SchemaVersion">The schema version of the cache format.</param>
    /// <param name="Games">The cached list of game entries.</param>
    /// <param name="Apps">The cached list of app entries.</param>
    /// <param name="TvAndFilms">The cached list of TV/film entries.</param>
    /// <param name="OsImages">The cached list of OS image entries.</param>
    /// <param name="GameSourceFolders">The configured game source folders at cache creation.</param>
    /// <param name="AppSourceFolders">The configured app source folders at cache creation.</param>
    /// <param name="TvAndFilmSourceFolders">The configured TV/film source folders at cache creation.</param>
    /// <param name="OsImageSourceFolders">The configured OS image source folders at cache creation.</param>
    /// <param name="CachedAt">The timestamp when this cache snapshot was created.</param>
    /// <param name="ItemFingerprints">A dictionary mapping relative paths to item fingerprints for change detection.</param>
    public record LibraryCacheSnapshot(
        int SchemaVersion,
        List<GameEntry> Games,
        List<GameEntry> Apps,
        List<GameEntry> TvAndFilms,
        List<GameEntry> OsImages,
        List<string> GameSourceFolders,
        List<string> AppSourceFolders,
        List<string> TvAndFilmSourceFolders,
        List<string> OsImageSourceFolders,
        DateTime CachedAt,
        Dictionary<string, ItemFingerprint> ItemFingerprints)
    {
        /// <summary>
        /// The current schema version constant for library cache validation.
        /// </summary>
        public const int CurrentSchemaVersion = 4;
    }

    /// <summary>
    /// Specifies the validation result status when verifying a library cache snapshot.
    /// </summary>
    public enum CacheValidationResult
    {
        /// <summary>Cache is fully valid and up-to-date.</summary>
        Valid,

        /// <summary>Cache configuration does not match current source folder settings.</summary>
        ConfigurationMismatch,

        /// <summary>One or more source folders are unavailable or unreachable.</summary>
        SourcesUnavailable,

        /// <summary>Library items have been modified, added, or deleted since cache creation.</summary>
        ItemsChanged,

        /// <summary>No existing cache snapshot was found.</summary>
        CacheNotFound,

        /// <summary>The cache file is corrupt or unparseable.</summary>
        CorruptOrInvalid
    }

    /// <summary>
    /// Holds the detailed outcome of a cache validation check.
    /// </summary>
    /// <param name="Result">The status result of the cache validation.</param>
    /// <param name="ChangedItems">The list of item paths that changed since the cache was created.</param>
    public record CacheValidationOutcome(
        CacheValidationResult Result,
        List<string> ChangedItems);

    /// <summary>
    /// Represents a persistent database history entry recording a completed transfer.
    /// </summary>
    /// <param name="Id">The auto-increment database primary key ID.</param>
    /// <param name="Timestamp">The date and time when the transfer was logged.</param>
    /// <param name="GameName">The name of the item or summary of items transferred.</param>
    /// <param name="TargetDriveLetter">The destination drive letter.</param>
    /// <param name="TargetDriveLabel">The destination drive label.</param>
    /// <param name="BytesTransferred">The total volume of data transferred in bytes.</param>
    /// <param name="IsSuccess">Indicates whether the transfer completed successfully.</param>
    /// <param name="Amount">The price charged or calculated for the transfer.</param>
    public record CopyHistoryRecord(
        int Id,
        DateTime Timestamp,
        string GameName,
        string TargetDriveLetter,
        string TargetDriveLabel,
        long BytesTransferred,
        bool IsSuccess,
        int Amount)
    {
        /// <summary>
        /// Gets or sets the total batch price amount for grouped transfer operations.
        /// </summary>
        public int BatchAmount { get; set; }
    }
}
