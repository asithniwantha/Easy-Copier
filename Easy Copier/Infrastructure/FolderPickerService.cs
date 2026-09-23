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
    /// <summary>
    /// Implements folder selection operations using WinRT <see cref="FolderPicker"/>.
    /// </summary>
    public class FolderPickerService : IFolderPickerService
    {
        private readonly IDispatcherService _dispatcherService;
        private readonly IAppWindowContext _appWindowContext;
        private readonly ILogger<FolderPickerService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderPickerService"/> class.
        /// </summary>
        /// <param name="dispatcherService">The UI thread dispatcher service.</param>
        /// <param name="appWindowContext">The application window context provider.</param>
        /// <param name="logger">Optional logger instance for diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dispatcherService"/> or <paramref name="appWindowContext"/> is null.</exception>
        public FolderPickerService(
            IDispatcherService dispatcherService,
            IAppWindowContext appWindowContext,
            ILogger<FolderPickerService>? logger = null)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _appWindowContext = appWindowContext ?? throw new ArgumentNullException(nameof(appWindowContext));
            _logger = logger ?? NullLogger<FolderPickerService>.Instance;
        }

        /// <summary>
        /// Displays a folder picker modal dialog allowing the user to select a target directory.
        /// also handles if the application is running in elevated mode or if the active window handle cannot be retrieved.
        /// </summary>
        /// <returns>The full path of the selected folder, or <c>null</c> if canceled.</returns>
        public async Task<string?> PickFolderAsync()
        {
            TaskCompletionSource<string?> tcs = new();

            bool enqueued = _dispatcherService.TryEnqueue((Func<Task>)(async () =>
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
