using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Services;
using System;
using System.Globalization;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for the OS Image details flyout, providing properties for display dates, name, and status information.
    /// </summary>
    public partial class OsImageDetailsViewModel : ObservableObject
    {
        private readonly IFileSystemService _fileSystemService;

        /// <summary>
        /// Gets or sets the name of the OS image file or directory.
        /// </summary>
        [ObservableProperty]
        public partial string ImageName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the formatted creation date string.
        /// </summary>
        [ObservableProperty]
        public partial string DateCreatedFormatted { get; set; } = "N/A";

        /// <summary>
        /// Gets or sets the formatted last modification date string.
        /// </summary>
        [ObservableProperty]
        public partial string DateModifiedFormatted { get; set; } = "N/A";

        /// <summary>
        /// Gets or sets the formatted last access date string.
        /// </summary>
        [ObservableProperty]
        public partial string DateAccessedFormatted { get; set; } = "N/A";

        /// <summary>
        /// Gets or sets the status message displayed when loading details.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the status message panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsStatusVisible { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OsImageDetailsViewModel"/> class.
        /// </summary>
        /// <param name="fileSystemService">The file system service used for path metadata inspection.</param>
        public OsImageDetailsViewModel(IFileSystemService fileSystemService)
        {
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        }

        /// <summary>
        /// Initializes the ViewModel details by reading file/directory metadata from the specified file system path.
        /// </summary>
        /// <param name="name">The display name of the OS image item.</param>
        /// <param name="path">The file system path to inspect for metadata.</param>
        public void Initialize(string name, string path)
        {
            ImageName = name;

            (DateTime? created, DateTime? modified, DateTime? accessed) = _fileSystemService.GetPathMetadata(path);

            if (!created.HasValue && !modified.HasValue && !accessed.HasValue)
            {
                StatusMessage = "File or folder not found";
                IsStatusVisible = true;
                DateCreatedFormatted = "N/A";
                DateModifiedFormatted = "N/A";
                DateAccessedFormatted = "N/A";
                return;
            }

            IsStatusVisible = false;
            StatusMessage = string.Empty;
            const string dateFormat = "dd/MM/yyyy hh:mm tt";

            DateCreatedFormatted = created.HasValue
                ? created.Value.ToString(dateFormat, CultureInfo.InvariantCulture)
                : "N/A";

            DateModifiedFormatted = modified.HasValue
                ? modified.Value.ToString(dateFormat, CultureInfo.InvariantCulture)
                : "N/A";

            DateAccessedFormatted = accessed.HasValue
                ? accessed.Value.ToString(dateFormat, CultureInfo.InvariantCulture)
                : "N/A";
        }
    }
}
