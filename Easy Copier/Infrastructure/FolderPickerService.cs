using Easy_Copier.Services;
using Microsoft.UI.Xaml;
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
            var windows = Application.Current?.GetType()
                .GetField("_window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(Application.Current) as Window;

            if (windows != null)
            {
                return WindowNative.GetWindowHandle(windows);
            }

            return IntPtr.Zero;
        }
    }
}
