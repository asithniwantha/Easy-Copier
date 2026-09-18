using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace Easy_Copier.Models
{
    /// <summary>
    /// Represents a file system item (folder or file) for display in navigation or selection controls.
    /// </summary>
    public partial class FileSystemItem : ObservableObject
    {
        /// <summary>
        /// Gets the name of the file system item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the full path of the file system item.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets a value indicating whether this item represents a directory/folder.
        /// </summary>
        public bool IsFolder { get; }

        /// <summary>
        /// Gets the font glyph string representing the folder or file icon.
        /// </summary>
        public string IconGlyph => IsFolder ? "\uE8D5" : "\uE7C3";

        // Partial property preferred over private field for [ObservableProperty] to ensure WinRT/AOT compatibility (MVVMTK0045)
        /// <summary>
        /// Gets or sets the formatted file size string (or status message).
        /// </summary>
        [ObservableProperty]
        public partial string SizeFormatted { get; set; } = "Calculating...";

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemItem"/> class.
        /// </summary>
        /// <param name="path">The target file or folder path.</param>
        /// <param name="isFolder">Whether the path points to a directory.</param>
        public FileSystemItem(string path, bool isFolder)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
            IsFolder = isFolder;

            if (!IsFolder)
            {
                try
                {
                    long size = new FileInfo(path).Length;
                    SizeFormatted = Easy_Copier.Infrastructure.FormattingHelpers.FormatBytes(size);
                }
                catch
                {
                    SizeFormatted = "Unknown";
                }
            }
        }
    }
}
