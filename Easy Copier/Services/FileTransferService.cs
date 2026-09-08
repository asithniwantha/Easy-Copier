using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

            NativeFileOperation.IFileOperation? fileOp = null;
            FileOperationProgressSink? sink = null;
            uint cookie = 0;

            int queuedCount = 0;
            List<string> initializationErrors = new();

            try
            {
                // Directly instantiate the CoClass wrapper defined in NativeFileOperation
                fileOp = (NativeFileOperation.IFileOperation)new NativeFileOperation.FileOperation();

                // FOF_NOCONFIRMMKDIR ensures we don't get prompts to create target folders.
                // If we don't pass FOF_NOCONFIRMATION, the user will be prompted for collisions.
                // But the previous `MergeDirectory` silent-skipped existing files (with no UI prompt).
                // Wait, actually, the user wants "use shell flags to match current UX intent".
                // We'll omit FOF_NOCONFIRMATION so standard shell collision resolution applies for "Default".

                uint flags = (uint)(NativeFileOperation.FileOperationFlags.FOF_NOCONFIRMMKDIR);

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

                NativeFileOperation.SHCreateItemFromParsingName(request.DestinationPath, IntPtr.Zero, NativeFileOperation.IShellItemGuid, out NativeFileOperation.IShellItem destFolderItem);

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
                                    NativeFileOperation.SHCreateItemFromParsingName(destPath, IntPtr.Zero, NativeFileOperation.IShellItemGuid, out NativeFileOperation.IShellItem existingItem);
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
                            NativeFileOperation.SHCreateItemFromParsingName(game.FolderPath, IntPtr.Zero, NativeFileOperation.IShellItemGuid, out NativeFileOperation.IShellItem sourceItem);

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
    }
}
