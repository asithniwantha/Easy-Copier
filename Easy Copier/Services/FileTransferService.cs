using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public class TransferProgress
    {
        public double Percentage { get; set; }
        public string SpeedText { get; set; } = string.Empty;
        public string RemainingTimeText { get; set; } = string.Empty;
    }

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
                                try
                                {
                                    if (Directory.Exists(destPath))
                                    {
                                        Directory.Delete(destPath, true);
                                    }
                                    else if (File.Exists(destPath))
                                    {
                                        File.Delete(destPath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to delete existing destination for replacement: {Dest}", destPath);
                                }
                            }

                            long queueTotalBytes = 0;
                            foreach (var i in request.Items)
                            {
                                if (i?.Game != null && i.Action != CopyAction.Skip)
                                {
                                    queueTotalBytes += i.Game.TotalBytes;
                                }
                            }

                            bool result = item.Action == CopyAction.Merge && Directory.Exists(destPath) && Directory.Exists(game.FolderPath)
                                ? MergeDirectory(game.FolderPath, destPath)
                                : CopyItemWithIFileOperation(game.FolderPath, request.DestinationPath, progress, queueTotalBytes, totalBytes, game.TotalBytes);
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
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.IsBackground = true;
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

        private bool CopyItemWithIFileOperation(string sourcePath, string destPath, IProgress<TransferProgress>? progress, long queueTotalBytes, long previouslyCopiedBytes, long currentItemBytes)
        {
            try
            {
                NativeMethods.IFileOperation? fileOperation = null;
                NativeMethods.IShellItem? sourceItem = null;
                NativeMethods.IShellItem? destFolder = null;
                uint cookie = 0;

                try
                {
                    Type? fileOperationType = Type.GetTypeFromCLSID(new Guid("3ad05575-8857-4850-9277-11b85bdb8e09"));
                    if (fileOperationType != null)
                    {
                        fileOperation = (NativeMethods.IFileOperation?)Activator.CreateInstance(fileOperationType);
                    }

                    if (fileOperation == null) return false;
                    fileOperation.SetOperationFlags(NativeMethods.FOF_NOCONFIRMMKDIR);

                    FileOperationProgressSink sink = new(progress, queueTotalBytes, previouslyCopiedBytes, currentItemBytes);
                    cookie = fileOperation.Advise(sink);

                    NativeMethods.SHCreateItemFromParsingName(sourcePath, IntPtr.Zero, typeof(NativeMethods.IShellItem).GUID, out sourceItem);
                    NativeMethods.SHCreateItemFromParsingName(destPath, IntPtr.Zero, typeof(NativeMethods.IShellItem).GUID, out destFolder);

                    fileOperation.CopyItem(sourceItem, destFolder, null!, null!);
                    fileOperation.PerformOperations();

                    return !fileOperation.GetAnyOperationsAborted();
                }
                finally
                {
                    if (fileOperation != null && cookie != 0)
                    {
                        try { fileOperation.Unadvise(cookie); } catch { }
                    }
                    if (sourceItem != null) Marshal.ReleaseComObject(sourceItem);
                    if (destFolder != null) Marshal.ReleaseComObject(destFolder);
                    if (fileOperation != null) Marshal.ReleaseComObject(fileOperation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IFileOperation copy failed for {Source}", sourcePath);
                return false;
            }
        }

        private class FileOperationProgressSink : NativeMethods.IFileOperationProgressSink
        {
            private readonly IProgress<TransferProgress>? _progress;
            private readonly long _queueTotalBytes;
            private readonly long _previouslyCopiedBytes;
            private readonly long _currentItemTotalBytes;
            private readonly System.Diagnostics.Stopwatch _stopwatch;

            public FileOperationProgressSink(IProgress<TransferProgress>? progress, long queueTotalBytes, long previouslyCopiedBytes, long currentItemTotalBytes)
            {
                _progress = progress;
                _queueTotalBytes = queueTotalBytes;
                _previouslyCopiedBytes = previouslyCopiedBytes;
                _currentItemTotalBytes = currentItemTotalBytes;
                _stopwatch = System.Diagnostics.Stopwatch.StartNew();
            }

            public void UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
            {
                if (_progress == null || _queueTotalBytes == 0) return;

                double currentItemProgress = iWorkTotal > 0 ? (double)iWorkSoFar / iWorkTotal : 0;
                long currentItemBytesCopied = (long)(currentItemProgress * _currentItemTotalBytes);
                long totalBytesCopiedSoFar = _previouslyCopiedBytes + currentItemBytesCopied;

                double totalPercentage = ((double)totalBytesCopiedSoFar / _queueTotalBytes) * 100.0;
                if (totalPercentage > 100.0) totalPercentage = 100.0;

                double elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
                string speedText = "";
                string remainingTimeText = "";

                if (elapsedSeconds > 0)
                {
                    double bytesPerSecond = currentItemBytesCopied / elapsedSeconds;
                    speedText = Infrastructure.FormattingHelpers.FormatBytes((long)bytesPerSecond) + "/s";

                    long bytesRemaining = _queueTotalBytes - totalBytesCopiedSoFar;
                    if (bytesPerSecond > 0)
                    {
                        double secondsRemaining = bytesRemaining / bytesPerSecond;
                        TimeSpan timeRemaining = TimeSpan.FromSeconds(secondsRemaining);

                        if (timeRemaining.TotalHours >= 1)
                        {
                            remainingTimeText = $"{(int)timeRemaining.TotalHours}h {timeRemaining.Minutes}m";
                        }
                        else if (timeRemaining.TotalMinutes >= 1)
                        {
                            remainingTimeText = $"{timeRemaining.Minutes}m {timeRemaining.Seconds}s";
                        }
                        else
                        {
                            remainingTimeText = $"{timeRemaining.Seconds}s";
                        }
                    }
                }

                _progress.Report(new TransferProgress
                {
                    Percentage = totalPercentage,
                    SpeedText = speedText,
                    RemainingTimeText = remainingTimeText
                });
            }

            public void StartOperations() { }
            public void FinishOperations(int hrResult) { }
            public void PreRenameItem(uint dwFlags, NativeMethods.IShellItem psiItem, string pszNewName) { }
            public void PostRenameItem(uint dwFlags, NativeMethods.IShellItem psiItem, string pszNewName, int hrRename, NativeMethods.IShellItem psiNewlyCreated) { }
            public void PreMoveItem(uint dwFlags, NativeMethods.IShellItem psiItem, NativeMethods.IShellItem psiDestinationFolder, string pszNewName) { }
            public void PostMoveItem(uint dwFlags, NativeMethods.IShellItem psiItem, NativeMethods.IShellItem psiDestinationFolder, string pszNewName, int hrMove, NativeMethods.IShellItem psiNewlyCreated) { }
            public void PreCopyItem(uint dwFlags, NativeMethods.IShellItem psiItem, NativeMethods.IShellItem psiDestinationFolder, string pszNewName) { }
            public void PostCopyItem(uint dwFlags, NativeMethods.IShellItem psiItem, NativeMethods.IShellItem psiDestinationFolder, string pszNewName, int hrCopy, NativeMethods.IShellItem psiNewlyCreated) { }
            public void PreDeleteItem(uint dwFlags, NativeMethods.IShellItem psiItem) { }
            public void PostDeleteItem(uint dwFlags, NativeMethods.IShellItem psiItem, int hrDelete, NativeMethods.IShellItem psiNewlyCreated) { }
            public void PreNewItem(uint dwFlags, NativeMethods.IShellItem psiDestinationFolder, string pszNewName) { }
            public void PostNewItem(uint dwFlags, NativeMethods.IShellItem psiDestinationFolder, string pszNewName, string pszTemplateName, uint dwFileAttributes, int hrNew, NativeMethods.IShellItem psiNewlyCreated) { }
            public void ResetTimer() { }
            public void PauseTimer() { }
            public void ResumeTimer() { }
        }

        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            public static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
            public static extern void SHCreateItemFromParsingName(
                [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                [In] IntPtr pbc,
                [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                [Out][MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

            [ComImport]
            [Guid("3ad05575-8857-4850-9277-11b85bdb8e09")]
            public class FileOperation { }

            [ComImport]
            [Guid("947aab5f-0a5c-4713-a4d6-4bf040b5d2b3")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [CoClass(typeof(FileOperation))]
            public interface IFileOperation
            {
                uint Advise(IFileOperationProgressSink pfops);
                void Unadvise(uint dwCookie);
                void SetOperationFlags(uint dwOperationFlags);
                void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
                void SetProgressDialog(IntPtr popd);
                void SetProperties(IntPtr pproparray);
                void SetOwnerWindow(IntPtr hwndOwner);
                void ApplyPropertiesToItem(IShellItem psiItem);
                void ApplyPropertiesToItems(IntPtr punkItems);
                void RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink? pfopsItem);
                void RenameItems(IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                void MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink? pfopsItem);
                void MoveItems(IntPtr punkItems, IShellItem psiDestinationFolder);
                void CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszCopyName, IFileOperationProgressSink? pfopsItem);
                void CopyItems(IntPtr punkItems, IShellItem psiDestinationFolder);
                void DeleteItem(IShellItem psiItem, IFileOperationProgressSink? pfopsItem);
                void DeleteItems(IntPtr punkItems);
                void NewItem(IShellItem psiDestinationFolder, uint dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, IFileOperationProgressSink? pfopsItem);
                void PerformOperations();
                [return: MarshalAs(UnmanagedType.Bool)]
                bool GetAnyOperationsAborted();
            }

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
            [Guid("04b0f1a5-8d70-48ea-a084-07d623678da4")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            public interface IFileOperationProgressSink
            {
                void StartOperations();
                void FinishOperations(int hrResult);
                void PreRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                void PostRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrRename, IShellItem psiNewlyCreated);
                void PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                void PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrMove, IShellItem psiNewlyCreated);
                void PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                void PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrCopy, IShellItem psiNewlyCreated);
                void PreDeleteItem(uint dwFlags, IShellItem psiItem);
                void PostDeleteItem(uint dwFlags, IShellItem psiItem, int hrDelete, IShellItem psiNewlyCreated);
                void PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
                void PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, uint dwFileAttributes, int hrNew, IShellItem psiNewlyCreated);
                void UpdateProgress(uint iWorkTotal, uint iWorkSoFar);
                void ResetTimer();
                void PauseTimer();
                void ResumeTimer();
            }

            public const int FO_COPY = 0x0002;
            public const ushort FOF_NOCONFIRMMKDIR = 0x0200;
            public const ushort FOF_NOERRORUI = 0x0400;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct SHFILEOPSTRUCT
            {
                public IntPtr hwnd;
                public int wFunc;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string pFrom;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string pTo;
                public ushort fFlags;
                [MarshalAs(UnmanagedType.Bool)]
                public bool fAnyOperationsAborted;
                public IntPtr hNameMappings;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string? lpszProgressTitle;
            }
        }
    }
}
