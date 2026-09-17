using System;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Abstracts dispatcher operations for executing code on the UI thread.
    /// </summary>
    public interface IDispatcherService
    {
        /// <summary>
        /// Gets a value indicating whether the caller has thread access to the UI thread.
        /// </summary>
        bool HasThreadAccess { get; }

        /// <summary>
        /// Enqueues a synchronous action to be executed on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns><c>true</c> if the action was enqueued or executed successfully; otherwise, <c>false</c>.</returns>
        bool TryEnqueue(Action action);

        /// <summary>
        /// Enqueues an asynchronous task delegate to be executed on the UI thread.
        /// </summary>
        /// <param name="action">The asynchronous task delegate to execute.</param>
        /// <returns><c>true</c> if the task was enqueued or executed successfully; otherwise, <c>false</c>.</returns>
        bool TryEnqueue(Func<Task> action);
    }
}
