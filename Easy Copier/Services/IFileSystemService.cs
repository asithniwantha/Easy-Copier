using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Represents file system creation, modification, and access metadata timestamps.
    /// </summary>
    /// <param name="Exists">Indicates whether the file or directory exists.</param>
    /// <param name="IsDirectory">Indicates whether the target path is a directory.</param>
    /// <param name="CreationTime">The creation timestamp, if available.</param>
    /// <param name="LastWriteTime">The last write/modification timestamp, if available.</param>
    /// <param name="LastAccessTime">The last access timestamp, if available.</param>
    public record FileSystemMetadata(
        bool Exists,
        bool IsDirectory,
        DateTime? CreationTime,
        DateTime? LastWriteTime,
        DateTime? LastAccessTime);

    /// <summary>
    /// Defines operations for inspecting file system paths, querying directory contents, and calculating directory sizes.
    /// Abstracts direct disk operations to enhance testability.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Retrieves child directory paths and file paths located directly within the specified directory.
        /// </summary>
        /// <param name="folderPath">The path of the directory to query.</param>
        /// <returns>A tuple containing lists of child directory paths and file paths.</returns>
        (IReadOnlyList<string> Directories, IReadOnlyList<string> Files) GetFolderContents(string folderPath);

        /// <summary>
        /// Asynchronously calculates the total size in bytes of all files within a directory and its subdirectories.
        /// </summary>
        /// <param name="folderPath">The directory path to evaluate.</param>
        /// <returns>A task representing the asynchronous operation, containing the directory size in bytes.</returns>
        Task<long> CalculateDirectorySizeAsync(string folderPath);

        /// <summary>
        /// Retrieves file system metadata for the specified file or directory path.
        /// </summary>
        /// <param name="path">The file or directory path to inspect.</param>
        /// <returns>A <see cref="FileSystemMetadata"/> instance with path details.</returns>
        FileSystemMetadata GetMetadata(string path);
    }
}
