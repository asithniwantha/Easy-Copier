using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Encapsulates evaluating completed drive transfer batches, executing audio alerts, and formatting/displaying Windows desktop toast notifications.
    /// </summary>
    public class BatchNotificationService : IBatchNotificationService
    {
        /// <summary>
        /// The settings service used to load application notification preferences.
        /// </summary>
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// The audio playback service used to play audio alert sound effects.
        /// </summary>
        private readonly IAudioPlaybackService _audioPlaybackService;

        /// <summary>
        /// The process service used to inspect process privilege levels.
        /// </summary>
        private readonly IProcessService _processService;

        /// <summary>
        /// The logger instance for diagnostic output.
        /// </summary>
        private readonly ILogger<BatchNotificationService> _logger;

        /// <inheritdoc />
        public event EventHandler<(string Title, string Message, bool IsSuccess)>? BatchCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchNotificationService"/> class.
        /// </summary>
        /// <param name="settingsService">The settings service for reading notification preferences.</param>
        /// <param name="audioPlaybackService">The audio playback service for playing success/failure sounds.</param>
        /// <param name="processService">The process service for checking process privilege levels.</param>
        /// <param name="logger">The logger instance for operational diagnostic output.</param>
        public BatchNotificationService(
            ISettingsService settingsService,
            IAudioPlaybackService audioPlaybackService,
            IProcessService processService,
            ILogger<BatchNotificationService> logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _audioPlaybackService = audioPlaybackService ?? throw new ArgumentNullException(nameof(audioPlaybackService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void EvaluateAndNotifyBatchCompletion(string driveLetter, IEnumerable<TransferQueueItem> queueItems)
        {
            ArgumentNullException.ThrowIfNull(queueItems);

            if (string.IsNullOrWhiteSpace(driveLetter))
            {
                return;
            }

            AppSettings settings = _settingsService.LoadSettingsSync();

            List<TransferQueueItem> itemsList = [.. queueItems];

            int activeItemsForDrive = itemsList.Count(i =>
                i.IsActive && string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase));

            if (activeItemsForDrive > 0)
            {
                return;
            }

            List<TransferQueueItem> batchItems = itemsList.Where(i =>
                !i.IsActive && string.Equals(i.TargetDrive.DriveLetter, driveLetter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (batchItems.Count == 0)
            {
                return;
            }

            bool anyFailedOrCancelled = batchItems.Any(i => i.Status is TransferQueueItemStatus.Failed or TransferQueueItemStatus.Cancelled);

            if (settings.PlayNotificationSounds)
            {
                if (anyFailedOrCancelled)
                {
                    _audioPlaybackService.PlayFailureSound();
                }
                else
                {
                    _audioPlaybackService.PlaySuccessSound();
                }
            }

            if (settings.ShowDesktopNotifications)
            {
                ShowDesktopNotification(driveLetter, batchItems, !anyFailedOrCancelled);
            }
        }

        /// <summary>
        /// Formats and displays a Windows desktop toast notification for a completed drive transfer batch.
        /// </summary>
        /// <param name="driveLetter">The drive letter identifier for the target volume.</param>
        /// <param name="batchItems">The collection of queue items in the completed transfer batch.</param>
        /// <param name="isSuccess"><see langword="true"/> if all items in the batch completed successfully; otherwise, <see langword="false"/>.</param>
        private void ShowDesktopNotification(string driveLetter, List<TransferQueueItem> batchItems, bool isSuccess)
        {
            try
            {
                TransferQueueItem firstItem = batchItems[0];
                long totalDriveCapacity = firstItem.TargetDrive.TotalBytes;
                string statusText = isSuccess ? "Complete" : "Failed";
                string title = $"{firstItem.TargetDrive.DriveLetter} - {FormattingHelpers.FormatBytes(totalDriveCapacity)} Capacity - {statusText}";

                long totalBytes = batchItems.Sum(x => x.TotalBytes);
                int totalPrice = batchItems.Sum(x => x.TotalPrice);
                List<string> allGames = batchItems.SelectMany(x => x.Items).Select(x => x.Game.Name).ToList();
                int totalItems = allGames.Count;

                string namesText = totalItems > 3
                    ? $"{totalItems} items: {string.Join(", ", allGames.Take(3))} and {totalItems - 3} more."
                    : $"{totalItems} items: {string.Join(", ", allGames)}.";
                string body = $"{namesText} Size: {FormattingHelpers.FormatBytes(totalBytes)}. Price: Rs. {totalPrice}";

                BatchCompleted?.Invoke(this, (title, body, isSuccess));

                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(body);

                _logger.LogInformation("Attempting to show desktop notification: {Title}", title);
                AppNotification notification = builder.BuildNotification();
                if (!AppNotificationManager.IsSupported())
                {
                    _logger.LogWarning("AppNotificationManager.IsSupported() returned false. Skipping toast display.");

                    if (_processService.IsRunningAsAdministrator())
                    {
                        _logger.LogWarning("Application is running as Administrator (elevated). Toast notifications are officially not supported by the Windows App SDK in elevated contexts.");
                    }
                    else
                    {
                        _logger.LogWarning("AppNotificationManager is not supported on this OS configuration, but the app is NOT elevated.");
                    }

                    return;
                }

                if (AppNotificationManager.Default.Setting == AppNotificationSetting.DisabledForApplication)
                {
                    _logger.LogWarning("Desktop notifications are disabled for this application by the user or system.");
                }
                else if (AppNotificationManager.Default.Setting == AppNotificationSetting.DisabledForUser)
                {
                    _logger.LogWarning("Desktop notifications are disabled globally for this user profile.");
                }
                else if (AppNotificationManager.Default.Setting == AppNotificationSetting.DisabledByGroupPolicy)
                {
                    _logger.LogWarning("Desktop notifications are disabled by Group Policy.");
                }
                else if (AppNotificationManager.Default.Setting == AppNotificationSetting.DisabledByManifest)
                {
                    _logger.LogWarning("Desktop notifications are disabled by the application manifest.");
                }

                AppNotificationManager.Default.Show(notification);

                if (notification.Id != 0)
                {
                    _logger.LogInformation("Desktop notification shown successfully with ID: {Id}", notification.Id);
                }
                else
                {
                    _logger.LogWarning("AppNotificationManager.Default.Show returned without throwing, but notification ID is 0.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show desktop notification.");
            }
        }
    }
}
