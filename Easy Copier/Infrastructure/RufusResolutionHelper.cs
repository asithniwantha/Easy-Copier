using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides utilities for discovering and resolving Rufus executable paths and version information.
    /// </summary>
    public static partial class RufusResolutionHelper
    {
        // Regex matches filenames like rufus-4.7.exe, rufus-4.10.exe, rufus_4.8_arm64.exe, rufus-4.7p.exe, etc.
        [GeneratedRegex(@"rufus[^\d]*(?<version>\d+(?:\.\d+)+)", RegexOptions.IgnoreCase)]
        private static partial Regex RufusVersionRegex();

        /// <summary>
        /// Attempts to parse a <see cref="Version"/> object from a Rufus filename (e.g., rufus-4.7.exe => Version 4.7).
        /// </summary>
        /// <param name="fileName">The file name or path to parse.</param>
        /// <returns>A parsed <see cref="Version"/> object if successful; otherwise, <c>null</c>.</returns>
        public static Version? TryParseVersionFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            Match match = RufusVersionRegex().Match(fileName);
            if (match.Success)
            {
                string versionStr = match.Groups["version"].Value;
                if (Version.TryParse(versionStr, out Version? parsedVersion))
                {
                    return parsedVersion;
                }
            }

            return null;
        }

        /// <summary>
        /// Given a configured Rufus path (with optional environment variables), expands environment variables,
        /// scans the directory for all rufus*.exe files with valid version numbers, and returns the path to the
        /// executable with the highest version. If no newer versioned file exists or directory is invalid, returns
        /// the expanded current path.
        /// </summary>
        /// <param name="currentRufusPath">The initial path or environment string for Rufus.</param>
        /// <returns>The resolved executable path for the latest Rufus version found, or empty string if input is invalid.</returns>
        public static string ResolveLatestRufusPath(string? currentRufusPath)
        {
            if (string.IsNullOrWhiteSpace(currentRufusPath))
            {
                return string.Empty;
            }

            string expandedPath = Environment.ExpandEnvironmentVariables(currentRufusPath);
            string? directory = Path.GetDirectoryName(expandedPath);

            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return expandedPath;
            }

            try
            {
                DirectoryInfo dirInfo = new(directory);
                FileInfo[] files = dirInfo.GetFiles("rufus*.exe", SearchOption.TopDirectoryOnly);

                var candidates = files
                    .Select(f => new
                    {
                        FileInfo = f,
                        Version = TryParseVersionFromFileName(f.Name)
                    })
                    .Where(c => c.Version != null)
                    .OrderByDescending(c => c.Version)
                    .ThenByDescending(c => c.FileInfo.LastWriteTimeUtc)
                    .ToList();

                if (candidates.Count > 0)
                {
                    return candidates[0].FileInfo.FullName;
                }
            }
            catch
            {
                // Fall back to expandedPath on any directory access error
            }

            return expandedPath;
        }
    }
}
