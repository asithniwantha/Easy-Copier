using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Easy_Copier.Infrastructure
{
    public interface IFilePickerService
    {
        Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices, IntPtr windowHandle);
    }

    public class FilePickerService : IFilePickerService
    {
        public async Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices, IntPtr windowHandle)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = suggestedFileName
            };

            foreach (var kvp in fileTypeChoices)
            {
                savePicker.FileTypeChoices.Add(kvp.Key, kvp.Value);
            }

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

            var file = await savePicker.PickSaveFileAsync();
            return file?.Path;
        }
    }
}
