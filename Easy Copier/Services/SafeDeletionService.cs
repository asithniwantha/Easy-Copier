using Easy_Copier.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Executes safe item deletion by moving files and folders to the Windows Recycle Bin using native Windows Shell <see cref="IFileOperation"/> COM interfaces.
    /// </summary>
    public class SafeDeletionService : ISafeDeletionService
    {
        private readonly ILogger<SafeDeletionService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeDeletionService"/> class.
        /// </summary>
        /// <param name="logger">The structured logger instance.</param>
        public SafeDeletionService(ILogger<SafeDeletionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<bool> DeleteToRecycleBinAsync(string targetPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return Task.FromResult(false);
            }

            // Execute on an STA background thread because Windows Shell IFileOperation requires COM STA apartment state.
            TaskCompletionSource<bool> tcs = new();

            Thread staThread = new(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    bool success = RecycleItemInternal(targetPath);
                    tcs.SetResult(success);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recycle item: {Path}", targetPath);
                    tcs.SetResult(false);
                }
            })
            {
                IsBackground = true
            };

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();

            return tcs.Task;
        }

        /// <inheritdoc />
        public Task<SafeDeletionResult> BatchDeleteToRecycleBinAsync(
            IReadOnlyList<(string Path, long Bytes)> targets,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(targets);

            if (targets.Count == 0)
            {
                return Task.FromResult(new SafeDeletionResult(0, 0, 0, []));
            }

            TaskCompletionSource<SafeDeletionResult> tcs = new();

            Thread staThread = new(() =>
            {
                int succeeded = 0;
                int failed = 0;
                long recoveredBytes = 0;
                List<string> errors = [];

                try
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        (string path, long bytes) = targets[i];
                        try
                        {
                            bool success = RecycleItemInternal(path);
                            if (success)
                            {
                                succeeded++;
                                recoveredBytes += bytes;
                            }
                            else
                            {
                                failed++;
                                errors.Add($"Failed to recycle '{path}': target could not be parsed or deleted by shell.");
                            }
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            errors.Add($"Error recycling '{path}': {ex.Message}");
                            _logger.LogError(ex, "Failed to recycle item {Path} during batch operation.", path);
                        }

                        // Report progress percentage incrementally
                        progress?.Report((double)(i + 1) / targets.Count * 100.0);
                    }

                    tcs.SetResult(new SafeDeletionResult(succeeded, failed, recoveredBytes, errors));
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fatal error during batch recycle operation.");
                    tcs.SetException(ex);
                }
            })
            {
                IsBackground = true
            };

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();

            return tcs.Task;
        }

        /// <summary>
        /// Recycles a single file or directory synchronously on the calling STA thread using native Windows Shell <see cref="IFileOperation"/>.
        /// </summary>
        /// <param name="targetPath">The absolute path of the target item.</param>
        /// <returns><see langword="true"/> if the item was successfully recycled or did not exist; otherwise <see langword="false"/>.</returns>
        private bool RecycleItemInternal(string targetPath)
        {
            if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                // Item is already gone
                return true;
            }

            object? fileOpObj = null;
            IShellItem? targetItem = null;

            try
            {
                Type? type = Type.GetTypeFromCLSID(new Guid(FileOperationInterop.CLSID_FileOperation));
                if (type == null)
                {
                    _logger.LogError("Unable to resolve CLSID_FileOperation COM type.");
                    return false;
                }

                fileOpObj = Activator.CreateInstance(type);
                if (fileOpObj is not IFileOperation fileOp)
                {
                    _logger.LogError("Failed to instantiate IFileOperation instance.");
                    return false;
                }

                // Intent: FOF_ALLOWUNDO ensures the operation moves items to the Windows Recycle Bin instead of deleting permanently.
                // FOF_NOCONFIRMATION avoids blocking dialogs since user already confirmed in Easy Copier UI.
                // FOF_NOERRORUI prevents native modal message boxes from freezing background thread.
                FILEOP_FLAGS flags = FILEOP_FLAGS.FOF_ALLOWUNDO
                                   | FILEOP_FLAGS.FOF_NOCONFIRMATION
                                   | FILEOP_FLAGS.FOFX_SHOWELEVATIONPROMPT
                                   | FILEOP_FLAGS.FOF_NOERRORUI;

                _ = fileOp.SetOperationFlags(flags);

                FileOperationInterop.SHCreateItemFromParsingName(
                    targetPath,
                    IntPtr.Zero,
                    typeof(IShellItem).GUID,
                    out targetItem);

                if (targetItem == null)
                {
                    _logger.LogWarning("SHCreateItemFromParsingName returned null target item for {Path}", targetPath);
                    return false;
                }

                uint delResult = fileOp.DeleteItem(targetItem, null);
                if (delResult != 0)
                {
                    _logger.LogWarning("DeleteItem returned error 0x{Result:X8} for {Path}", delResult, targetPath);
                    return false;
                }

                uint opResult = fileOp.PerformOperations();
                if (opResult != 0)
                {
                    _logger.LogWarning("PerformOperations returned error 0x{Result:X8} for {Path}", opResult, targetPath);
                    return false;
                }

                return true;
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
    }
}

