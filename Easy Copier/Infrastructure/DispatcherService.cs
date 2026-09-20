using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Manages execution of delegates on the WinUI 3 UI thread dispatcher queue.
    /// </summary>
    public class DispatcherService : IDispatcherService
    {
        private readonly DispatcherQueue _dispatcherQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherService"/> class bound to the current thread's dispatcher.
        /// </summary>
        public DispatcherService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        /// <summary>
        /// Gets a value indicating whether the caller is running on the UI thread.
        /// </summary>
        public bool HasThreadAccess => _dispatcherQueue?.HasThreadAccess ?? false;

        /// <summary>
        /// Enqueues a synchronous action to be executed on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns><c>true</c> if the action was successfully enqueued or executed directly; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        public bool TryEnqueue(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (_dispatcherQueue != null)
            {
                return _dispatcherQueue.TryEnqueue(() => action());
            }
            else
            {
                action();
                return true;
            }
        }

        /// <summary>
        /// Enqueues an asynchronous task delegate to be executed on the UI thread.
        /// </summary>
        /// <param name="action">The asynchronous task delegate to execute.</param>
        /// <returns><c>true</c> if the task was successfully enqueued or started directly; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        public bool TryEnqueue(Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (_dispatcherQueue != null)
            {
                return _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await action();
                    }
                    catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        // Ignore expected cancellation exceptions on the dispatcher to prevent application crash during shutdown
                    }
                });
            }
            else
            {
                _ = action();
                return true;
            }
        }
    }
}
