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
        private readonly IDispatcherService _dispatcherService;

        public FilePickerService(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        }

        public async Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices)
        {
            ArgumentNullException.ThrowIfNull(fileTypeChoices);

            TaskCompletionSource<string?> tcs = new();

            bool enqueued = _dispatcherService.TryEnqueue(async () =>
            {
                try
                {
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
                        tcs.SetResult(null);
                        return;
                    }

                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    tcs.SetResult(file?.Path);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            if (!enqueued)
            {
                tcs.SetResult(null);
            }

            return await tcs.Task;
        }

        private static IntPtr GetActiveWindowHandle()
        {
            return App.MainWindow != null ? WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow) : nint.Zero;
        }
    }
}
