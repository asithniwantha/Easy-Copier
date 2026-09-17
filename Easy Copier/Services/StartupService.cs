using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for configuring Windows startup behavior on user logon.
    /// </summary>
    public interface IStartupService
    {
        /// <summary>
        /// Registers or unregisters the application to run automatically upon Windows user logon.
        /// </summary>
        /// <param name="enable"><c>true</c> to enable launch at logon; <c>false</c> to disable.</param>
        void UpdateStartOnLogon(bool enable);
    }

    /// <summary>
    /// Service for adding or removing the application's executable path in the Windows CurrentUser Run registry key.
    /// </summary>
    /// <param name="logger">The logger instance for operational output.</param>
    public class StartupService(ILogger<StartupService> logger) : IStartupService
    {
        /// <summary>
        /// Registry subkey path for current user auto-run entries.
        /// </summary>
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Application name value name stored in the Windows registry.
        /// </summary>
        private const string AppName = "EasyCopier";

        /// <summary>
        /// Logger instance used for diagnostic logging.
        /// </summary>
        private readonly ILogger<StartupService> _logger = logger;

        /// <summary>
        /// Sets or removes the Windows registry entry under <c>HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run</c> to start application at logon.
        /// </summary>
        /// <param name="enable"><c>true</c> to add registry value; <c>false</c> to delete registry value.</param>
        public void UpdateStartOnLogon(bool enable)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null)
                {
                    _logger.LogWarning("Failed to open Registry Run key.");
                    return;
                }

                if (enable)
                {
                    string executablePath = Environment.ProcessPath ?? string.Empty;
                    if (string.IsNullOrEmpty(executablePath))
                    {
                        _logger.LogWarning("Failed to determine application path for startup registration.");
                        return;
                    }

                    // Add quotes around the path to handle spaces
                    string launchCommand = $"\"{executablePath}\"";

                    key.SetValue(AppName, launchCommand);
                    _logger.LogInformation("Added {AppName} to startup registry with path: {LaunchCommand}", AppName, launchCommand);
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                        _logger.LogInformation("Removed {AppName} from startup registry.", AppName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating startup registry key.");
            }
        }
    }
}
