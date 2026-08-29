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
        Task<TransferOutcome> TransferGamesAsync(TransferRequest request);
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
            double gb = bytes / (1024.0 * 1024.0 * 1024.0);

            return gb <= 5.0
                ? settings.PriceTier1
                : gb <= 10.0 ? settings.PriceTier2 : gb < 16.0 ? settings.PriceTier3 : settings.PriceTier4;
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

        public async Task<TransferOutcome> TransferGamesAsync(TransferRequest request)
        {
            return await Task.Run(() =>
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

                            bool result = item.Action == CopyAction.Merge && Directory.Exists(destPath) && Directory.Exists(game.FolderPath)
                                ? MergeDirectory(game.FolderPath, destPath)
                                : CopyItemWithShellDialog(game.FolderPath, destPath);
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

                    return new TransferOutcome(

                        allSuccess && errors.Count == 0,
                        message,
                        successCount,
                        totalBytes,
                        DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transfer failed");
                    return new TransferOutcome(
                        false,
                        $"Transfer failed: {ex.Message}",
                        0,
                        0,
                        DateTime.Now);
                }
            });
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

        private bool CopyItemWithShellDialog(string sourcePath, string destPath)
        {
            try
            {
                NativeMethods.SHFILEOPSTRUCT fileOp = new()
                {
                    wFunc = NativeMethods.FO_COPY,
                    pFrom = sourcePath + "\0\0",
                    pTo = destPath + "\0\0",
                    fFlags = NativeMethods.FOF_NOCONFIRMMKDIR,
                    hwnd = IntPtr.Zero
                };

                int result = NativeMethods.SHFileOperation(ref fileOp);

                return result == 0 && !fileOp.fAnyOperationsAborted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shell copy operation failed for {Source}", sourcePath);
                return false;
            }
        }

        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            public static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

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
