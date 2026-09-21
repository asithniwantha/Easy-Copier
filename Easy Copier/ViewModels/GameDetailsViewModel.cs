using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel managing folder contents listing and folder size calculations for selected library items.
    /// Uses <see cref="IFileSystemService"/> for abstracted filesystem operations.
    /// </summary>
    public partial class GameDetailsViewModel(
        IDispatcherService dispatcherService,
        IFileSystemService fileSystemService) : ObservableObject
    {
        private readonly IDispatcherService _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        private readonly IFileSystemService _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));

        /// <summary>
        /// Gets the collection of child file system items (files and subdirectories) inside the target folder.
        /// </summary>
        public ObservableCollection<FileSystemItem> FolderContents { get; } = [];

        [ObservableProperty]
        public partial string FolderStatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsFolderStatusVisible { get; set; }

        /// <summary>
        /// Asynchronously loads the directory contents for the specified folder path.
        /// </summary>
        /// <param name="folderPath">The path of the folder to inspect.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadFolderContentsAsync(string folderPath)
        {
            FolderContents.Clear();
            IsFolderStatusVisible = false;

            try
            {
                FileSystemMetadata metadata = _fileSystemService.GetMetadata(folderPath);
                if (!metadata.Exists || !metadata.IsDirectory)
                {
                    FolderStatusMessage = "Folder not found";
                    IsFolderStatusVisible = true;
                    return;
                }

                (IReadOnlyList<string> dirs, IReadOnlyList<string> files) = _fileSystemService.GetFolderContents(folderPath);

                foreach (string dir in dirs)
                {
                    FileSystemItem item = new(dir, true);
                    FolderContents.Add(item);
                    _ = CalculateFolderSizeAsync(item);
                }

                foreach (string file in files)
                {
                    FolderContents.Add(new FileSystemItem(file, false));
                }

                if (FolderContents.Count == 0)
                {
                    FolderStatusMessage = "Empty folder";
                    IsFolderStatusVisible = true;
                }
            }
            catch (Exception ex)
            {
                FolderStatusMessage = $"Error loading folder: {ex.Message}";
                IsFolderStatusVisible = true;
            }

            await Task.CompletedTask;
        }

        private async Task CalculateFolderSizeAsync(FileSystemItem item)
        {
            if (!item.IsFolder)
            {
                return;
            }

            try
            {
                long size = await _fileSystemService.CalculateDirectorySizeAsync(item.Path);
                _ = _dispatcherService.TryEnqueue(() =>
                {
                    item.SizeFormatted = FormattingHelpers.FormatBytes(size);
                });
            }
            catch
            {
                _ = _dispatcherService.TryEnqueue(() =>
                {
                    item.SizeFormatted = "Unknown";
                });
            }
        }
    }
}
