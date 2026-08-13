using System;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    public interface IDispatcherService
    {
        bool HasThreadAccess { get; }
        bool TryEnqueue(Action action);
        bool TryEnqueue(Func<Task> action);
    }
}
