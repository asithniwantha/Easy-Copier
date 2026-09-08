using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Easy_Copier.Services
{
    internal class FileOperationProgressSink : NativeFileOperation.IFileOperationProgressSink
    {
        private readonly IProgress<TransferProgress>? _progress;
        private readonly CancellationToken _cancellationToken;
        private readonly Stopwatch _stopwatch = new();
        private long _totalBytesToTransfer;
        private long _bytesTransferred;
        private string _currentItemName = string.Empty;
        private DateTime _lastUpdate = DateTime.MinValue;

        private readonly Dictionary<string, TransferItem> _itemMap;
        private readonly ICopyHistoryService _copyHistoryService;
        private readonly RemovableDrive _targetDrive;
        private readonly Func<long, int> _calculateAmount;

        public bool AnyOperationsAborted { get; private set; }
        public int SuccessCount { get; private set; }
        public long TotalBytesCopied { get; private set; }
        public List<string> Errors { get; } = new();

        public FileOperationProgressSink(
            IProgress<TransferProgress>? progress,
            CancellationToken cancellationToken,
            long totalBytesToTransfer,
            Dictionary<string, TransferItem> itemMap,
            ICopyHistoryService copyHistoryService,
            RemovableDrive targetDrive,
            Func<long, int> calculateAmount)
        {
            _progress = progress;
            _cancellationToken = cancellationToken;
            _totalBytesToTransfer = totalBytesToTransfer;
            _itemMap = itemMap;
            _copyHistoryService = copyHistoryService;
            _targetDrive = targetDrive;
            _calculateAmount = calculateAmount;
        }

        public void StartOperations()
        {
            _stopwatch.Start();
        }

        public void FinishOperations(int hrResult)
        {
            _stopwatch.Stop();
        }

        public void PreRenameItem(uint dwFlags, NativeFileOperation.IShellItem psiItem, string pszNewName) { CheckCancellation(); }
        public void PostRenameItem(uint dwFlags, NativeFileOperation.IShellItem psiItem, string pszNewName, int hrRename, NativeFileOperation.IShellItem psiNewlyCreated) { }
        public void PreMoveItem(uint dwFlags, NativeFileOperation.IShellItem psiItem, NativeFileOperation.IShellItem psiDestinationFolder, string pszNewName) { CheckCancellation(); }
        public void PostMoveItem(uint dwFlags, NativeFileOperation.IShellItem psiItem, NativeFileOperation.IShellItem psiDestinationFolder, string pszNewName, int hrMove, NativeFileOperation.IShellItem psiNewlyCreated) { }

        public void PreCopyItem(uint dwFlags, NativeFileOperation.IShellItem psiItem, NativeFileOperation.IShellItem psiDestinationFolder, string pszNewName)
        {
            CheckCancellation();
            if (psiItem != null)
            {
                psiItem.GetDisplayName(0, out string name); // 0 = SIGDN_NORMALDISPLAY
                _currentItemName = name;
            }
        }

        public void PostCopyItem(uint dwFlags, NativeFileOperation.IShellItem psiItem, NativeFileOperation.IShellItem psiDestinationFolder, string pszNewName, int hrCopy, NativeFileOperation.IShellItem psiNewlyCreated)
        {
            if (psiItem != null)
            {
                psiItem.GetDisplayName(0x80058000, out string parsingPath); // SIGDN_FILESYSPATH

                // We only want to log history for top-level queued items, not every recursive file
                if (parsingPath != null && _itemMap.TryGetValue(parsingPath, out TransferItem? item))
                {
                    bool isSuccess = hrCopy >= 0 && !AnyOperationsAborted; // SUCCEEDED(hrCopy)
                    if (isSuccess)
                    {
                        SuccessCount++;
                        TotalBytesCopied += item.Game.TotalBytes;
                    }
                    else
                    {
                        Errors.Add($"{item.Game.Name}: Copy failed with HRESULT 0x{hrCopy:X8}");
                    }

                    _copyHistoryService.AddRecordAsync(new CopyHistoryRecord(
                        Id: 0,
                        Timestamp: DateTime.Now,
                        GameName: item.Game.Name,
                        TargetDriveLetter: _targetDrive.DriveLetter,
                        TargetDriveLabel: _targetDrive.DriveLabel,
                        BytesTransferred: isSuccess ? item.Game.TotalBytes : 0,
                        IsSuccess: isSuccess,
                        Amount: isSuccess ? _calculateAmount(item.Game.TotalBytes) : 0
                    )).GetAwaiter().GetResult();
                }
            }
        }

        public void PreDeleteItem(uint dwFlags, NativeFileOperation.IShellItem psiItem) { CheckCancellation(); }
        public void PostDeleteItem(uint dwFlags, NativeFileOperation.IShellItem psiItem, int hrDelete, NativeFileOperation.IShellItem psiNewlyCreated) { }
        public void PreNewItem(uint dwFlags, NativeFileOperation.IShellItem psiDestinationFolder, string pszNewName) { CheckCancellation(); }
        public void PostNewItem(uint dwFlags, NativeFileOperation.IShellItem psiDestinationFolder, string pszNewName, string pszTemplateName, uint dwFileAttributes, int hrNew, NativeFileOperation.IShellItem psiNewItem) { }

        public void UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
        {
            CheckCancellation();

            if (_progress == null)
                return;

            double fraction = iWorkTotal > 0 ? (double)iWorkSoFar / iWorkTotal : 0;
            _bytesTransferred = (long)(fraction * _totalBytesToTransfer);

            if ((DateTime.Now - _lastUpdate).TotalMilliseconds < 250 && iWorkSoFar < iWorkTotal)
                return;

            _lastUpdate = DateTime.Now;

            int percentage = (int)(fraction * 100);
            percentage = Math.Clamp(percentage, 0, 100);

            double speed = 0;
            TimeSpan eta = TimeSpan.Zero;

            if (_stopwatch.Elapsed.TotalSeconds > 0)
            {
                double bytesPerSec = _bytesTransferred / _stopwatch.Elapsed.TotalSeconds;
                speed = bytesPerSec / (1024 * 1024); // MB/s

                if (bytesPerSec > 0 && _totalBytesToTransfer > _bytesTransferred)
                {
                    double secondsRemaining = (_totalBytesToTransfer - _bytesTransferred) / bytesPerSec;
                    if (secondsRemaining < 99 * 3600)
                    {
                        eta = TimeSpan.FromSeconds(secondsRemaining);
                    }
                }
            }

            _progress.Report(new TransferProgress(
                Percentage: percentage,
                SpeedMegabytesPerSecond: speed,
                EstimatedTimeRemaining: eta,
                CurrentItemName: _currentItemName,
                BytesTransferred: _bytesTransferred,
                TotalBytesToTransfer: _totalBytesToTransfer
            ));
        }

        public void ResetTimer() { _stopwatch.Restart(); }
        public void PauseTimer() { _stopwatch.Stop(); }
        public void ResumeTimer() { _stopwatch.Start(); }

        private void CheckCancellation()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                AnyOperationsAborted = true;
                throw new OperationCanceledException("Transfer cancelled by user.");
            }
        }
    }
}
