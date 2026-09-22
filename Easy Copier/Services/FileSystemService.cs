using Easy_Copier.Infrastructure;
using System;
using System.IO;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IFileSystemService"/> delegating to standard .NET System.IO APIs.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        /// <inheritdoc />
        public bool DirectoryExists(string? path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }

        /// <inheritdoc />
        public bool FileExists(string? path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        /// <inheritdoc />
        public string[] GetDirectories(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            return Directory.GetDirectories(path);
        }

        /// <inheritdoc />
        public string[] GetFiles(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            return Directory.GetFiles(path);
        }

        /// <inheritdoc />
        public long CalculateDirectorySize(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            DirectoryInfo di = new(path);
            return FileSystemHelpers.CalculateDirectorySize(di);
        }

        /// <inheritdoc />
        public (DateTime? Created, DateTime? Modified, DateTime? Accessed) GetPathMetadata(string path)
        {
            if (FileExists(path))
            {
                FileInfo fi = new(path);
                return (fi.CreationTime, fi.LastWriteTime, fi.LastAccessTime);
            }

            if (DirectoryExists(path))
            {
                DirectoryInfo di = new(path);
                return (di.CreationTime, di.LastWriteTime, di.LastAccessTime);
            }

            return (null, null, null);
        }
    }
}
