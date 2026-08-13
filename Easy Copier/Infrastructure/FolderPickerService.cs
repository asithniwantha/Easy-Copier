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
        private readonly IDispatcherService _dispatcherService;

        public FolderPickerService(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        }

        public async Task<string?> PickFolderAsync()
        {
            TaskCompletionSource<string?> tcs = new();

            bool enqueued = _dispatcherService.TryEnqueue(async () =>
            {
                try
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
                        tcs.SetResult(null);
                        return;
                    }

                    InitializeWithWindow.Initialize(folderPicker, windowHandle);

                    StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                    tcs.SetResult(folder?.Path);
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
            return App.MainWindow != null ? WindowNative.GetWindowHandle(App.MainWindow) : nint.Zero;
        }
    }
}
