using Easy_Copier.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IFileSystemService"/> executing disk operations using <see cref="System.IO"/>.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        /// <inheritdoc />
        public (IReadOnlyList<string> Directories, IReadOnlyList<string> Files) GetFolderContents(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return (Array.Empty<string>(), Array.Empty<string>());
            }

            try
            {
                IReadOnlyList<string> dirs = Directory.GetDirectories(folderPath).OrderBy(d => d).ToList();
                IReadOnlyList<string> files = Directory.GetFiles(folderPath).OrderBy(f => f).ToList();
                return (dirs, files);
            }
            catch
            {
                return (Array.Empty<string>(), Array.Empty<string>());
            }
        }

        /// <inheritdoc />
        public Task<long> CalculateDirectorySizeAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return Task.FromResult(0L);
            }

            return Task.Run(() =>
            {
                try
                {
                    DirectoryInfo dirInfo = new(folderPath);
                    return FileSystemHelpers.CalculateDirectorySize(dirInfo);
                }
                catch
                {
                    return 0L;
                }
            });
        }

        /// <inheritdoc />
        public FileSystemMetadata GetMetadata(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new FileSystemMetadata(false, false, null, null, null);
            }

            if (File.Exists(path))
            {
                FileInfo fileInfo = new(path);
                return new FileSystemMetadata(
                    Exists: true,
                    IsDirectory: false,
                    CreationTime: fileInfo.CreationTime,
                    LastWriteTime: fileInfo.LastWriteTime,
                    LastAccessTime: fileInfo.LastAccessTime);
            }

            if (Directory.Exists(path))
            {
                DirectoryInfo dirInfo = new(path);
                return new FileSystemMetadata(
                    Exists: true,
                    IsDirectory: true,
                    CreationTime: dirInfo.CreationTime,
                    LastWriteTime: dirInfo.LastWriteTime,
                    LastAccessTime: dirInfo.LastAccessTime);
            }

            return new FileSystemMetadata(false, false, null, null, null);
        }
    }
}
