using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Globalization;
using System.IO;

namespace Easy_Copier.Views
{
    public sealed partial class OsImageDetailsFlyout : UserControl
    {
        public string ImageName { get; }
        public string DateCreatedFormatted { get; }
        public string DateModifiedFormatted { get; }
        public string DateAccessedFormatted { get; }
        public string StatusMessage { get; } = string.Empty;
        public Visibility IsStatusVisible { get; } = Visibility.Collapsed;

        public OsImageDetailsFlyout(string name, string path)
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
                IsStatusVisible = Visibility.Visible;
            }

            const string dateFormat = "dd/MM/yyyy hh:mm tt";
            DateCreatedFormatted = created.HasValue ? created.Value.ToString(dateFormat, CultureInfo.InvariantCulture) : "N/A";
            DateModifiedFormatted = modified.HasValue ? modified.Value.ToString(dateFormat, CultureInfo.InvariantCulture) : "N/A";
            DateAccessedFormatted = accessed.HasValue ? accessed.Value.ToString(dateFormat, CultureInfo.InvariantCulture) : "N/A";

            InitializeComponent();
        }
    }
}
