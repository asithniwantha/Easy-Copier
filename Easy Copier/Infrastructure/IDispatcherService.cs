using System;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    public interface IDispatcherService
    {
        bool HasThreadAccess { get; }
        void TryEnqueue(Action action);
        void TryEnqueue(Func<Task> action);
    }
}
