using System;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines file system inspection and metadata operations, abstracting direct disk I/O calls from ViewModels.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        /// <returns><see langword="true"/> if path refers to an existing directory; otherwise, <see langword="false"/>.</returns>
        bool DirectoryExists(string? path);

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns><see langword="true"/> if path refers to an existing file; otherwise, <see langword="false"/>.</returns>
        bool FileExists(string? path);

        /// <summary>
        /// Returns the names of subdirectories in the specified directory.
        /// </summary>
        /// <param name="path">The relative or absolute path to the directory to search.</param>
        /// <returns>An array of the full names (including paths) for subdirectories in the specified directory.</returns>
        string[] GetDirectories(string path);

        /// <summary>
        /// Returns the names of files in the specified directory.
        /// </summary>
        /// <param name="path">The relative or absolute path to the directory to search.</param>
        /// <returns>An array of the full names (including paths) for files in the specified directory.</returns>
        string[] GetFiles(string path);

        /// <summary>
        /// Calculates the total size in bytes of all files contained within the target directory recursively.
        /// </summary>
        /// <param name="path">The full path of the target directory.</param>
        /// <returns>The accumulated directory size in bytes.</returns>
        long CalculateDirectorySize(string path);

        /// <summary>
        /// Retrieves creation, modification, and access timestamps for the target file or directory path.
        /// </summary>
        /// <param name="path">The file or directory path to inspect.</param>
        /// <returns>A tuple containing optional creation, modification, and access timestamps.</returns>
        (DateTime? Created, DateTime? Modified, DateTime? Accessed) GetPathMetadata(string path);
    }
}
