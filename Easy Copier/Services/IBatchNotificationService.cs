using Easy_Copier.Models;
using System;
using System.Collections.Generic;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Contract for managing audio playback and desktop toast notifications upon queue batch completion.
    /// </summary>
    public interface IBatchNotificationService
    {
        /// <summary>
        /// Evaluates a completed drive batch and dispatches appropriate sound effects and toast notifications.
        /// </summary>
        /// <param name="driveLetter">Target drive letter identifier.</param>
        /// <param name="batchItems">List of transfer queue items associated with the drive batch.</param>
        /// <param name="batchCompletedCallback">Optional callback to raise event notifications.</param>
        void NotifyBatchCompletion(
            string driveLetter,
            IReadOnlyList<TransferQueueItem> batchItems,
            Action<(string Title, string Message, bool IsSuccess)>? batchCompletedCallback = null);
    }
}
