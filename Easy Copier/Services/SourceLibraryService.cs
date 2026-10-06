using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for interacting with UI folder selection picker dialogs.
    /// </summary>
    public interface IFolderPickerService
    {
        /// <summary>
        /// Prompts the user to select a folder from the operating system folder selection dialog.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the chosen directory folder path, or <see langword="null"/> if cancelled.</returns>
        Task<string?> PickFolderAsync();
    }

    /// <summary>
    /// Defines operations for validating source folder paths and reading game system requirements documents.
    /// </summary>
    public interface ISourceLibraryService
    {
        /// <summary>
        /// Asynchronously checks a list of source folder paths and returns accessibility status for each folder.
        /// </summary>
        /// <param name="folderPaths">The list of folder directory paths to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="SourceFolder"/> validation instances.</returns>
        Task<IReadOnlyList<SourceFolder>> ValidateSourceFoldersAsync(IEnumerable<string> folderPaths);

        /// <summary>
        /// Asynchronously verifies whether a directory folder path exists on disk.
        /// </summary>
        /// <param name="folderPath">The directory path to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the folder exists; otherwise, <see langword="false"/>.</returns>
        Task<bool> FolderExistsAsync(string folderPath);

        /// <summary>
        /// Reads system requirements content from <c>system_requirements.txt</c> in the specified folder if present.
        /// </summary>
        /// <param name="folderPath">The target game folder path.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the system requirements text content or status message.</returns>
        Task<string> GetSystemRequirementsAsync(string folderPath);
    }

    /// <summary>
    /// Service for validating source folder paths and reading local game metadata files.
    /// </summary>
    /// <param name="logger">The logger instance for operational diagnostic output.</param>
    public class SourceLibraryService(ILogger<SourceLibraryService> logger) : ISourceLibraryService
    {
        /// <summary>
        /// Logger instance used for diagnostic logging.
        /// </summary>
        private readonly ILogger<SourceLibraryService> _logger = logger;

        /// <inheritdoc />
        public async Task<IReadOnlyList<SourceFolder>> ValidateSourceFoldersAsync(IEnumerable<string> folderPaths)
        {
            ArgumentNullException.ThrowIfNull(folderPaths);

            IEnumerable<Task<SourceFolder>> tasks = folderPaths.Select(async path =>
            {
                try
                {
                    bool exists = await Task.Run(() => Directory.Exists(path));
                    SourceFolder folder = new(path, exists, DateTime.Now);

                    if (!exists)
                    {
                        _logger.LogWarning("Source folder not accessible: {Path}", path);
                    }
                    return folder;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating source folder: {Path}", path);
                    return new SourceFolder(path, false, DateTime.Now);
                }
            });

            SourceFolder[] validatedFolders = await Task.WhenAll(tasks);
            return validatedFolders;
        }

        /// <inheritdoc />
        public async Task<bool> FolderExistsAsync(string folderPath)
        {
            return await Task.Run(() => Directory.Exists(folderPath));
        }

        /// <inheritdoc />
        public async Task<string> GetSystemRequirementsAsync(string folderPath)
        {
            return await Task.Run(() =>
            {
                string reqFilePath = Path.Combine(folderPath, "system_requirements.txt");

                if (File.Exists(reqFilePath))
                {
                    try
                    {
                        return File.ReadAllText(reqFilePath);
                    }
                    catch (Exception)
                    {
                        return "Error reading system requirements.";
                    }
                }
                else
                {
                    return "System requirements aren't available.";
                }
            });
        }
    }
}
