using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Partial class implementation of <see cref="MainViewModel"/> providing software update checks, downloads, and lifecycle management.
    /// </summary>
    public sealed partial class MainViewModel
    {
        /// <summary>
        /// Restarts the application and applies any pending software update via the update service.
        /// </summary>
        [RelayCommand]
        private void RestartAndApplyUpdate()
        {
            _updateService.RestartAndApplyUpdate();
        }

        /// <summary>
        /// Checks for application updates in the background and optionally triggers automatic download if enabled.
        /// </summary>
        /// <returns>A task representing the asynchronous background update check.</returns>
        private async Task CheckForUpdatesBackgroundAsync()
        {
            // Atomically acquire the update lock (0 -> 1) to prevent concurrent update checks from timer ticks or triggers.
            if (IsDisposed || Interlocked.CompareExchange(ref _isCheckingForUpdates, 1, 0) != 0)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Checking for new updates in the background...");
                bool hasUpdate = await _updateService.CheckForUpdatesAsync();
                if (hasUpdate)
                {
                    _logger.LogInformation("A new update is available.");
                    AppSettings settings = await _settingsService.LoadSettingsAsync();
                    if (settings.AutoDownloadUpdates)
                    {
                        _logger.LogInformation("Automatic update download is enabled. Starting background download...");
                        _ = TryEnqueueIfActive(() =>
                        {
                            IsUpdateAvailable = true;
                            UpdateMessage = "Downloading update in background...";
                        });

                        await _updateService.DownloadUpdateAsync();

                        _logger.LogInformation("Background update download completed. Update is ready to install.");
                        _ = TryEnqueueIfActive(() =>
                        {
                            IsUpdateAvailable = false;
                            IsUpdateReadyToInstall = true;
                            UpdateMessage = "Update ready to install. Restart to apply.";
                        });
                    }
                    else
                    {
                        _logger.LogInformation("Automatic update download is disabled. Notifying user.");
                        _ = TryEnqueueIfActive(() =>
                        {
                            IsUpdateAvailable = true;
                            UpdateMessage = "A new update is available!";
                        });
                    }
                }
                else
                {
                    _logger.LogInformation("No new updates found in the background.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates in background");
                System.Diagnostics.Debug.WriteLine($"Error checking for updates in background: {ex}");
            }
            finally
            {
                // Release the update lock atomically.
                _ = Interlocked.Exchange(ref _isCheckingForUpdates, 0);
            }
        }

        /// <summary>
        /// Initiates manual download of an available application update.
        /// </summary>
        /// <returns>A task representing the asynchronous update download operation.</returns>
        [RelayCommand]
        private async Task DownloadUpdateAsync()
        {
            try
            {
                IsUpdateAvailable = true;
                UpdateMessage = "Downloading update...";

                await _updateService.DownloadUpdateAsync();

                IsUpdateAvailable = false;
                IsUpdateReadyToInstall = true;
                UpdateMessage = "Update ready to install. Restart to apply.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading update manually: {ex}");
                UpdateMessage = "Failed to download update.";
            }
        }
    }
}

