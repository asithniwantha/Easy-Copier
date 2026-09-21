using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Services;
using System;
using System.Globalization;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing details, formatting metadata, and timestamps for OS image items.
    /// Uses <see cref="IFileSystemService"/> for file system metadata extraction.
    /// </summary>
    public partial class OsImageDetailsViewModel(IFileSystemService fileSystemService) : ObservableObject
    {
        private readonly IFileSystemService _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));

        /// <summary>
        /// Initializes a new instance of the <see cref="OsImageDetailsViewModel"/> class with a default <see cref="FileSystemService"/>.
        /// </summary>
        public OsImageDetailsViewModel()
            : this(new FileSystemService())
        {
        }

        [ObservableProperty]
        public partial string ImageName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string DateCreatedFormatted { get; set; } = "N/A";

        [ObservableProperty]
        public partial string DateModifiedFormatted { get; set; } = "N/A";

        [ObservableProperty]
        public partial string DateAccessedFormatted { get; set; } = "N/A";

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsStatusVisible { get; set; }

        /// <summary>
        /// Initializes the ViewModel with item name and file system path to retrieve metadata timestamps.
        /// </summary>
        /// <param name="name">The display name of the image item.</param>
        /// <param name="path">The file system path to inspect.</param>
        public void Initialize(string name, string path)
        {
            ImageName = name;

            FileSystemMetadata metadata = _fileSystemService.GetMetadata(path);

            if (!metadata.Exists)
            {
                StatusMessage = "File or folder not found";
                IsStatusVisible = true;
                return;
            }

            const string dateFormat = "dd/MM/yyyy hh:mm tt";

            if (metadata.CreationTime.HasValue)
            {
                DateCreatedFormatted = metadata.CreationTime.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
            }

            if (metadata.LastWriteTime.HasValue)
            {
                DateModifiedFormatted = metadata.LastWriteTime.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
            }

            if (metadata.LastAccessTime.HasValue)
            {
                DateAccessedFormatted = metadata.LastAccessTime.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
            }
        }
    }
}
