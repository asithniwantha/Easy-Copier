using Easy_Copier.Services;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Easy_Copier.Infrastructure
{
    public class FolderPickerService : IFolderPickerService
    {
        public async Task<string?> PickFolderAsync()
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };

            folderPicker.FileTypeFilter.Add("*");

            var windowHandle = GetActiveWindowHandle();
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            InitializeWithWindow.Initialize(folderPicker, windowHandle);

            var folder = await folderPicker.PickSingleFolderAsync();
            return folder?.Path;
        }

        private IntPtr GetActiveWindowHandle()
        {
            if (App.MainWindow != null)
            {
                return WindowNative.GetWindowHandle(App.MainWindow);
            }

            return IntPtr.Zero;
        }
    }
}
