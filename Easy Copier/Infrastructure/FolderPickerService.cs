using Easy_Copier.Services;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Easy_Copier.Infrastructure
{
    public class FolderPickerService : IFolderPickerService
    {
        public async Task<string?> PickFolderAsync()
        {
            FolderPicker folderPicker = new()
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };

            folderPicker.FileTypeFilter.Add("*");

            nint windowHandle = GetActiveWindowHandle();
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            InitializeWithWindow.Initialize(folderPicker, windowHandle);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            return folder?.Path;
        }

        private static IntPtr GetActiveWindowHandle()
        {
            return App.MainWindow != null ? WindowNative.GetWindowHandle(App.MainWindow) : nint.Zero;
        }
    }
}
