using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Represents the summary result of a safe file/folder recycle operation.
    /// </summary>
    /// <param name="SucceededCount">Number of items successfully moved to the Recycle Bin.</param>
    /// <param name="FailedCount">Number of items that could not be deleted.</param>
    /// <param name="RecoveredBytes">Total byte size of items successfully recycled.</param>
    /// <param name="Errors">List of error descriptions encountered during the operation.</param>
    public record SafeDeletionResult(
        int SucceededCount,
        int FailedCount,
        long RecoveredBytes,
        IReadOnlyList<string> Errors);

    /// <summary>
    /// Defines safe deletion operations that route files and directories to the Windows Recycle Bin rather than permanently destroying them.
    /// </summary>
    public interface ISafeDeletionService
    {
        /// <summary>
        /// Moves a single file or directory to the Windows Recycle Bin using native Windows Shell <see cref="Easy_Copier.Interop.IFileOperation"/>.
        /// </summary>
        /// <param name="targetPath">The absolute path of the file or folder to recycle.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task returning <see langword="true"/> if the item was successfully moved to the Recycle Bin; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeleteToRecycleBinAsync(string targetPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves multiple files or directories to the Windows Recycle Bin in a batch with progress reporting.
        /// </summary>
        /// <param name="targets">Collection of target paths with their sizes in bytes.</param>
        /// <param name="progress">An optional progress reporter reporting percentage completed (0.0 to 100.0).</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A summary of the deletion operation.</returns>
        Task<SafeDeletionResult> BatchDeleteToRecycleBinAsync(
            IReadOnlyList<(string Path, long Bytes)> targets,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default);
    }
}

