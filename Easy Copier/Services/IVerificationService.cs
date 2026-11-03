using Easy_Copier.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for verifying the integrity of transferred files and directories against their source.
    /// Intent: Provides an extensible abstraction for post-copy auditing to detect incomplete writes, corrupted sectors, or missing files.
    /// </summary>
    public interface IVerificationService
    {
        /// <summary>
        /// Asynchronously validates the contents of a destination file or directory against its source using the specified verification strategy.
        /// </summary>
        /// <param name="sourcePath">The source file or directory path.</param>
        /// <param name="destinationPath">The destination file or directory path to verify.</param>
        /// <param name="mode">The integrity verification strategy to execute.</param>
        /// <param name="progress">Optional progress reporter receiving percentage completion (0.0 to 100.0).</param>
        /// <param name="cancellationToken">Cancellation token for aborting verification.</param>
        /// <returns>A task that represents the asynchronous verification operation, producing a <see cref="VerificationResult"/>.</returns>
        Task<VerificationResult> VerifyCopyAsync(
            string sourcePath,
            string destinationPath,
            CopyVerificationMode mode,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default);
    }
}

