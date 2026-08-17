using System.IO;

namespace Easy_Copier.Infrastructure
{
    public static class FileSystemHelpers
    {
        public static long CalculateDirectorySize(DirectoryInfo directoryInfo)
        {
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
