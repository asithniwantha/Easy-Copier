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
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="directoryInfo"/> is null.</exception>
        public static long CalculateDirectorySize(DirectoryInfo directoryInfo)
        {
            System.ArgumentNullException.ThrowIfNull(directoryInfo);

            long size = 0;
            try
            {
                FileInfo[] files = directoryInfo.GetFiles();
                foreach (FileInfo file in files)
                {
                    size += file.Length;
                }

                DirectoryInfo[] subDirs = directoryInfo.GetDirectories();
                foreach (DirectoryInfo dir in subDirs)
                {
                    size += CalculateDirectorySize(dir);
                }
            }
            catch
            {
                // Ignore access errors
            }
            return size;
        }
    }
}
