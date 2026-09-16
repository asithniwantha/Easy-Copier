using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;
using System.IO;

namespace Easy_Copier.ViewModels
{
    public partial class OsImageDetailsViewModel : ObservableObject
    {
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
            if (created.HasValue) DateCreatedFormatted = created.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
            if (modified.HasValue) DateModifiedFormatted = modified.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
            if (accessed.HasValue) DateAccessedFormatted = accessed.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
        }
    }
}
