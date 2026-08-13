using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Easy_Copier.Services
{
    public interface IStartupService
    {
        void UpdateStartOnLogon(bool enable);
    }

    public class StartupService(ILogger<StartupService> logger) : IStartupService
    {
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "EasyCopier";
        private readonly ILogger<StartupService> _logger = logger;

        public void UpdateStartOnLogon(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
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
