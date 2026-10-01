using Easy_Copier.Models;
using System;
using System.Collections.Generic;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for evaluating completed drive transfer batches, executing audio alerts, and presenting desktop notifications.
    /// </summary>
    public interface IBatchNotificationService
    {
        /// <summary>
        /// Evaluates completed batch status for a target drive letter and triggers configured audio alerts and desktop toast notifications.
        /// </summary>
        /// <param name="driveLetter">The target drive letter identifier.</param>
        /// <param name="queueItems">The current collection of transfer queue items.</param>
        void EvaluateAndNotifyBatchCompletion(string driveLetter, IEnumerable<TransferQueueItem> queueItems);

        /// <summary>
        /// Event raised when an entire drive transfer batch finishes processing, providing title, body, and success status details.
        /// </summary>
        event EventHandler<(string Title, string Message, bool IsSuccess)>? BatchCompleted;
    }
}
