using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service providing file and folder integrity verification through file presence, size checks, and streaming cryptographic hashes.
    /// Intent: Protects users against bad flash drive blocks, silent data corruptions, and interrupted transfers.
    /// </summary>
    public class VerificationService : IVerificationService
    {
        /// <summary>
        /// Logger instance used to record integrity verification milestones, warnings, and errors.
        /// </summary>
        private readonly ILogger<VerificationService> _logger;

        /// <summary>
        /// Chunk buffer size (1 MB) used for streaming file reads during cryptographic and non-cryptographic checksum calculation.
        /// </summary>
        private const int ChunkBufferSize = 1024 * 1024; // 1 MB chunk size for balanced memory footprint and streaming throughput

        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for operational and error telemetry.</param>
        public VerificationService(ILogger<VerificationService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<VerificationResult> VerifyCopyAsync(
            string sourcePath,
            string destinationPath,
            CopyVerificationMode mode,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (mode == CopyVerificationMode.None)
            {
                return new VerificationResult(true);
            }

            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
            {
                return new VerificationResult(false, "Source or destination path is empty.");
            }

            try
            {
                bool isSourceFile = File.Exists(sourcePath);
                bool isSourceDir = Directory.Exists(sourcePath);

                return !isSourceFile && !isSourceDir
                    ? new VerificationResult(false, $"Source path does not exist: {sourcePath}")
                    : isSourceFile
                    ? await VerifySingleFileAsync(sourcePath, destinationPath, mode, progress, cancellationToken)
                    : await VerifyDirectoryAsync(sourcePath, destinationPath, mode, progress, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Verification cancelled by user for {Dest}", destinationPath);
                return new VerificationResult(false, "Verification was cancelled by user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during verification between {Source} and {Dest}", sourcePath, destinationPath);
                return new VerificationResult(false, $"Verification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies a single source file against its destination file.
        /// </summary>
        private static async Task<VerificationResult> VerifySingleFileAsync(
            string sourceFile,
            string destinationFile,
            CopyVerificationMode mode,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(destinationFile))
            {
                return new VerificationResult(false, $"Destination file does not exist: {destinationFile}", (string[])[Path.GetFileName(destinationFile)]);
            }

            FileInfo srcInfo = new(sourceFile);
            FileInfo dstInfo = new(destinationFile);

            if (srcInfo.Length != dstInfo.Length)
            {
                string msg = $"File size mismatch: source ({srcInfo.Length:N0} bytes) != destination ({dstInfo.Length:N0} bytes)";
                return new VerificationResult(false, msg, (string[])[Path.GetFileName(destinationFile)]);
            }

            if (mode == CopyVerificationMode.QuickSizeAndCount)
            {
                progress?.Report(100.0);
                return new VerificationResult(true);
            }

            bool matches = mode == CopyVerificationMode.FastXxHash64
                ? await CompareXxHash64Async(sourceFile, destinationFile, srcInfo.Length, progress, cancellationToken)
                : await CompareSha256Async(sourceFile, destinationFile, srcInfo.Length, progress, cancellationToken);

            progress?.Report(100.0);

            if (!matches)
            {
                string modeName = mode == CopyVerificationMode.FastXxHash64 ? "XxHash64" : "SHA-256";
                return new VerificationResult(false, $"{modeName} checksum mismatch", (string[])[Path.GetFileName(destinationFile)]);
            }

            return new VerificationResult(true);
        }

        /// <summary>
        /// Verifies an entire directory structure recursively against its destination.
        /// </summary>
        private async Task<VerificationResult> VerifyDirectoryAsync(
            string sourceDir,
            string destinationDir,
            CopyVerificationMode mode,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            if (!Directory.Exists(destinationDir))
            {
                return new VerificationResult(false, $"Destination directory does not exist: {destinationDir}");
            }

            string[] sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            List<string> mismatchedFiles = [];

            // Pre-calculate total bytes to provide accurate streaming progress
            long totalBytes = 0;
            List<(string SrcPath, string DstPath, string RelPath, long Size)> workList = [with(sourceFiles.Length)];

            foreach (string srcFile in sourceFiles)
            {
                string relPath = Path.GetRelativePath(sourceDir, srcFile);
                string dstFile = Path.Combine(destinationDir, relPath);
                long size = new FileInfo(srcFile).Length;
                totalBytes += size;
                workList.Add((srcFile, dstFile, relPath, size));
            }

            long processedBytes = 0;

            foreach ((string SrcPath, string DstPath, string RelPath, long Size) in workList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(DstPath))
                {
                    mismatchedFiles.Add($"{RelPath} (missing at destination)");
                    processedBytes += Size;
                    continue;
                }

                FileInfo dstInfo = new(DstPath);
                if (Size != dstInfo.Length)
                {
                    mismatchedFiles.Add($"{RelPath} (size mismatch: {Size:N0} != {dstInfo.Length:N0} bytes)");
                    processedBytes += Size;
                    continue;
                }

                if (mode == CopyVerificationMode.QuickSizeAndCount)
                {
                    processedBytes += Size;
                    if (totalBytes > 0)
                    {
                        progress?.Report((double)processedBytes / totalBytes * 100.0);
                    }
                    continue;
                }

                // Cryptographic or SIMD hash comparison
                bool matches = mode == CopyVerificationMode.FastXxHash64
                    ? await CompareXxHash64Async(SrcPath, DstPath, Size, null, cancellationToken)
                    : await CompareSha256Async(SrcPath, DstPath, Size, null, cancellationToken);

                if (!matches)
                {
                    string hashName = mode == CopyVerificationMode.FastXxHash64 ? "XxHash64" : "SHA-256";
                    mismatchedFiles.Add($"{RelPath} ({hashName} hash mismatch)");
                }

                processedBytes += Size;
                if (totalBytes > 0)
                {
                    progress?.Report((double)processedBytes / totalBytes * 100.0);
                }
            }

            progress?.Report(100.0);

            if (mismatchedFiles.Count > 0)
            {
                string summary = $"Integrity verification failed for {mismatchedFiles.Count} file(s).";
                _logger.LogWarning("Verification failed for {Dest}: {Summary} Details: {Details}",
                    destinationDir, summary, string.Join("; ", mismatchedFiles));
                return new VerificationResult(false, summary, mismatchedFiles);
            }

            _logger.LogInformation("Successfully verified {Count} file(s) ({Bytes:N0} bytes) in {Dest}",
                sourceFiles.Length, totalBytes, destinationDir);
            return new VerificationResult(true);
        }

        /// <summary>
        /// Computes and compares XxHash64 checksums across two files using streamed buffers.
        /// </summary>
        private static async Task<bool> CompareXxHash64Async(
            string srcPath,
            string dstPath,
            long fileSize,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            XxHash64 srcHasher = new();
            XxHash64 dstHasher = new();

            byte[] srcBuf = new byte[ChunkBufferSize];
            byte[] dstBuf = new byte[ChunkBufferSize];

            using FileStream srcStream = new(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkBufferSize, useAsync: true);
            using FileStream dstStream = new(dstPath, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkBufferSize, useAsync: true);

            long bytesReadTotal = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int srcRead = await srcStream.ReadAsync(srcBuf.AsMemory(0, ChunkBufferSize), cancellationToken);
                int dstRead = await dstStream.ReadAsync(dstBuf.AsMemory(0, ChunkBufferSize), cancellationToken);

                if (srcRead != dstRead)
                {
                    return false;
                }

                if (srcRead == 0)
                {
                    break;
                }

                srcHasher.Append(srcBuf.AsSpan(0, srcRead));
                dstHasher.Append(dstBuf.AsSpan(0, dstRead));

                bytesReadTotal += srcRead;
                if (fileSize > 0 && progress != null)
                {
                    progress.Report((double)bytesReadTotal / fileSize * 100.0);
                }
            }

            return srcHasher.GetCurrentHashAsUInt64() == dstHasher.GetCurrentHashAsUInt64();
        }

        /// <summary>
        /// Computes and compares SHA-256 hashes across two files using streamed buffers.
        /// </summary>
        private static async Task<bool> CompareSha256Async(
            string srcPath,
            string dstPath,
            long fileSize,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            using IncrementalHash srcHasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using IncrementalHash dstHasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            byte[] srcBuf = new byte[ChunkBufferSize];
            byte[] dstBuf = new byte[ChunkBufferSize];

            using FileStream srcStream = new(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkBufferSize, useAsync: true);
            using FileStream dstStream = new(dstPath, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkBufferSize, useAsync: true);

            long bytesReadTotal = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int srcRead = await srcStream.ReadAsync(srcBuf.AsMemory(0, ChunkBufferSize), cancellationToken);
                int dstRead = await dstStream.ReadAsync(dstBuf.AsMemory(0, ChunkBufferSize), cancellationToken);

                if (srcRead != dstRead)
                {
                    return false;
                }

                if (srcRead == 0)
                {
                    break;
                }

                srcHasher.AppendData(srcBuf, 0, srcRead);
                dstHasher.AppendData(dstBuf, 0, dstRead);

                bytesReadTotal += srcRead;
                if (fileSize > 0 && progress != null)
                {
                    progress.Report((double)bytesReadTotal / fileSize * 100.0);
                }
            }

            byte[] srcHash = srcHasher.GetHashAndReset();
            byte[] dstHash = dstHasher.GetHashAndReset();

            return CryptographicOperations.FixedTimeEquals(srcHash, dstHash);
        }
    }
}

