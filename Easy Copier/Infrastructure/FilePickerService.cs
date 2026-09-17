using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Easy_Copier.Infrastructure
{
    public interface IFilePickerService
    {
        Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices);
    }

    public class FilePickerService : IFilePickerService
    {
        private readonly IDispatcherService _dispatcherService;
        private readonly IAppWindowContext _appWindowContext;
        private readonly ILogger<FilePickerService> _logger;

        public FilePickerService(
            IDispatcherService dispatcherService,
            IAppWindowContext appWindowContext,
            ILogger<FilePickerService>? logger = null)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _appWindowContext = appWindowContext ?? throw new ArgumentNullException(nameof(appWindowContext));
            _logger = logger ?? NullLogger<FilePickerService>.Instance;
        }

        public async Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices)
        {
            ArgumentNullException.ThrowIfNull(fileTypeChoices);

            TaskCompletionSource<string?> tcs = new();

            bool enqueued = _dispatcherService.TryEnqueue(async () =>
            {
                try
                {
                    nint windowHandle = NativeWindowHelper.GetActiveWindowHandle();
                    if (windowHandle == IntPtr.Zero)
                    {
                        _logger.LogWarning("NativeWindowHelper.GetActiveWindowHandle() returned IntPtr.Zero (likely elevated or inactive window). Attempting fallback to AppWindowContext.MainWindow handle.");
                        if (_appWindowContext.MainWindow is Microsoft.UI.Xaml.Window mainWindow)
                        {
                            windowHandle = WindowNative.GetWindowHandle(mainWindow);
                        }
                    }

                    if (windowHandle == IntPtr.Zero)
                    {
                        _logger.LogError("Unable to retrieve a valid window handle for FileSavePicker. File save picker cannot be initialized.");
                        tcs.SetResult(null);
                        return;
                    }

                    FileSavePicker savePicker = new()
                    {
                        SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                        SuggestedFileName = suggestedFileName
                    };

                    foreach (KeyValuePair<string, IList<string>> kvp in fileTypeChoices)
                    {
                        savePicker.FileTypeChoices.Add(kvp.Key, kvp.Value);
                    }

                    InitializeWithWindow.Initialize(savePicker, windowHandle);

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    tcs.SetResult(file?.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while showing FileSavePicker.");
                    tcs.SetException(ex);
                }
            });

            if (!enqueued)
            {
                _logger.LogError("Failed to enqueue FileSavePicker display on UI thread dispatcher.");
                tcs.SetResult(null);
            }

            return await tcs.Task;
        }
    }
}
