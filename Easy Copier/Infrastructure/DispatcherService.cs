using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    public class DispatcherService : IDispatcherService
    {
        private readonly DispatcherQueue _dispatcherQueue;

        public DispatcherService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public bool HasThreadAccess => _dispatcherQueue?.HasThreadAccess ?? false;

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

        public bool TryEnqueue(Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (_dispatcherQueue != null)
            {
                return _dispatcherQueue.TryEnqueue(async () => await action());
            }
            else
            {
                _ = action();
                return true;
            }
        }
    }
}
