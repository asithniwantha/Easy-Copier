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
    }

    public class WindowsShellTransferService : IFileTransferService
    {
        private readonly ILogger<WindowsShellTransferService> _logger;

        private readonly ICopyHistoryService _copyHistoryService;

        public WindowsShellTransferService(
            ILogger<WindowsShellTransferService> logger,
            ICopyHistoryService copyHistoryService)
        {
            _logger = logger;
            _copyHistoryService = copyHistoryService;
        }

        public async Task<TransferOutcome> TransferGamesAsync(TransferRequest request)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation(
                        "Starting transfer of {Count} games to {Drive}",
                        request.Games.Count,
                        request.TargetDrive.DriveLetter);

                    if (!Directory.Exists(request.DestinationPath))
                    {
                        Directory.CreateDirectory(request.DestinationPath);
                        _logger.LogInformation("Created destination directory: {Path}", request.DestinationPath);
                    }

                    int successCount = 0;
                    long totalBytes = 0;
                    var errors = new List<string>();

                    foreach (var game in request.Games)
                    {
                        try
                        {
                            var destPath = Path.Combine(request.DestinationPath, game.Name);

                            _logger.LogInformation("Copying {Game} to {Dest}", game.Name, destPath);

                            var result = CopyDirectoryWithShellDialog(game.FolderPath, destPath);

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
                            _ = _copyHistoryService.AddRecordAsync(new CopyHistoryRecord(
                                Id: 0,
                                Timestamp: DateTime.Now,
                                GameName: game.Name,
                                TargetDriveLetter: request.TargetDrive.DriveLetter,
                                TargetDriveLabel: request.TargetDrive.DriveLabel,
                                BytesTransferred: result ? game.TotalBytes : 0,
                                IsSuccess: result
                            ));
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{game.Name}: {ex.Message}");
                            _logger.LogError(ex, "Error copying game: {Game}", game.Name);

                            // Log failure to history
                            _ = _copyHistoryService.AddRecordAsync(new CopyHistoryRecord(
                                Id: 0,
                                Timestamp: DateTime.Now,
                                GameName: game.Name,
                                TargetDriveLetter: request.TargetDrive.DriveLetter,
                                TargetDriveLabel: request.TargetDrive.DriveLabel,
                                BytesTransferred: 0,
                                IsSuccess: false
                            ));
                        }
                    }

                    var allSuccess = successCount == request.Games.Count;
                    var message = allSuccess
                        ? $"Successfully copied {successCount} game(s)"
                        : $"Copied {successCount} of {request.Games.Count} games. Errors: {string.Join("; ", errors)}";

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

        private bool CopyDirectoryWithShellDialog(string sourcePath, string destPath)
        {
            try
            {
                var fileOp = new NativeMethods.SHFILEOPSTRUCT
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
