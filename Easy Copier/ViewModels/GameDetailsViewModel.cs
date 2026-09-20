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
    public partial class GameDetailsViewModel : ObservableObject
    {
        private readonly IDispatcherService _dispatcherService;

        public ObservableCollection<FileSystemItem> FolderContents { get; } = [];

        // Partial properties used for [ObservableProperty] to ensure CsWinRT/AOT compatibility (MVVMTK0045)
        [ObservableProperty]
        public partial string FolderStatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsFolderStatusVisible { get; set; }

        public GameDetailsViewModel(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService;
        }

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