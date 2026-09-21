using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for displaying detailed folder contents and status information of a game entry.
    /// </summary>
    public partial class GameDetailsViewModel : ObservableObject
    {
        private readonly IDispatcherService _dispatcherService;

        /// <summary>
        /// Gets the collection of file system items contained in the target game folder.
        /// </summary>
        public ObservableCollection<FileSystemItem> FolderContents { get; } = [];

        // Partial properties used for [ObservableProperty] to ensure CsWinRT/AOT compatibility (MVVMTK0045)
        /// <summary>
        /// Gets or sets the folder status message displayed when loading or when folder errors occur.
        /// </summary>
        [ObservableProperty]
        public partial string FolderStatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the folder status message panel is visible.
        /// </summary>
        [ObservableProperty]
        public partial bool IsFolderStatusVisible { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDetailsViewModel"/> class.
        /// </summary>
        /// <param name="dispatcherService">The dispatcher service for marshaling UI updates.</param>
        public GameDetailsViewModel(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService;
        }

        /// <summary>
        /// Asynchronously loads directory and file items from the specified folder path.
        /// </summary>
        /// <param name="folderPath">The file system folder path to read.</param>
        /// <returns>A task representing the asynchronous folder content loading operation.</returns>
        public async Task LoadFolderContentsAsync(string folderPath)
        {
            FolderContents.Clear();
            IsFolderStatusVisible = false;

            try
            {
                if (Directory.Exists(folderPath))
                {
                    IOrderedEnumerable<string> dirs = Directory.GetDirectories(folderPath).OrderBy(d => d);
                    IOrderedEnumerable<string> files = Directory.GetFiles(folderPath).OrderBy(f => f);

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
                else
                {
                    FolderStatusMessage = "Folder not found";
                    IsFolderStatusVisible = true;
                }
            }
            catch (Exception ex)
            {
                FolderStatusMessage = $"Error loading folder: {ex.Message}";
                IsFolderStatusVisible = true;
            }
        }

        private async Task CalculateFolderSizeAsync(FileSystemItem item)
        {
            if (!item.IsFolder)
            {
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    long size = FileSystemHelpers.CalculateDirectorySize(new DirectoryInfo(item.Path));
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
            });
        }
    }
}
