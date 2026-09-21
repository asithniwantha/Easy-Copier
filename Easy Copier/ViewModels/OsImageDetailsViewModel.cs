using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;
using System.IO;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for the OS Image details flyout, providing properties for display dates, name, and status information.
    /// </summary>
    public partial class OsImageDetailsViewModel : ObservableObject
    {
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
        /// Initializes the ViewModel details by reading file/directory metadata from the specified file system path.
        /// </summary>
        /// <param name="name">The display name of the OS image item.</param>
        /// <param name="path">The file system path to inspect for metadata.</param>
        public void Initialize(string name, string path)
        {
            ImageName = name;

            DateTime? created = null;
            DateTime? modified = null;
            DateTime? accessed = null;

            if (File.Exists(path))
            {
                FileInfo fi = new(path);
                created = fi.CreationTime;
                modified = fi.LastWriteTime;
                accessed = fi.LastAccessTime;
            }
            else if (Directory.Exists(path))
            {
                DirectoryInfo di = new(path);
                created = di.CreationTime;
                modified = di.LastWriteTime;
                accessed = di.LastAccessTime;
            }
            else
            {
                StatusMessage = "File or folder not found";
                IsStatusVisible = true;
            }

            const string dateFormat = "dd/MM/yyyy hh:mm tt";
            if (created.HasValue)
            {
                DateCreatedFormatted = created.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
            }

            if (modified.HasValue)
            {
                DateModifiedFormatted = modified.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
            }

            if (accessed.HasValue)
            {
                DateAccessedFormatted = accessed.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
            }
        }
    }
}
