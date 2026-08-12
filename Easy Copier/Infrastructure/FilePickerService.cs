using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Easy_Copier.Infrastructure
{
    public interface IFilePickerService
    {
        Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices);
    }

    public class FilePickerService : IFilePickerService
    {
        public async Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices)
        {
            ArgumentNullException.ThrowIfNull(fileTypeChoices);

            FileSavePicker savePicker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = suggestedFileName
            };

            foreach (KeyValuePair<string, IList<string>> kvp in fileTypeChoices)
            {
                savePicker.FileTypeChoices.Add(kvp.Key, kvp.Value);
            }

            nint windowHandle = GetActiveWindowHandle();
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

            StorageFile file = await savePicker.PickSaveFileAsync();
            return file?.Path;
        }

        private static IntPtr GetActiveWindowHandle()
        {
            return App.MainWindow != null ? WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow) : nint.Zero;
        }
    }
}
