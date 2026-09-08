using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

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

        public async Task<TransferOutcome> TransferGamesAsync(TransferRequest request, IProgress<TransferProgress>? progress = null)
        {
            var tcs = new TaskCompletionSource<TransferOutcome>();

            // Pre-calculate the total known bytes for the entire queue
            long batchTotalBytes = 0;
            foreach (var item in request.Items)
            {
                if (item.Action != CopyAction.Skip && item.Game != null)
                {
                    batchTotalBytes += item.Game.TotalBytes;
                }
            }

            Thread workerThread = new Thread(() =>
            {
                bool comInitialized = false;
                try
                {
                    int hrInit = NativeMethods.CoInitializeEx(IntPtr.Zero, NativeMethods.COINIT_APARTMENTTHREADED);
                    comInitialized = hrInit == 0 || hrInit == 1; // S_OK or S_FALSE

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

                    long accumulatedBytesCompleted = 0;

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

                            bool result = false;
                            bool isMergePath = item.Action == CopyAction.Merge && Directory.Exists(destPath) && Directory.Exists(game.FolderPath);

                            if (isMergePath)
                            {
                                result = MergeDirectory(game.FolderPath, destPath);
                                if (result) {
                                    accumulatedBytesCompleted += game.TotalBytes;
                                }
                            }
                            else
                            {
                                // Use IFileOperation for standard Copy/Replace
                                result = ExecuteIFileOperation(game.FolderPath, destPath, item.Action == CopyAction.Replace, progress, batchTotalBytes, accumulatedBytesCompleted, game.TotalBytes);
                                if (result) {
                                    accumulatedBytesCompleted += game.TotalBytes;
                                }
                            }
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
                finally
                {
                    if (comInitialized)
                    {
                        NativeMethods.CoUninitialize();
                    }
                }
            });
            workerThread.SetApartmentState(ApartmentState.STA);
            workerThread.Start();
            return await tcs.Task;
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

        private bool ExecuteIFileOperation(string sourcePath, string destPath, bool isReplace, IProgress<TransferProgress>? progress, long batchTotalBytes, long accumulatedBytesCompleted, long currentItemBytes)
        {
            NativeMethods.IFileOperation? fileOp = null;
            NativeMethods.IShellItem? sourceItem = null;
            NativeMethods.IShellItem? destDirItem = null;
            NativeMethods.IShellItem? destItemForDelete = null;
            uint cookie = 0;

            try
            {
                Type? fileOpType = Type.GetTypeFromCLSID(NativeMethods.CLSID_FileOperation);
                if (fileOpType == null) throw new Exception("IFileOperation CLSID not found.");

                fileOp = (NativeMethods.IFileOperation)Activator.CreateInstance(fileOpType)!;

                // Set Explorer UI flags
                fileOp.SetOperationFlags(NativeMethods.FOFX_NOMINIMIZEBOX | NativeMethods.FOF_NOCONFIRMMKDIR);

                TransferProgressSink? sinkRef = progress != null ? new TransferProgressSink(progress, batchTotalBytes, accumulatedBytesCompleted, currentItemBytes) : null;
                if (sinkRef != null)
                {
                    fileOp.Advise(sinkRef, out cookie);
                }

                NativeMethods.SHCreateItemFromParsingName(sourcePath, IntPtr.Zero, NativeMethods.IID_IShellItem, out sourceItem);

                string destDirPath = Path.GetDirectoryName(destPath) ?? string.Empty;
                if (!Directory.Exists(destDirPath)) Directory.CreateDirectory(destDirPath);
                NativeMethods.SHCreateItemFromParsingName(destDirPath, IntPtr.Zero, NativeMethods.IID_IShellItem, out destDirItem);

                if (isReplace)
                {
                    try
                    {
                        NativeMethods.SHCreateItemFromParsingName(destPath, IntPtr.Zero, NativeMethods.IID_IShellItem, out destItemForDelete);
                        fileOp.DeleteItem(destItemForDelete, null);
                    }
                    catch { /* file might not exist yet */ }
                }

                fileOp.CopyItem(sourceItem, destDirItem, Path.GetFileName(destPath), null);

                int hrPerform = fileOp.PerformOperations();

                fileOp.GetAnyOperationsAborted(out bool aborted);

                if (hrPerform < 0)
                {
                    _logger.LogWarning("IFileOperation.PerformOperations failed with HRESULT: 0x{HR:X}", hrPerform);
                }

                GC.KeepAlive(sinkRef);

                return hrPerform >= 0 && !aborted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IFileOperation copy failed for {Source}", sourcePath);
                return false;
            }
            finally
            {
                if (fileOp != null && cookie != 0) fileOp.Unadvise(cookie);
                if (sourceItem != null) Marshal.ReleaseComObject(sourceItem);
                if (destDirItem != null) Marshal.ReleaseComObject(destDirItem);
                if (destItemForDelete != null) Marshal.ReleaseComObject(destItemForDelete);
                if (fileOp != null) Marshal.ReleaseComObject(fileOp);
            }
        }

        internal static class NativeMethods
        {
            public const uint FOFX_NOMINIMIZEBOX = 0x00008000;
            public const uint FOF_NOCONFIRMMKDIR = 0x0200;
            public const uint FOF_NO_UI = 0x0400 | 0x0010 | 0x0004 | 0x0200;

            // Shell COM definitions for IFileOperation

            [DllImport("ole32.dll")]
            public static extern int CoInitializeEx(IntPtr pvReserved, int dwCoInit);

            [DllImport("ole32.dll")]
            public static extern void CoUninitialize();

            public const int COINIT_APARTMENTTHREADED = 0x2;

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
            public static extern void SHCreateItemFromParsingName(
                [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                IntPtr pbc,
                [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                out IShellItem ppv);

            public static readonly Guid CLSID_FileOperation = new("3ad05575-8857-4850-9277-11b85bdb8e09");
            public static readonly Guid IID_IShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

            [ComImport]
            [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            public interface IShellItem
            {
                void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
                void GetParent(out IShellItem ppsi);
                void GetDisplayName(uint sigdnName, out IntPtr ppszName);
                void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
                void Compare(IShellItem psi, uint hint, out int piOrder);
            }

            [ComImport]
            [Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            public interface IFileOperation
            {
                [PreserveSig] int Advise(IFileOperationProgressSink pfops, out uint pdwCookie);
                [PreserveSig] int Unadvise(uint dwCookie);
                [PreserveSig] int SetOperationFlags(uint dwOperationFlags);
                [PreserveSig] int SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
                [PreserveSig] int SetProgressDialog([MarshalAs(UnmanagedType.Interface)] IntPtr popd);
                [PreserveSig] int SetProperties([MarshalAs(UnmanagedType.Interface)] IntPtr pproparray);
                [PreserveSig] int SetOwnerWindow(IntPtr hwndOwner);
                [PreserveSig] int ApplyPropertiesToItem(IShellItem psiItem);
                [PreserveSig] int ApplyPropertiesToItems([MarshalAs(UnmanagedType.Interface)] IntPtr punkItems);
                [PreserveSig] int RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink? pfopsItem);
                [PreserveSig] int RenameItems([MarshalAs(UnmanagedType.Interface)] IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                [PreserveSig] int MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string? pszNewName, IFileOperationProgressSink? pfopsItem);
                [PreserveSig] int MoveItems([MarshalAs(UnmanagedType.Interface)] IntPtr punkItems, IShellItem psiDestinationFolder);
                [PreserveSig] int CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string? pszCopyName, IFileOperationProgressSink? pfopsItem);
                [PreserveSig] int CopyItems([MarshalAs(UnmanagedType.Interface)] IntPtr punkItems, IShellItem psiDestinationFolder);
                [PreserveSig] int DeleteItem(IShellItem psiItem, IFileOperationProgressSink? pfopsItem);
                [PreserveSig] int DeleteItems([MarshalAs(UnmanagedType.Interface)] IntPtr punkItems);
                [PreserveSig] int NewItem(IShellItem psiDestinationFolder, uint dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string? pszTemplateName, IFileOperationProgressSink? pfopsItem);
                [PreserveSig] int PerformOperations();
                [PreserveSig] int GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool pfAnyOperationsAborted);
            }

            [ComImport]
            [Guid("04b0f1a5-8d70-48ea-a0cb-d64fac112401")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            public interface IFileOperationProgressSink
            {
                [PreserveSig] int StartOperations();
                [PreserveSig] int FinishOperations(int hrResult);
                [PreserveSig] int PreRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                [PreserveSig] int PostRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrRename, IShellItem psiNewlyCreated);
                [PreserveSig] int PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                [PreserveSig] int PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrMove, IShellItem psiNewlyCreated);
                [PreserveSig] int PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                [PreserveSig] int PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrCopy, IShellItem psiNewlyCreated);
                [PreserveSig] int PreDeleteItem(uint dwFlags, IShellItem psiItem);
                [PreserveSig] int PostDeleteItem(uint dwFlags, IShellItem psiItem, int hrDelete, IShellItem psiNewlyCreated);
                [PreserveSig] int PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                [PreserveSig] int PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, uint dwFileAttributes, int hrNew, IShellItem psiNewItem);
                [PreserveSig] int UpdateProgress(uint iWorkTotal, uint iWorkSoFar);
                [PreserveSig] int ResetTimer();
                [PreserveSig] int PauseTimer();
                [PreserveSig] int ResumeTimer();
            }

        }
    }
}
