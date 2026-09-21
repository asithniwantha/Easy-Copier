using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service responsible for discovering Rufus executable updates and launching ISO image files in Rufus.
    /// </summary>
    public class RufusService : IRufusService
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<RufusService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RufusService"/> class.
        /// </summary>
        /// <param name="settingsService">The application settings management service.</param>
        /// <param name="logger">The logger instance.</param>
        public RufusService(ISettingsService settingsService, ILogger<RufusService> logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<(bool Success, string Message)> LaunchWithIsoAsync(string isoPath)
        {
            if (string.IsNullOrWhiteSpace(isoPath))
            {
                return (false, "No ISO file specified.");
            }

            if (!File.Exists(isoPath))
            {
                return (false, $"ISO file not found: {isoPath}");
            }

            try
            {
                AppSettings settings = await _settingsService.LoadSettingsAsync();
                string latestRufusPath = RufusResolutionHelper.ResolveLatestRufusPath(settings.RufusExecutablePath);

                if (!string.Equals(latestRufusPath, settings.RufusExecutablePath, StringComparison.OrdinalIgnoreCase))
                {
                    settings.RufusExecutablePath = latestRufusPath;
                    await _settingsService.SaveSettingsAsync(settings);
                }

                if (!File.Exists(latestRufusPath))
                {
                    _logger.LogWarning("Rufus executable not found at resolved path: {RufusPath}", latestRufusPath);
                    return (false, $"Rufus executable not found at: {latestRufusPath}");
                }

                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = latestRufusPath,
                    Arguments = $"-i \"{isoPath}\"",
                    UseShellExecute = true
                });

                _logger.LogInformation("Successfully launched Rufus ({RufusPath}) with ISO: {IsoPath}", latestRufusPath, isoPath);
                return (true, $"Opened {Path.GetFileName(isoPath)} in Rufus.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch Rufus for ISO path: {IsoPath}", isoPath);
                return (false, $"Error opening Rufus: {ex.Message}");
            }
        }
    }
}
