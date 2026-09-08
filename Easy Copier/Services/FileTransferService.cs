using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface IFileTransferService
    {
        Task<TransferOutcome> TransferGamesAsync(TransferRequest request, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default);
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

        public async Task<TransferOutcome> TransferGamesAsync(TransferRequest request, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<TransferOutcome> tcs = new();

            Thread workerThread = new(() =>
            {
                try
                {
                    // IFileOperation requires an STA thread. We must explicitly initialize COM for this thread.
                    int hr = NativeFileOperation.CoInitializeEx(IntPtr.Zero, NativeFileOperation.COINIT_APARTMENTTHREADED);
                    bool comInitialized = hr >= 0;

                    try
                    {
                        TransferOutcome result = ExecuteTransferWithIFileOperation(request, progress, cancellationToken);
                        tcs.SetResult(result);
                    }
                    finally
                    {
                        if (comInitialized)
                        {
                            NativeFileOperation.CoUninitialize();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transfer thread encountered an error");
                    tcs.SetException(ex);
                }
            })
            {
                IsBackground = true
            };
            workerThread.SetApartmentState(ApartmentState.STA);
            workerThread.Start();

            return await tcs.Task;
        }

        private TransferOutcome ExecuteTransferWithIFileOperation(TransferRequest request, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
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

            long totalBytesToTransfer = request.Items.Where(i => i.Action != CopyAction.Skip && i.Game != null).Sum(i => i.Game.TotalBytes);
            Dictionary<string, TransferItem> itemMap = new(StringComparer.OrdinalIgnoreCase);

            IFileOperation? fileOp = null;
            FileOperationProgressSink? sink = null;
            uint cookie = 0;

            int queuedCount = 0;
            List<string> initializationErrors = new();

            try
            {
                fileOp = TryCreateFileOperation();
                if (fileOp is null)
                {
                    return ExecuteTransferWithManagedCopy(request, progress, cancellationToken);
                }

                // Suppress folder-creation prompts while letting the shell keep its native collision behavior.
                uint flags = (uint)(FileOperationFlags.FOF_NOCONFIRMMKDIR);

                fileOp.SetOperationFlags(flags);
                fileOp.SetOwnerWindow(IntPtr.Zero);

                sink = new FileOperationProgressSink(
                    progress,
                    cancellationToken,
                    totalBytesToTransfer,
                    itemMap,
                    _copyHistoryService,
                    request.TargetDrive,
                    CalculateAmount);

                cookie = fileOp.Advise(sink);

                NativeFileOperation.SHCreateItemFromParsingName(request.DestinationPath, IntPtr.Zero, IShellItemGuid, out IShellItem destFolderItem);

                try
                {
                    foreach (TransferItem item in request.Items)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            initializationErrors.Add("Transfer cancelled by user.");
                            break;
                        }

                        if (item == null || item.Game == null)
                        {
                            _logger.LogWarning("Found null transfer item or game reference; skipping.");
                            continue;
                        }

                        GameEntry game = item.Game;

                        if (item.Action == CopyAction.Skip)
                        {
                            _logger.LogInformation("Skipping {Game}", game.Name);
                            continue;
                        }

                        // Handle Replace action natively using COM
                        if (item.Action == CopyAction.Replace)
                        {
                            string destPath = Path.Combine(request.DestinationPath, game.Name);
                            if (File.Exists(game.FolderPath))
                            {
                                destPath = Path.Combine(request.DestinationPath, Path.GetFileName(game.FolderPath));
                            }

                            if (Directory.Exists(destPath) || File.Exists(destPath))
                            {
                                try
                                {
                                    NativeFileOperation.SHCreateItemFromParsingName(destPath, IntPtr.Zero, IShellItemGuid, out IShellItem existingItem);
                                    fileOp.DeleteItem(existingItem, null);
                                    Marshal.ReleaseComObject(existingItem);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to queue deletion for replacement: {Dest}", destPath);
                                }
                            }
                        }

                        try
                        {
                            NativeFileOperation.SHCreateItemFromParsingName(game.FolderPath, IntPtr.Zero, IShellItemGuid, out IShellItem sourceItem);

                            string? copyName = null;
                            if (Directory.Exists(game.FolderPath))
                            {
                                copyName = game.Name;
                            }

                            fileOp.CopyItem(sourceItem, destFolderItem, copyName, null);

                            // Map the parsing path to the item for the sink
                            string normalizedPath = game.FolderPath.Replace('/', '\\');
                            itemMap[normalizedPath] = item;

                            queuedCount++;

                            Marshal.ReleaseComObject(sourceItem);
                        }
                        catch (Exception ex)
                        {
                            initializationErrors.Add($"{game.Name}: Missing source path or COM error: {ex.Message}");
                            _logger.LogWarning("Failed to queue item: {Game}", game.Name);
                        }
                    }

                    if (queuedCount > 0)
                    {
                        fileOp.PerformOperations();

                        if (fileOp.GetAnyOperationsAborted() || sink.AnyOperationsAborted)
                        {
                            initializationErrors.Add("Operation was cancelled or aborted.");
                        }
                    }
                }
                finally
                {
                    if (destFolderItem != null) Marshal.ReleaseComObject(destFolderItem);
                }
            }
            catch (Exception ex)
            {
                initializationErrors.Add($"Transfer failed: {ex.Message}");
                _logger.LogError(ex, "IFileOperation batch failed");
            }
            finally
            {
                if (fileOp != null && cookie != 0)
                {
                    try { fileOp.Unadvise(cookie); } catch { }
                }

                if (fileOp != null)
                {
                    Marshal.ReleaseComObject(fileOp);
                }
            }

            int finalSuccessCount = sink?.SuccessCount ?? 0;
            long finalBytes = sink?.TotalBytesCopied ?? 0;
            var finalErrors = initializationErrors.Concat(sink?.Errors ?? new List<string>()).ToList();

            bool allSuccess = finalErrors.Count == 0 && finalSuccessCount == request.Items.Count(i => i.Action != CopyAction.Skip);
            string message = allSuccess
                ? $"Successfully copied {finalSuccessCount} item(s)"
                : $"Copied {finalSuccessCount} items. Errors: {string.Join("; ", finalErrors)}";

            return new TransferOutcome(
                allSuccess,
                message,
                finalSuccessCount,
                finalBytes,
                DateTime.Now);
        }

        private IFileOperation? TryCreateFileOperation()
        {
            try
            {
                return (IFileOperation)new FileOperation();
            }
            catch (Exception ex) when (ex is InvalidCastException or COMException)
            {
                _logger.LogWarning(ex, "Native IFileOperation activation failed; using managed copy fallback.");
                return null;
            }
        }

        private TransferOutcome ExecuteTransferWithManagedCopy(TransferRequest request, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starting managed transfer of {Count} items to {Drive}",
                request.Items.Count,
                request.TargetDrive.DriveLetter);

            if (!Directory.Exists(request.DestinationPath))
            {
                _ = Directory.CreateDirectory(request.DestinationPath);
                _logger.LogInformation("Created destination directory: {Path}", request.DestinationPath);
            }

            long totalBytesToTransfer = request.Items.Where(i => i.Action != CopyAction.Skip && i.Game != null).Sum(i => i.Game.TotalBytes);
            long bytesCopied = 0;
            int successCount = 0;
            List<string> errors = new();
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                foreach (TransferItem item in request.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.Game is null)
                    {
                        _logger.LogWarning("Found null transfer item or game reference; skipping.");
                        continue;
                    }

                    GameEntry game = item.Game;

                    if (item.Action == CopyAction.Skip)
                    {
                        _logger.LogInformation("Skipping {Game}", game.Name);
                        continue;
                    }

                    string destinationItemPath = GetManagedDestinationItemPath(request.DestinationPath, game);

                    try
                    {
                        if (item.Action == CopyAction.Replace)
                        {
                            RemoveExistingPath(destinationItemPath);
                        }

                        CopyPathManaged(
                            game.FolderPath,
                            destinationItemPath,
                            item.Action,
                            ref bytesCopied,
                            totalBytesToTransfer,
                            stopwatch,
                            progress,
                            cancellationToken);

                        successCount++;

                        _copyHistoryService.AddRecordAsync(new CopyHistoryRecord(
                            Id: 0,
                            Timestamp: DateTime.Now,
                            GameName: game.Name,
                            TargetDriveLetter: request.TargetDrive.DriveLetter,
                            TargetDriveLabel: request.TargetDrive.DriveLabel,
                            BytesTransferred: game.TotalBytes,
                            IsSuccess: true,
                            Amount: CalculateAmount(game.TotalBytes))).GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        errors.Add("Transfer cancelled by user.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{game.Name}: {ex.Message}");
                        _logger.LogWarning(ex, "Managed copy failed for {Game}", game.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Transfer failed: {ex.Message}");
                _logger.LogError(ex, "Managed transfer failed");
            }

            ReportManagedProgress(progress, stopwatch, bytesCopied, totalBytesToTransfer, string.Empty);

            bool allSuccess = errors.Count == 0 && successCount == request.Items.Count(i => i.Action != CopyAction.Skip);
            string message = allSuccess
                ? $"Successfully copied {successCount} item(s)"
                : $"Copied {successCount} items. Errors: {string.Join("; ", errors)}";

            return new TransferOutcome(allSuccess, message, successCount, bytesCopied, DateTime.Now);
        }

        private static string GetManagedDestinationItemPath(string destinationRoot, GameEntry game)
        {
            string sourcePath = game.FolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string itemName = Directory.Exists(game.FolderPath)
                ? game.Name
                : Path.GetFileName(sourcePath);

            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = game.Name;
            }

            return Path.Combine(destinationRoot, itemName);
        }

        private static void RemoveExistingPath(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return;
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private void CopyPathManaged(
            string sourcePath,
            string destinationPath,
            CopyAction action,
            ref long bytesCopied,
            long totalBytesToTransfer,
            Stopwatch stopwatch,
            IProgress<TransferProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (File.Exists(sourcePath))
            {
                CopyFileManaged(sourcePath, destinationPath, action, ref bytesCopied, totalBytesToTransfer, stopwatch, progress);
                return;
            }

            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"Source path not found: {sourcePath}");
            }

            if (action == CopyAction.Replace)
            {
                RemoveExistingPath(destinationPath);
            }

            Directory.CreateDirectory(destinationPath);

            foreach (string directory in Directory.EnumerateDirectories(sourcePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string targetDirectory = Path.Combine(destinationPath, Path.GetFileName(directory));
                CopyPathManaged(directory, targetDirectory, action, ref bytesCopied, totalBytesToTransfer, stopwatch, progress, cancellationToken);
            }

            foreach (string file in Directory.EnumerateFiles(sourcePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string targetFile = Path.Combine(destinationPath, Path.GetFileName(file));
                CopyFileManaged(file, targetFile, action, ref bytesCopied, totalBytesToTransfer, stopwatch, progress);
            }
        }

        private void CopyFileManaged(
            string sourcePath,
            string destinationPath,
            CopyAction action,
            ref long bytesCopied,
            long totalBytesToTransfer,
            Stopwatch stopwatch,
            IProgress<TransferProgress>? progress)
        {
            if (action != CopyAction.Replace && File.Exists(destinationPath))
            {
                return;
            }

            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourcePath, destinationPath, overwrite: action == CopyAction.Replace);

            bytesCopied += new FileInfo(sourcePath).Length;
            ReportManagedProgress(progress, stopwatch, bytesCopied, totalBytesToTransfer, Path.GetFileName(sourcePath));
        }

        private static void ReportManagedProgress(
            IProgress<TransferProgress>? progress,
            Stopwatch stopwatch,
            long bytesCopied,
            long totalBytesToTransfer,
            string currentItemName)
        {
            if (progress is null)
            {
                return;
            }

            double fraction = totalBytesToTransfer > 0 ? (double)bytesCopied / totalBytesToTransfer : 0;
            fraction = Math.Clamp(fraction, 0, 1);

            int percentage = (int)(fraction * 100);
            percentage = Math.Clamp(percentage, 0, 100);

            double speed = 0;
            TimeSpan eta = TimeSpan.Zero;

            if (stopwatch.Elapsed.TotalSeconds > 0)
            {
                double bytesPerSecond = bytesCopied / stopwatch.Elapsed.TotalSeconds;
                speed = bytesPerSecond / (1024 * 1024);

                if (bytesPerSecond > 0 && totalBytesToTransfer > bytesCopied)
                {
                    double secondsRemaining = (totalBytesToTransfer - bytesCopied) / bytesPerSecond;
                    if (secondsRemaining < 99 * 3600)
                    {
                        eta = TimeSpan.FromSeconds(secondsRemaining);
                    }
                }
            }

            progress.Report(new TransferProgress(
                Percentage: percentage,
                SpeedMegabytesPerSecond: speed,
                EstimatedTimeRemaining: eta,
                CurrentItemName: currentItemName,
                BytesTransferred: bytesCopied,
                TotalBytesToTransfer: totalBytesToTransfer));
        }
    }
}
