using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly IAppWindowContext _appWindowContext;
        private readonly ILogger<FolderPickerService> _logger;

        public FolderPickerService(
            IDispatcherService dispatcherService,
            IAppWindowContext appWindowContext,
            ILogger<FolderPickerService>? logger = null)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _appWindowContext = appWindowContext ?? throw new ArgumentNullException(nameof(appWindowContext));
            _logger = logger ?? NullLogger<FolderPickerService>.Instance;
        }

        public async Task<string?> PickFolderAsync()
        {
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
                        _logger.LogError("Unable to retrieve a valid window handle for FolderPicker. Folder picker cannot be initialized.");
                        tcs.SetResult(null);
                        return;
                    }

                    FolderPicker folderPicker = new()
                    {
                        SuggestedStartLocation = PickerLocationId.ComputerFolder,
                        ViewMode = PickerViewMode.List
                    };

                    folderPicker.FileTypeFilter.Add("*");

                    InitializeWithWindow.Initialize(folderPicker, windowHandle);

                    StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                    tcs.SetResult(folder?.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while showing FolderPicker.");
                    tcs.SetException(ex);
                }
            });

            if (!enqueued)
            {
                _logger.LogError("Failed to enqueue FolderPicker display on UI thread dispatcher.");
                tcs.SetResult(null);
            }

            return await tcs.Task;
        }
    }
}
