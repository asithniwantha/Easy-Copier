using Easy_Copier.Models;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Easy_Copier.Interop
{
    /// <summary>
    /// Implements <see cref="IFileOperationProgressSink"/> to monitor progress and calculate ETA/speed metrics during native Windows Shell file operations.
    /// </summary>
    public class FileOperationProgressSink : IFileOperationProgressSink
    {
        private readonly IProgress<TransferProgress>? _progress;
        private readonly long _totalBytes;
        private readonly Stopwatch _stopwatch;
        private long _bytesTransferred;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileOperationProgressSink"/> class.
        /// </summary>
        /// <param name="progress">The progress reporter instance used to update UI progress state.</param>
        /// <param name="totalBytes">The expected total byte size of the file operation batch.</param>
        public FileOperationProgressSink(IProgress<TransferProgress>? progress, long totalBytes)
        {
            _progress = progress;
            _totalBytes = totalBytes;
            _stopwatch = new Stopwatch();
        }

        /// <inheritdoc />
        public uint StartOperations()
        {
            _stopwatch.Start();
            return 0;
        }

        /// <inheritdoc />
        public uint FinishOperations(uint hrResult)
        {
            _stopwatch.Stop();
            _progress?.Report(new TransferProgress(100.0, "", "Completed"));
            return 0;
        }

        /// <inheritdoc />
        public uint PreRenameItem(uint dwFlags, IShellItem psiItem, string pszNewName)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PostRenameItem(uint dwFlags, IShellItem psiItem, string pszNewName, uint hrRename, IShellItem psiNewlyCreated)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, uint hrMove, IShellItem psiNewlyCreated)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, uint hrCopy, IShellItem psiNewlyCreated)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PreDeleteItem(uint dwFlags, IShellItem psiItem)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PostDeleteItem(uint dwFlags, IShellItem psiItem, uint hrDelete, IShellItem psiNewlyCreated)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, string pszName)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, string pszNewName, string pszTemplateName, uint hrNew, IShellItem psiNewlyCreated)
        {
            return 0;
        }

        /// <inheritdoc />
        public uint UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
        {
            if (_progress != null && iWorkTotal > 0 && _totalBytes > 0)
            {
                double fraction = (double)iWorkSoFar / iWorkTotal;
                _bytesTransferred = (long)(fraction * _totalBytes);

                double percentage = fraction * 100.0;

                double elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
                string speedText = "Calculating...";
                string remainingTimeText = "Calculating...";

                if (elapsedSeconds > 0)
                {
                    double bytesPerSecond = _bytesTransferred / elapsedSeconds;
                    speedText = Infrastructure.FormattingHelpers.FormatBytes((long)bytesPerSecond) + "/s";

                    if (bytesPerSecond > 0)
                    {
                        long remainingBytes = _totalBytes - _bytesTransferred;
                        double remainingSeconds = remainingBytes / bytesPerSecond;
                        TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                        remainingTimeText = remainingTime.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
                    }
                }

                _progress.Report(new TransferProgress(percentage, speedText, remainingTimeText));
            }

            return 0;
        }

        /// <inheritdoc />
        public uint ResetTimer()
        {
            return 0;
        }

        /// <inheritdoc />
        public uint PauseTimer()
        {
            return 0;
        }

        /// <inheritdoc />
        public uint ResumeTimer()
        {
            return 0;
        }
    }
}
