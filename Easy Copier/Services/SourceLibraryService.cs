using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
    }

    public class SourceLibraryService : ISourceLibraryService
    {
        private readonly ILogger<SourceLibraryService> _logger;

        public SourceLibraryService(ILogger<SourceLibraryService> logger)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyList<SourceFolder>> ValidateSourceFoldersAsync(IEnumerable<string> folderPaths)
        {
            var validatedFolders = new List<SourceFolder>();

            foreach (var path in folderPaths)
            {
                try
                {
                    var exists = await Task.Run(() => Directory.Exists(path));
                    var folder = new SourceFolder(path, exists, DateTime.Now);
                    validatedFolders.Add(folder);

                    if (!exists)
                    {
                        _logger.LogWarning("Source folder not accessible: {Path}", path);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating source folder: {Path}", path);
                    validatedFolders.Add(new SourceFolder(path, false, DateTime.Now));
                }
            }

            return validatedFolders;
        }

        public async Task<bool> FolderExistsAsync(string folderPath)
        {
            return await Task.Run(() => Directory.Exists(folderPath));
        }
    }
}
