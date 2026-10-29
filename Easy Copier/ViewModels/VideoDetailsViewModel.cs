using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Represents a single file listed in the video details flyout.
    /// </summary>
    /// <param name="Name">The file path relative to the video folder.</param>
    /// <param name="SizeFormatted">The human-readable file size.</param>
    /// <param name="IsLarge">True when the file exceeds the FAT32 4GB limit.</param>
    public sealed record VideoFileInfo(string Name, string SizeFormatted, bool IsLarge);

    /// <summary>
    /// ViewModel for the film/TV details flyout, exposing the folder path, file list and total size.
    /// </summary>
    public partial class VideoDetailsViewModel : ObservableObject
    {
        /// <summary>
        /// FAT32 maximum file size (4GB - 1 byte); larger files cannot be copied to FAT32 drives.
        /// </summary>
        private const long Fat32MaxFileBytes = 4_294_967_295;

        /// <summary>
        /// Gets or sets the display title of the film or series.
        /// </summary>
        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full folder path.
        /// </summary>
        [ObservableProperty]
        public partial string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the summary line (file count and total size).
        /// </summary>
        [ObservableProperty]
        public partial string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status message shown when the folder cannot be read.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the status message is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsStatusVisible { get; set; }

        /// <summary>
        /// Gets the files contained in the video folder.
        /// </summary>
        public ObservableCollection<VideoFileInfo> Files { get; } = [];

        /// <summary>
        /// Loads the file list for the given folder off the UI thread.
        /// </summary>
        /// <param name="title">The display title.</param>
        /// <param name="folderPath">The folder to inspect; may be malformed or missing.</param>
        public async Task InitializeAsync(string title, string folderPath)
        {
            Title = title;
            FolderPath = folderPath;
            Files.Clear();

            // Enumeration runs in the background and failures surface as a status message, never an exception.
            (List<VideoFileInfo> files, long total, string? error) = await Task.Run(() => LoadFiles(folderPath));

            if (error != null)
            {
                StatusMessage = error;
                IsStatusVisible = true;
                Summary = string.Empty;
                return;
            }

            IsStatusVisible = false;
            foreach (VideoFileInfo file in files)
            {
                Files.Add(file);
            }

            Summary = $"{files.Count} file(s) • {FormattingHelpers.FormatBytes(total)}";
        }

        /// <summary>
        /// Enumerates files recursively, skipping unreadable entries.
        /// </summary>
        private static (List<VideoFileInfo> Files, long Total, string? Error) LoadFiles(string folderPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                {
                    return ([], 0, "Folder not found");
                }

                EnumerationOptions options = new() { RecurseSubdirectories = true, IgnoreInaccessible = true };
                List<FileInfo> infos = [.. new DirectoryInfo(folderPath).EnumerateFiles("*", options)];
                List<VideoFileInfo> files = [.. infos
                    .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                    .Select(f => new VideoFileInfo(
                        Path.GetRelativePath(folderPath, f.FullName),
                        FormattingHelpers.FormatBytes(f.Length),
                        f.Length > Fat32MaxFileBytes))];

                return (files, infos.Sum(f => f.Length), null);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return ([], 0, $"Unable to read folder: {ex.Message}");
            }
        }
    }
}
