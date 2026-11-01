using System;
using System.IO;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides filesystem calculation and helper utilities.
    /// </summary>
    public static class FileSystemHelpers
    {
        /// <summary>
        /// Recursively calculates the total byte size of a directory and all of its subdirectories.
        /// </summary>
        /// <param name="directoryInfo">The directory to measure.</param>
        /// <returns>The total size in bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryInfo"/> is <c>null</c>.</exception>
        public static long CalculateDirectorySize(DirectoryInfo directoryInfo)
        {
            ArgumentNullException.ThrowIfNull(directoryInfo);

            long size = 0;
            System.Collections.Generic.Queue<DirectoryInfo> queue = new();
            queue.Enqueue(directoryInfo);

            const int maxDirectories = 100_000;
            int directoryCount = 0;

            while (queue.Count > 0 && directoryCount < maxDirectories)
            {
                DirectoryInfo current = queue.Dequeue();
                directoryCount++;

                try
                {
                    foreach (FileInfo file in current.EnumerateFiles())
                    {
                        size += file.Length;
                    }

                    foreach (DirectoryInfo subDir in current.EnumerateDirectories())
                    {
                        // Exclude reparse points (symlinks, junctions) to avoid circular reference loops.
                        if ((subDir.Attributes & FileAttributes.ReparsePoint) == 0)
                        {
                            queue.Enqueue(subDir);
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore access-denied or transient I/O exceptions on individual directories.
                }
            }

            return size;
        }
    }
}
