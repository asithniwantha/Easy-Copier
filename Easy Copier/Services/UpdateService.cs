using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Easy_Copier.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly UpdateManager? _updateManager;
        private UpdateInfo? _updateInfo;

        public UpdateService(ILogger<UpdateService> logger)
        {
            _logger = logger;
            try
            {
                var source = new GithubSource("https://github.com/asithniwantha/Easy-Copier", null, false);
                _updateManager = new UpdateManager(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Velopack UpdateManager.");
            }
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            if (_updateManager == null || !_updateManager.IsInstalled)
            {
                _logger.LogInformation("Update check skipped: Application is not installed via Velopack.");
                return false;
            }

            try
            {
                _updateInfo = await _updateManager.CheckForUpdatesAsync();
                return _updateInfo != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking for updates.");
                return false;
            }
        }

        public async Task DownloadUpdateAsync()
        {
            if (_updateManager == null || _updateInfo == null)
            {
                _logger.LogWarning("Download skipped: UpdateManager not initialized or no update available.");
                return;
            }

            try
            {
                await _updateManager.DownloadUpdatesAsync(_updateInfo);
                _logger.LogInformation("Update downloaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while downloading update.");
            }
        }

        public void RestartAndApplyUpdate()
        {
            if (_updateManager == null || _updateInfo == null)
            {
                return;
            }

            try
            {
                _updateManager.ApplyUpdatesAndRestart(_updateInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while applying update and restarting.");
            }
        }
    }
}
