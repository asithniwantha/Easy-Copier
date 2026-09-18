using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service managing automatic application update checks, package downloads, and updates application execution using Velopack.
    /// </summary>
    public partial class UpdateService : IUpdateService
    {
        /// <summary>
        /// Logger instance used for diagnostic logging.
        /// </summary>
        private readonly ILogger<UpdateService> _logger;

        /// <summary>
        /// Velopack update manager instance connected to the GitHub releases feed.
        /// </summary>
        private readonly UpdateManager? _updateManager;

        /// <summary>
        /// Currently detected update package info.
        /// </summary>
        private UpdateInfo? _updateInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateService"/> class and sets up Velopack update source.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
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

        /// <summary>
        /// Gets the current installed version string from Velopack or assembly reflection.
        /// </summary>
        /// <returns>The version string.</returns>
        private string GetCurrentVersion()
        {
            return _updateManager != null && _updateManager.IsInstalled && _updateManager.CurrentVersion != null
                ? _updateManager.CurrentVersion.ToString()
                : Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Asynchronously checks if a new version release is available on GitHub via Velopack.
        /// </summary>
        /// <returns>A task returning <c>true</c> if a newer update package is available; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Asynchronously downloads the latest update package files.
        /// </summary>
        /// <returns>A task representing the download operation.</returns>
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

        /// <summary>
        /// Applies the downloaded update and restarts the application.
        /// </summary>
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

        /// <summary>
        /// Logger message for initialization failures.
        /// </summary>
        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to initialize Velopack UpdateManager.")]
        private static partial void LogUpdateManagerInitializationFailed(ILogger logger, Exception ex);

        /// <summary>
        /// Logger message when update check is skipped because app is not installed via Velopack.
        /// </summary>
        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Update check skipped: Application is not installed via Velopack.")]
        private static partial void LogUpdateCheckSkippedNotInstalled(ILogger logger);

        /// <summary>
        /// Logger message when starting update check.
        /// </summary>
        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Checking for updates via Velopack... Local version: {LocalVersion}")]
        private static partial void LogCheckingForUpdates(ILogger logger, string localVersion);

        /// <summary>
        /// Logger message when a new update is found.
        /// </summary>
        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Update found. Local version: {LocalVersion}, Remote version: {RemoteVersion}")]
        private static partial void LogUpdateFound(ILogger logger, string localVersion, string remoteVersion);

        /// <summary>
        /// Logger message when no update is available.
        /// </summary>
        [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "No update available. Current local version ({LocalVersion}) is up to date with remote.")]
        private static partial void LogNoUpdateAvailable(ILogger logger, string localVersion);

        /// <summary>
        /// Logger message when error occurs during update check.
        /// </summary>
        [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error while checking for updates.")]
        private static partial void LogCheckForUpdatesError(ILogger logger, Exception ex);

        /// <summary>
        /// Logger message when download is skipped.
        /// </summary>
        [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Download skipped: UpdateManager not initialized or no update available.")]
        private static partial void LogDownloadSkipped(ILogger logger);

        /// <summary>
        /// Logger message when update download starts.
        /// </summary>
        [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Starting download for update version {TargetVersion}.")]
        private static partial void LogDownloadStart(ILogger logger, string targetVersion);

        /// <summary>
        /// Logger message when update download succeeds.
        /// </summary>
        [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Update version {TargetVersion} downloaded successfully.")]
        private static partial void LogDownloadSuccess(ILogger logger, string targetVersion);

        /// <summary>
        /// Logger message when update download fails.
        /// </summary>
        [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Error while downloading update version {TargetVersion}.")]
        private static partial void LogDownloadError(ILogger logger, Exception ex, string targetVersion);

        /// <summary>
        /// Logger message when applying update and restarting.
        /// </summary>
        [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Applying update version {TargetVersion} and restarting.")]
        private static partial void LogApplyUpdateStart(ILogger logger, string targetVersion);

        /// <summary>
        /// Logger message when apply update fails.
        /// </summary>
        [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error while applying update version {TargetVersion} and restarting.")]
        private static partial void LogApplyUpdateError(ILogger logger, Exception ex, string targetVersion);
    }
}
