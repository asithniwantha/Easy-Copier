using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for checking, downloading, and applying application updates via Velopack.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Asynchronously checks if a newer version of the application is available from the update server or release releases feed.
        /// </summary>
        /// <returns>A task returning <c>true</c> if an update is available; otherwise, <c>false</c>.</returns>
        Task<bool> CheckForUpdatesAsync();

        /// <summary>
        /// Asynchronously downloads the latest available application update package.
        /// </summary>
        /// <returns>A task representing the download operation.</returns>
        Task DownloadUpdateAsync();

        /// <summary>
        /// Restarts the application and applies the downloaded update package.
        /// </summary>
        void RestartAndApplyUpdate();
    }
}
