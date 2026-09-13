using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace Easy_Copier.Models
{
    public partial class FileSystemItem : ObservableObject
    {
        public string Name { get; }
        public string Path { get; }
        public bool IsFolder { get; }

        public string IconGlyph => IsFolder ? "\uE8D5" : "\uE7C3";

        [ObservableProperty]
        private string _sizeFormatted = "Calculating...";

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
                    _sizeFormatted = Easy_Copier.Infrastructure.FormattingHelpers.FormatBytes(size);
                }
                catch
                {
                    _sizeFormatted = "Unknown";
                }
            }
        }
    }
}