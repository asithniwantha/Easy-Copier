using Easy_Copier.Models;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    public interface IDialogService
    {
        Task<(CopyAction Action, bool ApplyToAll)> ShowConflictDialogAsync(string itemName, long srcSize, int srcCount, long destSize, int destCount);
    }
}
