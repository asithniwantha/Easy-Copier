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
    /// <summary>
    /// Provides file picker dialog functionality for selecting files to open or save.
    /// </summary>
    public interface IFilePickerService
    {
        /// <summary>
        /// Displays a file save picker modal dialog allowing the user to select a destination path.
        /// </summary>
        /// <param name="suggestedFileName">The initial default file name suggested to the user.</param>
        /// <param name="fileTypeChoices">A dictionary mapping file type names to lists of supported extension patterns.</param>
        /// <returns>The full file path selected by the user, or <c>null</c> if canceled.</returns>
        Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices);

        /// <summary>
        /// Displays a file open picker modal dialog allowing the user to select an existing file.
        /// </summary>
        /// <param name="fileTypeFilters">A list of file extension filters (e.g., ".txt", ".iso") to allow.</param>
        /// <returns>The full file path selected by the user, or <c>null</c> if canceled.</returns>
        Task<string?> PickOpenFileAsync(IList<string> fileTypeFilters);
    }

    /// <summary>
    /// Implements file picker operations using WinRT <see cref="FileOpenPicker"/> and <see cref="FileSavePicker"/>.
    /// </summary>
    public class FilePickerService : IFilePickerService
    {
        private readonly IDispatcherService _dispatcherService;
        private readonly IAppWindowContext _appWindowContext;
        private readonly ILogger<FilePickerService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilePickerService"/> class.
        /// </summary>
        /// <param name="dispatcherService">The UI thread dispatcher service.</param>
        /// <param name="appWindowContext">The application window context provider.</param>
        /// <param name="logger">Optional logger instance for diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dispatcherService"/> or <paramref name="appWindowContext"/> is null.</exception>
        public FilePickerService(
            IDispatcherService dispatcherService,
            IAppWindowContext appWindowContext,
            ILogger<FilePickerService>? logger = null)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _appWindowContext = appWindowContext ?? throw new ArgumentNullException(nameof(appWindowContext));
            _logger = logger ?? NullLogger<FilePickerService>.Instance;
        }

        /// <summary>
        /// Displays a file open picker modal dialog allowing the user to select an existing file.
        /// </summary>
        /// <param name="fileTypeFilters">A list of file extension filters (e.g., ".txt", ".iso") to allow.</param>
        /// <returns>The full file path selected by the user, or <c>null</c> if canceled.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileTypeFilters"/> is null.</exception>
        public async Task<string?> PickOpenFileAsync(IList<string> fileTypeFilters)
        {
            ArgumentNullException.ThrowIfNull(fileTypeFilters);

            TaskCompletionSource<string?> tcs = new();

            bool enqueued = _dispatcherService.TryEnqueue((Func<Task>)(async () =>
            {
                try
                {
                    FileOpenPicker openPicker = new()
                    {
                        SuggestedStartLocation = PickerLocationId.ComputerFolder,
                        ViewMode = PickerViewMode.List
                    };

                    foreach (string filter in fileTypeFilters)
                    {
                        openPicker.FileTypeFilter.Add(filter);
                    }

                    nint windowHandle = NativeWindowHelper.GetActiveWindowHandle();
                    if (windowHandle == IntPtr.Zero && _appWindowContext.MainWindow is Microsoft.UI.Xaml.Window mainWindow)
                    {
                        windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                    }

                    if (windowHandle == IntPtr.Zero)
                    {
                        tcs.SetResult(null);
                        return;
                    }

                    WinRT.Interop.InitializeWithWindow.Initialize(openPicker, windowHandle);

                    StorageFile file = await openPicker.PickSingleFileAsync();
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

        /// <summary>
        /// Displays a file save picker modal dialog allowing the user to select a destination path.
        /// </summary>
        /// <param name="suggestedFileName">The initial default file name suggested to the user.</param>
        /// <param name="fileTypeChoices">A dictionary mapping file type names to lists of supported extension patterns.</param>
        /// <returns>The full file path selected by the user, or <c>null</c> if canceled.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileTypeChoices"/> is null.</exception>
        public async Task<string?> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypeChoices)
        {
            ArgumentNullException.ThrowIfNull(fileTypeChoices);

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
