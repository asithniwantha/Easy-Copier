using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface IUpdateService
    {
        Task<bool> CheckForUpdatesAsync();
        Task DownloadUpdateAsync();
        void RestartAndApplyUpdate();
    }
}
