using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface IFolderPickerService
    {
        Task<string?> PickFolderAsync();
    }

    public interface ISourceLibraryService
    {
        Task<IReadOnlyList<SourceFolder>> ValidateSourceFoldersAsync(IEnumerable<string> folderPaths);
        Task<bool> FolderExistsAsync(string folderPath);
        Task<string> GetSystemRequirementsAsync(string folderPath);
    }

    public class SourceLibraryService(ILogger<SourceLibraryService> logger) : ISourceLibraryService
    {
        private readonly ILogger<SourceLibraryService> _logger = logger;

        public async Task<IReadOnlyList<SourceFolder>> ValidateSourceFoldersAsync(IEnumerable<string> folderPaths)
        {
            ArgumentNullException.ThrowIfNull(folderPaths);

            var tasks = folderPaths.Select(async path =>
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

        public async Task<bool> FolderExistsAsync(string folderPath)
        {
            return await Task.Run(() => Directory.Exists(folderPath));
        }

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
