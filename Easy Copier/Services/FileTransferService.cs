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
    public interface IFileTransferService
    {
        Task<TransferOutcome> TransferGamesAsync(TransferRequest request, IProgress<TransferProgress>? progress = null);
        Task<(long Size, int Count)> GetFolderStatsAsync(string path);
    }

    public class WindowsShellTransferService : IFileTransferService
    {
        private readonly ILogger<WindowsShellTransferService> _logger;
        private readonly ICopyHistoryService _copyHistoryService;
        private readonly ISettingsService _settingsService;

        public WindowsShellTransferService(
            ILogger<WindowsShellTransferService> logger,
            ICopyHistoryService copyHistoryService,
            ISettingsService settingsService)
        {
            _logger = logger;
            _copyHistoryService = copyHistoryService;
            _settingsService = settingsService;
        }

        private int CalculateAmount(long bytes)
        {
            AppSettings settings = _settingsService.LoadSettingsSync();
            return Infrastructure.FormattingHelpers.CalculatePrice(bytes, settings);
        }

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
                            if (result)
                            {
                                successCount++;
                                totalBytes += game.TotalBytes;
                                _logger.LogInformation("Successfully copied: {Game}", game.Name);
                            }
                            else
                            {
                                errors.Add($"{game.Name}: Copy operation was cancelled or failed");
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
                                Amount: result ? CalculateAmount(game.TotalBytes) : 0
                            )).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            // Safely extract the game name using null-conditional access to handle cases where item is null
                            string itemName = item?.Game?.Name ?? "Unknown item";
                            errors.Add($"{itemName}: {ex.Message}");
                            _logger.LogError(ex, "Error copying game: {Game}", itemName);

                            // Log failure to history
                            _copyHistoryService.AddRecordAsync(new CopyHistoryRecord(
                                Id: 0,
                                Timestamp: DateTime.Now,
                                GameName: itemName,
                                TargetDriveLetter: request.TargetDrive.DriveLetter,
                                TargetDriveLabel: request.TargetDrive.DriveLabel,
                                BytesTransferred: 0,
                                IsSuccess: false,
                                Amount: 0
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

                _ = fileOp.PerformOperations();
                _ = fileOp.GetAnyOperationsAborted(out bool aborted);

                return !aborted;
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

        private static class NativeMethods
        {
            [DllImport("ole32.dll")]
            public static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

            public const uint COINIT_APARTMENTTHREADED = 0x2;
        }
    }
}
