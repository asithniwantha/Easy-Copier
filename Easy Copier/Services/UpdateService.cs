using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Easy_Copier.Services
{
    public partial class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly UpdateManager? _updateManager;
        private UpdateInfo? _updateInfo;

        public UpdateService(ILogger<UpdateService> logger)
        {
            _logger = logger;
            try
            {
                GithubSource source = new("https://github.com/asithniwantha/Easy-Copier", null, false);
                _updateManager = new UpdateManager(source);
            }
            catch (Exception ex)
            {
                LogUpdateManagerInitializationFailed(logger, ex);
            }
        }

        private string GetCurrentVersion()
        {
            return _updateManager != null && _updateManager.IsInstalled && _updateManager.CurrentVersion != null
                ? _updateManager.CurrentVersion.ToString()
                : Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            if (_updateManager == null || !_updateManager.IsInstalled)
            {
                LogUpdateCheckSkippedNotInstalled(_logger);
                return false;
            }

            string currentVersion = GetCurrentVersion();

            try
            {
                LogCheckingForUpdates(_logger, currentVersion);
                _updateInfo = await _updateManager.CheckForUpdatesAsync();

                if (_updateInfo != null)
                {
                    string targetVersion = _updateInfo.TargetFullRelease.Version.ToString();
                    LogUpdateFound(_logger, currentVersion, targetVersion);
                    return true;
                }
                else
                {
                    LogNoUpdateAvailable(_logger, currentVersion);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogCheckForUpdatesError(_logger, ex);
                return false;
            }
        }

        public async Task DownloadUpdateAsync()
        {
            if (_updateManager == null || _updateInfo == null)
            {
                LogDownloadSkipped(_logger);
                return;
            }

            string targetVersion = _updateInfo.TargetFullRelease.Version.ToString();

            try
            {
                LogDownloadStart(_logger, targetVersion);
                await _updateManager.DownloadUpdatesAsync(_updateInfo);
                LogDownloadSuccess(_logger, targetVersion);
            }
            catch (Exception ex)
            {
                LogDownloadError(_logger, ex, targetVersion);
            }
        }

        public void RestartAndApplyUpdate()
        {
            if (_updateManager == null || _updateInfo == null)
            {
                return;
            }

            string targetVersion = _updateInfo.TargetFullRelease.Version.ToString();

            try
            {
                LogApplyUpdateStart(_logger, targetVersion);
                _updateManager.ApplyUpdatesAndRestart(_updateInfo);
            }
            catch (Exception ex)
            {
                LogApplyUpdateError(_logger, ex, targetVersion);
            }
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to initialize Velopack UpdateManager.")]
        private static partial void LogUpdateManagerInitializationFailed(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Update check skipped: Application is not installed via Velopack.")]
        private static partial void LogUpdateCheckSkippedNotInstalled(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Checking for updates via Velopack... Local version: {LocalVersion}")]
        private static partial void LogCheckingForUpdates(ILogger logger, string localVersion);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Update found. Local version: {LocalVersion}, Remote version: {RemoteVersion}")]
        private static partial void LogUpdateFound(ILogger logger, string localVersion, string remoteVersion);

        [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "No update available. Current local version ({LocalVersion}) is up to date with remote.")]
        private static partial void LogNoUpdateAvailable(ILogger logger, string localVersion);

        [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error while checking for updates.")]
        private static partial void LogCheckForUpdatesError(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Download skipped: UpdateManager not initialized or no update available.")]
        private static partial void LogDownloadSkipped(ILogger logger);

        [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Starting download for update version {TargetVersion}.")]
        private static partial void LogDownloadStart(ILogger logger, string targetVersion);

        [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Update version {TargetVersion} downloaded successfully.")]
        private static partial void LogDownloadSuccess(ILogger logger, string targetVersion);

        [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Error while downloading update version {TargetVersion}.")]
        private static partial void LogDownloadError(ILogger logger, Exception ex, string targetVersion);

        [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Applying update version {TargetVersion} and restarting.")]
        private static partial void LogApplyUpdateStart(ILogger logger, string targetVersion);

        [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error while applying update version {TargetVersion} and restarting.")]
        private static partial void LogApplyUpdateError(ILogger logger, Exception ex, string targetVersion);
    }
}
