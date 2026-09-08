using System;
using System.Diagnostics;
using Easy_Copier.Models;

namespace Easy_Copier.Services
{
    internal class TransferProgressSink : WindowsShellTransferService.NativeMethods.IFileOperationProgressSink
    {
        private readonly IProgress<TransferProgress>? _progress;
        private readonly long _batchTotalBytes;
        private readonly long _accumulatedBytesCompleted;
        private readonly long _currentItemBytes;
        private readonly Stopwatch _stopwatch;

        public TransferProgressSink(IProgress<TransferProgress>? progress, long batchTotalBytes, long accumulatedBytesCompleted, long currentItemBytes)
        {
            _progress = progress;
            _batchTotalBytes = batchTotalBytes;
            _accumulatedBytesCompleted = accumulatedBytesCompleted;
            _currentItemBytes = currentItemBytes;
            _stopwatch = new Stopwatch();
        }

        public int StartOperations()
        {
            _stopwatch.Start();
            return 0; // S_OK
        }

        public int FinishOperations(int hrResult)
        {
            _stopwatch.Stop();
            return 0; // S_OK
        }

        public int PreRenameItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiItem, string pszNewName) => 0;
        public int PostRenameItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiItem, string pszNewName, int hrRename, WindowsShellTransferService.NativeMethods.IShellItem psiNewlyCreated) => 0;
        public int PreMoveItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiItem, WindowsShellTransferService.NativeMethods.IShellItem psiDestinationFolder, string pszNewName) => 0;
        public int PostMoveItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiItem, WindowsShellTransferService.NativeMethods.IShellItem psiDestinationFolder, string pszNewName, int hrMove, WindowsShellTransferService.NativeMethods.IShellItem psiNewlyCreated) => 0;
        public int PreCopyItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiItem, WindowsShellTransferService.NativeMethods.IShellItem psiDestinationFolder, string pszNewName) => 0;
        public int PostCopyItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiItem, WindowsShellTransferService.NativeMethods.IShellItem psiDestinationFolder, string pszNewName, int hrCopy, WindowsShellTransferService.NativeMethods.IShellItem psiNewlyCreated) => 0;
        public int PreDeleteItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiItem) => 0;
        public int PostDeleteItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiItem, int hrDelete, WindowsShellTransferService.NativeMethods.IShellItem psiNewlyCreated) => 0;
        public int PreNewItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiDestinationFolder, string pszNewName) => 0;
        public int PostNewItem(uint dwFlags, WindowsShellTransferService.NativeMethods.IShellItem psiDestinationFolder, string pszNewName, string pszTemplateName, uint dwFileAttributes, int hrNew, WindowsShellTransferService.NativeMethods.IShellItem psiNewItem) => 0;

        public int UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
        {
            try
            {
                if (_progress != null && iWorkTotal > 0 && _batchTotalBytes > 0)
                {
                    double currentItemFraction = (double)iWorkSoFar / iWorkTotal;
                    long currentItemBytesSoFar = (long)(currentItemFraction * _currentItemBytes);

                    long totalBytesSoFar = _accumulatedBytesCompleted + currentItemBytesSoFar;

                    double percentage = ((double)totalBytesSoFar / _batchTotalBytes) * 100.0;

                    double elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
                    string speedText = "Calculating...";
                    string remainingTimeText = "Calculating...";

                    if (elapsedSeconds > 0 && currentItemBytesSoFar > 0)
                    {
                        double bytesPerSecond = currentItemBytesSoFar / elapsedSeconds;
                        speedText = $"{Infrastructure.FormattingHelpers.FormatBytes((long)bytesPerSecond)}/s";

                        long remainingBytes = _batchTotalBytes - totalBytesSoFar;
                        if (remainingBytes > 0 && bytesPerSecond > 0)
                        {
                            double remainingSeconds = remainingBytes / bytesPerSecond;
                            TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                            remainingTimeText = $"{(int)remainingTime.TotalMinutes}m {remainingTime.Seconds}s remaining";
                        }
                        else
                        {
                            remainingTimeText = "0s remaining";
                        }
                    }

                    _progress.Report(new TransferProgress(percentage, speedText, remainingTimeText));
                }
            }
            catch { /* Do not let exceptions cross COM boundaries */ }
            return 0; // S_OK
        }

        public int ResetTimer() { _stopwatch.Reset(); return 0; }
        public int PauseTimer() { _stopwatch.Stop(); return 0; }
        public int ResumeTimer() { _stopwatch.Start(); return 0; }
    }
}
