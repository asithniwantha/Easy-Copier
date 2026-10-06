using Easy_Copier.Interop;
using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for executing file transfer requests and querying directory file size statistics.
    /// </summary>
    public interface IFileTransferService
    {
        /// <summary>
        /// Asynchronously transfers game files according to the provided transfer request configuration.
        /// </summary>
        /// <param name="request">The transfer request containing target destination details and items to copy.</param>
        /// <param name="progress">An optional progress reporter for tracking transfer speed, completion percentage, and remaining time.</param>
        /// <returns>A task returning the <see cref="TransferOutcome"/> containing the result summary.</returns>
        Task<TransferOutcome> TransferGamesAsync(TransferRequest request, IProgress<TransferProgress>? progress = null);

        /// <summary>
        /// Calculates the total byte size and total count of files contained within a specified directory or file path.
        /// </summary>
        /// <param name="path">The directory or file path to evaluate.</param>
        /// <returns>A task returning a tuple containing total size in bytes and file count.</returns>
        Task<(long Size, int Count)> GetFolderStatsAsync(string path);
    }

    /// <summary>
    /// Implements file transfer operations utilizing the Windows Shell COM API (<see cref="IFileOperation"/>) on STA background threads.
    /// </summary>
    public partial class WindowsShellTransferService : IFileTransferService
    {
        /// <summary>
        /// Logger instance used for diagnostic logging of transfer operations.
        /// </summary>
        private readonly ILogger<WindowsShellTransferService> _logger;

        /// <summary>
        /// Service for persisting history records of performed copy operations.
        /// </summary>
        private readonly ICopyHistoryService _copyHistoryService;

        /// <summary>
        /// Service for loading pricing and transfer configurations.
        /// </summary>
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsShellTransferService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <param name="copyHistoryService">The copy history service for logging copy outcomes.</param>
        /// <param name="settingsService">The settings service for fetching application settings and pricing rules.</param>
        public WindowsShellTransferService(
            ILogger<WindowsShellTransferService> logger,
            ICopyHistoryService copyHistoryService,
            ISettingsService settingsService)
        {
            _logger = logger;
            _copyHistoryService = copyHistoryService;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Calculates the monetary price for transferring a specific byte volume based on current settings.
        /// </summary>
        /// <param name="bytes">The total bytes transferred.</param>
        /// <returns>The calculated monetary amount.</returns>
        private int CalculateAmount(long bytes)
        {
            AppSettings settings = _settingsService.LoadSettingsSync();
            return Infrastructure.FormattingHelpers.CalculatePrice(bytes, settings);
        }

        /// <summary>
        /// Asynchronously enumerates files and calculates total byte size and file count for a given file or folder path.
        /// </summary>
        /// <param name="path">The file or directory path to inspect.</param>
        /// <returns>A task returning a tuple containing total size in bytes and file count.</returns>
        public async Task<(long Size, int Count)> GetFolderStatsAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    return (0L, 0);
                }

                try
                {
                    if (File.Exists(path))
                    {
                        return (new FileInfo(path).Length, 1);
                    }

                    if (Directory.Exists(path))
                    {
                        DirectoryInfo dirInfo = new(path);
                        long size = 0;
                        int count = 0;

                        foreach (FileInfo file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
                            size += file.Length;
                            count++;
                        }

                        return (size, count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to calculate stats for {Path}", path);
                }

                return (0L, 0);
            });
        }

        /// <summary>
        /// Executes a transfer request asynchronously on a dedicated STA background thread using Windows Shell COM interop.
        /// </summary>
        /// <param name="request">The transfer request specifying items and target drive destination.</param>
        /// <param name="progress">Optional progress reporter for granular transfer progress updates.</param>
        /// <returns>A task returning a <see cref="TransferOutcome"/> containing the result summary.</returns>
        public Task<TransferOutcome> TransferGamesAsync(TransferRequest request, IProgress<TransferProgress>? progress = null)
        {
            TaskCompletionSource<TransferOutcome> tcs = new();

            System.Threading.Thread thread = new(() =>
            {
                try
                {
                    int hr = NativeMethods.CoInitializeEx(IntPtr.Zero, NativeMethods.COINIT_APARTMENTTHREADED);
                    if (hr < 0)
                    {
                        _logger.LogWarning("CoInitializeEx failed with HRESULT 0x{HResult:X8}", hr);
                    }

                    _logger.LogInformation(
                        "Starting transfer of {Count} items to {Drive}",
                        request.Items.Count,
                        request.TargetDrive.DriveLetter);

                    if (!Directory.Exists(request.DestinationPath))
                    {
                        _ = Directory.CreateDirectory(request.DestinationPath);
                        _logger.LogInformation("Created destination directory: {Path}", request.DestinationPath);
                    }

                    int successCount = 0;
                    long totalBytes = 0;
                    List<string> errors = [];

                    foreach (TransferItem item in request.Items)
                    {
                        try
                        {
                            // Verify that both the transfer item and its associated game entry are not null.
                            // This ensures proper support for C# nullable reference types and guards against malformed inputs.
                            if (item == null || item.Game == null)
                            {
                                _logger.LogWarning("Found null transfer item or game reference; skipping.");
                                continue;
                            }

                            GameEntry game = item.Game;
                            string destPath = Path.Combine(request.DestinationPath, game.Name);
                            if (File.Exists(game.FolderPath))
                            {
                                destPath = Path.Combine(request.DestinationPath, Path.GetFileName(game.FolderPath));
                            }

                            _logger.LogInformation("Copying {Game} to {Dest} with action {Action}", game.Name, destPath, item.Action);

                            if (item.Action == CopyAction.Skip)
                            {
                                _logger.LogInformation("Skipping {Game}", game.Name);
                                continue;
                            }

                            if (item.Action == CopyAction.Replace)
                            {
                                DeleteItemWithFileOperation(destPath);
                            }

                            bool result = item.Action == CopyAction.Merge && Directory.Exists(destPath) && Directory.Exists(game.FolderPath)
                                ? MergeDirectory(game.FolderPath, destPath)
                                : CopyItemWithFileOperation(game.FolderPath, request.DestinationPath, destPath, progress, game.TotalBytes);
                            string errorMsg = "";
                            string subFilesJson = "";
                            try
                            {
                                if (Directory.Exists(game.FolderPath))
                                {
                                    var files = Directory.GetFiles(game.FolderPath, "*", SearchOption.AllDirectories);
                                    subFilesJson = System.Text.Json.JsonSerializer.Serialize(files);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to scan source directory for sub-files: {Path}", game.FolderPath);
                            }

                            if (result)
                            {
                                successCount++;
                                totalBytes += game.TotalBytes;
                                _logger.LogInformation("Successfully copied: {Game}", game.Name);
                            }
                            else
                            {
                                errorMsg = "Copy operation was cancelled or failed";
                                errors.Add($"{game.Name}: {errorMsg}");
                                _logger.LogWarning("Copy failed or cancelled: {Game}", game.Name);
                            }

                            // Log to history
                            _copyHistoryService.AddRecordAsync(new CopyHistoryRecord(
                                Id: 0,
                                Timestamp: DateTime.Now,
                                GameName: game.Name,
                                TargetDriveLetter: request.TargetDrive.DriveLetter,
                                TargetDriveLabel: request.TargetDrive.DriveLabel,
                                BytesTransferred: result ? game.TotalBytes : 0,
                                IsSuccess: result,
                                Amount: result ? CalculateAmount(game.TotalBytes) : 0,
                                SourcePath: game.FolderPath,
                                DestinationPath: destPath,
                                ErrorLog: errorMsg,
                                SubFilesJson: subFilesJson
                            )).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            string errorMsg = ex.Message;
                            // Safely extract the game name using null-conditional access to handle cases where item is null
                            string itemName = item?.Game?.Name ?? "Unknown item";
                            errors.Add($"{itemName}: {errorMsg}");
                            _logger.LogError(ex, "Error copying game: {Game}", itemName);

                            string srcPath = item?.Game?.FolderPath ?? "";
                            string destPath = Path.Combine(request.DestinationPath, itemName);

                            // Log failure to history
                            _copyHistoryService.AddRecordAsync(new CopyHistoryRecord(
                                Id: 0,
                                Timestamp: DateTime.Now,
                                GameName: itemName,
                                TargetDriveLetter: request.TargetDrive.DriveLetter,
                                TargetDriveLabel: request.TargetDrive.DriveLabel,
                                BytesTransferred: 0,
                                IsSuccess: false,
                                Amount: 0,
                                SourcePath: srcPath,
                                DestinationPath: destPath,
                                ErrorLog: errorMsg,
                                SubFilesJson: ""
                            )).GetAwaiter().GetResult();
                        }
                    }

                    bool allSuccess = successCount == request.Items.Count;
                    string message = allSuccess
                        ? $"Successfully copied {successCount} item(s)"
                        : $"Copied {successCount} of {request.Items.Count} items. Errors: {string.Join("; ", errors)}";

                    _logger.LogInformation("Transfer completed. Success: {SuccessCount}, Total Bytes: {TotalBytes}, Errors: {ErrorCount}", successCount, totalBytes, errors.Count);

                    tcs.SetResult(new TransferOutcome(

                        allSuccess && errors.Count == 0,
                        message,
                        successCount,
                        totalBytes,
                        DateTime.Now));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transfer failed");
                    tcs.SetResult(new TransferOutcome(
                        false,
                        $"Transfer failed: {ex.Message}",
                        0,
                        0,
                        DateTime.Now));
                }
            })
            {
                IsBackground = true
            };
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

        /// <summary>
        /// Merges a source directory into a target destination directory without overwriting existing target files.
        /// </summary>
        /// <param name="sourceDir">The path of the source directory.</param>
        /// <param name="destDir">The path of the destination directory.</param>
        /// <returns><c>true</c> if the merge completed successfully; otherwise, <c>false</c>.</returns>
        private bool MergeDirectory(string sourceDir, string destDir)
        {
            try
            {
                DirectoryInfo dir = new(sourceDir);

                if (!dir.Exists)
                {
                    return false;
                }

                DirectoryInfo[] dirs = dir.GetDirectories();
                _ = Directory.CreateDirectory(destDir);

                foreach (FileInfo file in dir.GetFiles())
                {
                    string targetFilePath = Path.Combine(destDir, file.Name);
                    if (!File.Exists(targetFilePath))
                    {
                        _ = file.CopyTo(targetFilePath, false);
                    }
                }

                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destDir, subDir.Name);
                    if (!MergeDirectory(subDir.FullName, newDestinationDir))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Merge operation failed for {Source}", sourceDir);
                return false;
            }
        }

        /// <summary>
        /// Deletes a file or folder using native Windows Shell <see cref="IFileOperation"/> COM interface.
        /// </summary>
        /// <param name="targetPath">The full path of the target file or directory to delete.</param>
        private void DeleteItemWithFileOperation(string targetPath)
        {
            if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                return;
            }

            object? fileOpObj = null;
            IShellItem? targetItem = null;

            try
            {
                Type? type = Type.GetTypeFromCLSID(new Guid(FileOperationInterop.CLSID_FileOperation));
                if (type == null)
                {
                    throw new InvalidOperationException("Could not get type from CLSID");
                }

                fileOpObj = Activator.CreateInstance(type);
                if (fileOpObj == null)
                {
                    throw new InvalidOperationException("Could not create IFileOperation instance");
                }

                IFileOperation? fileOp = (IFileOperation)fileOpObj;

                FILEOP_FLAGS flags = FILEOP_FLAGS.FOF_NOCONFIRMATION | FILEOP_FLAGS.FOFX_SHOWELEVATIONPROMPT;
                _ = fileOp.SetOperationFlags(flags);

                FileOperationInterop.SHCreateItemFromParsingName(targetPath, IntPtr.Zero, typeof(IShellItem).GUID, out targetItem);

                _ = fileOp.DeleteItem(targetItem, null);
                _ = fileOp.PerformOperations();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete existing destination for replacement: {Dest}", targetPath);
            }
            finally
            {
                if (targetItem != null)
                {
                    _ = Marshal.ReleaseComObject(targetItem);
                }

                if (fileOpObj != null && Marshal.IsComObject(fileOpObj))
                {
                    _ = Marshal.ReleaseComObject(fileOpObj);
                }
            }
        }

        /// <summary>
        /// Copies a source file or directory to a destination folder using Windows Shell <see cref="IFileOperation"/> COM interface.
        /// </summary>
        /// <param name="sourcePath">The path of the source item.</param>
        /// <param name="destFolder">The destination directory path.</param>
        /// <param name="destPath">The complete path expected for the item at destination.</param>
        /// <param name="progress">An optional progress sink reporter.</param>
        /// <param name="totalBytes">The total size in bytes of the item being transferred.</param>
        /// <returns><c>true</c> if the copy completed successfully without cancellation or failure; otherwise, <c>false</c>.</returns>
        private bool CopyItemWithFileOperation(string sourcePath, string destFolder, string destPath, IProgress<TransferProgress>? progress, long totalBytes)
        {
            object? fileOpObj = null;
            IFileOperation? fileOp = null;
            IShellItem? sourceItem = null;
            IShellItem? destFolderItem = null;
            uint cookie = 0;

            try
            {
                Type? type = Type.GetTypeFromCLSID(new Guid(FileOperationInterop.CLSID_FileOperation));
                if (type == null)
                {
                    throw new InvalidOperationException("Could not get type from CLSID");
                }

                fileOpObj = Activator.CreateInstance(type);
                if (fileOpObj == null)
                {
                    throw new InvalidOperationException("Could not create IFileOperation instance");
                }

                fileOp = (IFileOperation)fileOpObj;

                FILEOP_FLAGS flags = FILEOP_FLAGS.FOF_NOCONFIRMMKDIR | FILEOP_FLAGS.FOFX_SHOWELEVATIONPROMPT;
                _ = fileOp.SetOperationFlags(flags);

                IFileOperationProgressSink? sink = new FileOperationProgressSink(progress, totalBytes);
                _ = fileOp.Advise(sink, out cookie);

                FileOperationInterop.SHCreateItemFromParsingName(sourcePath, IntPtr.Zero, typeof(IShellItem).GUID, out sourceItem);
                FileOperationInterop.SHCreateItemFromParsingName(destFolder, IntPtr.Zero, typeof(IShellItem).GUID, out destFolderItem);

                string destName = Path.GetFileName(destPath);
                _ = fileOp.CopyItem(sourceItem, destFolderItem, destName, null);

                uint hr = fileOp.PerformOperations();
                _ = fileOp.GetAnyOperationsAborted(out bool aborted);

                const uint COPYENGINE_E_USER_CANCELLED = 0x80270000;

                // hr >= 0x80000000 means failure HRESULT
                bool isFailed = hr is >= 0x80000000 and not COPYENGINE_E_USER_CANCELLED;

                return !(aborted || hr == COPYENGINE_E_USER_CANCELLED || isFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "COM copy operation failed for {Source}", sourcePath);
                return false;
            }
            finally
            {
                if (fileOp != null && cookie != 0)
                {
                    _ = fileOp.Unadvise(cookie);
                }

                if (sourceItem != null)
                {
                    _ = Marshal.ReleaseComObject(sourceItem);
                }

                if (destFolderItem != null)
                {
                    _ = Marshal.ReleaseComObject(destFolderItem);
                }

                if (fileOpObj != null && Marshal.IsComObject(fileOpObj))
                {
                    _ = Marshal.ReleaseComObject(fileOpObj);
                }
            }
        }

        /// <summary>
        /// P/Invoke definitions for native Windows COM library functions.
        /// </summary>
        private static partial class NativeMethods
        {
            /// <summary>
            /// Initializes the COM library on the calling thread.
            /// </summary>
            /// <param name="pvReserved">Reserved; must be <see cref="IntPtr.Zero"/>.</param>
            /// <param name="dwCoInit">The concurrency model and initialization flags.</param>
            /// <returns>An HRESULT indicating success or failure.</returns>
            [LibraryImport("ole32.dll")]
            public static partial int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

            /// <summary>
            /// Initializes the thread for single-threaded apartment (STA) COM execution.
            /// </summary>
            public const uint COINIT_APARTMENTTHREADED = 0x2;
        }
    }
}
