using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for detecting, enumerating, and monitoring removable and USB drives.
    /// </summary>
    public interface IDriveDiscoveryService : IDisposable
    {
        /// <summary>
        /// Asynchronously queries and returns a list of connected removable and USB drives.
        /// </summary>
        /// <returns>A task returning a read-only list of <see cref="RemovableDrive"/> objects.</returns>
        Task<IReadOnlyList<RemovableDrive>> GetRemovableDrivesAsync();

        /// <summary>
        /// Occurs when a volume change event is detected (e.g., drive insertion or removal).
        /// </summary>
        event EventHandler? DrivesChanged;

        /// <summary>
        /// Starts watching for hardware volume change events via WMI.
        /// </summary>
        void StartWatching();

        /// <summary>
        /// Stops watching for hardware volume change events.
        /// </summary>
        void StopWatching();
    }

    /// <summary>
    /// Provides physical and logical drive detection and real-time drive insertion/removal monitoring via WMI.
    /// </summary>
    public sealed class DriveDiscoveryService : IDriveDiscoveryService
    {
        /// <summary>
        /// Logger instance used for recording drive discovery and monitoring events.
        /// </summary>
        private readonly ILogger<DriveDiscoveryService> _logger;

        /// <summary>
        /// Lock object ensuring thread-safe access to WMI watcher instantiation and teardown.
        /// </summary>
        private readonly object _watcherLock = new();

        /// <summary>
        /// Signal used to ensure in-flight WMI callback events finish prior to stopping or disposing.
        /// </summary>
        private readonly ManualResetEventSlim _driveChangeCallbacksCompleted = new(initialState: true);

        /// <summary>
        /// WMI event watcher monitoring <c>Win32_VolumeChangeEvent</c> events.
        /// </summary>
        private ManagementEventWatcher? _driveWatcher;

        /// <summary>
        /// Count of active, concurrently executing drive change callback handlers.
        /// </summary>
        private int _activeDriveChangeCallbacks;

        /// <summary>
        /// Atomic flag indicating whether active WMI event monitoring is enabled (1) or disabled (0).
        /// </summary>
        private int _isWatching;

        /// <summary>
        /// Atomic flag indicating whether this service instance has been disposed (1) or not (0).
        /// </summary>
        private int _isDisposed;

        /// <summary>
        /// Event raised when a system volume insertion or removal is detected.
        /// </summary>
        public event EventHandler? DrivesChanged;

        /// <summary>
        /// Constant value representing BusType 7 (USB) according to MSFT_PhysicalDisk documentation.
        /// </summary>
        private const ushort UsbBusType = 7;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriveDiscoveryService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used for recording drive service events.</param>
        public DriveDiscoveryService(ILogger<DriveDiscoveryService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously retrieves all connected drives that are classified as removable or USB-connected storage.
        /// </summary>
        /// <returns>A task that returns a list of <see cref="RemovableDrive"/> instances.</returns>
        public async Task<IReadOnlyList<RemovableDrive>> GetRemovableDrivesAsync()
        {
            return await Task.Run(() =>
            {
                List<RemovableDrive> removableDrives = [];

                try
                {
                    DriveInfo[] allDrives = DriveInfo.GetDrives();

                    foreach (DriveInfo drive in allDrives)
                    {
                        try
                        {
                            if (!drive.IsReady)
                            {
                                continue;
                            }

                            string driveLetterWithColon = drive.Name.TrimEnd('\\');
                            (string? Model, bool IsUsb) = GetPhysicalDiskInfo(driveLetterWithColon);

                            // Include drives Windows already flags as Removable (USB flash drives),
                            // plus Fixed drives that are actually connected via USB (e.g. portable
                            // hard drives/SSDs, which Windows often reports as "Fixed").
                            bool isEligible = drive.DriveType == DriveType.Removable
                                || (drive.DriveType == DriveType.Fixed && IsUsb);

                            if (!isEligible)
                            {
                                continue;
                            }

                            long usedBytes = drive.TotalSize - drive.AvailableFreeSpace;
                            double usedPercentage = drive.TotalSize > 0
                                ? (double)usedBytes / drive.TotalSize * 100
                                : 0;

                            string brand = Model ?? "Unknown";

                            RemovableDrive removableDrive = new(
                                driveLetterWithColon,
                                string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Removable Drive" : drive.VolumeLabel,
                                drive.DriveFormat,
                                drive.TotalSize,
                                drive.AvailableFreeSpace,
                                usedPercentage,
                                brand);

                            removableDrives.Add(removableDrive);

                            _logger.LogInformation(
                                "Found removable drive: {Letter} ({Label}), {Format}, {Brand}, {Free} free of {Total}",
                                drive.Name, drive.VolumeLabel, drive.DriveFormat, brand,
                                drive.AvailableFreeSpace, drive.TotalSize);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error reading drive: {Name}", drive.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enumerating drives");
                }

                return removableDrives;
            });
        }

        /// <summary>
        /// Queries WMI to retrieve physical disk model information and determine if the drive is connected via USB.
        /// </summary>
        /// <param name="driveLetterWithColon">The drive letter identifier (e.g., "E:").</param>
        /// <returns>A tuple containing the hardware model name (if found) and a boolean indicating whether it is connected via USB.</returns>
        private (string? Model, bool IsUsb) GetPhysicalDiskInfo(string driveLetterWithColon)
        {
            try
            {
                string escapedLetter = driveLetterWithColon.Replace("\\", "\\\\", StringComparison.Ordinal);

                using ManagementObjectSearcher partitionSearcher = new(
                    $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{escapedLetter}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");

                foreach (ManagementObject partition in partitionSearcher.Get())
                {
                    string? partitionDeviceId = partition["DeviceID"]?.ToString();
                    if (string.IsNullOrEmpty(partitionDeviceId))
                    {
                        continue;
                    }

                    string escapedPartitionId = partitionDeviceId.Replace("\\", "\\\\", StringComparison.Ordinal);

                    using ManagementObjectSearcher diskSearcher = new(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{escapedPartitionId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");

                    foreach (ManagementObject disk in diskSearcher.Get())
                    {
                        string? model = disk["Model"]?.ToString();
                        string? interfaceType = disk["InterfaceType"]?.ToString();
                        bool isUsb = string.Equals(interfaceType, "USB", StringComparison.OrdinalIgnoreCase);

                        // Some USB enclosures (especially UASP-capable NVMe/SSD bridges) report
                        // InterfaceType as "SCSI" instead of "USB". Fall back to querying the
                        // storage subsystem's BusType, which correctly identifies these as USB.
                        if (!isUsb && disk["Index"] != null)
                        {
                            isUsb = IsUsbBusType(disk["Index"].ToString());
                        }

                        return (string.IsNullOrWhiteSpace(model) ? null : model.Trim(), isUsb);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error querying physical disk info for {Letter}", driveLetterWithColon);
            }

            return (null, false);
        }

        /// <summary>
        /// Queries the MSFT_PhysicalDisk WMI class in the root\Microsoft\Windows\Storage namespace to verify BusType.
        /// </summary>
        /// <param name="diskIndex">The physical disk device index string.</param>
        /// <returns><c>true</c> if the physical disk BusType is 7 (USB); otherwise, <c>false</c>.</returns>
        private bool IsUsbBusType(string? diskIndex)
        {
            if (string.IsNullOrEmpty(diskIndex))
            {
                return false;
            }

            try
            {
                ManagementScope scope = new(@"root\Microsoft\Windows\Storage");
                scope.Connect();

                using ManagementObjectSearcher searcher = new(
                    scope,
                    new ObjectQuery($"SELECT BusType FROM MSFT_PhysicalDisk WHERE DeviceId = '{diskIndex}'"));

                foreach (ManagementObject physicalDisk in searcher.Get())
                {
                    ushort busType = Convert.ToUInt16(physicalDisk["BusType"], System.Globalization.CultureInfo.InvariantCulture);
                    return busType == UsbBusType;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error querying MSFT_PhysicalDisk BusType for disk index {Index}", diskIndex);
            }

            return false;
        }

        /// <summary>
        /// Starts the WMI event listener for Windows volume change events.
        /// </summary>
        public void StartWatching()
        {
            lock (_watcherLock)
            {
                if (_isWatching != 0 || _isDisposed != 0)
                {
                    return;
                }

                try
                {
                    WqlEventQuery query = new("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3");
                    _driveWatcher = new ManagementEventWatcher(query);
                    _driveWatcher.EventArrived += OnDriveChanged;
                    _driveWatcher.Start();
                    _isWatching = 1;

                    _logger.LogInformation("Drive watcher started");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start drive watcher");
                }
            }
        }

        /// <summary>
        /// Stops the WMI event listener and cleans up watcher instances safely.
        /// </summary>
        public void StopWatching()
        {
            ManagementEventWatcher? watcherToDispose;

            lock (_watcherLock)
            {
                if (_isWatching == 0 || _driveWatcher == null)
                {
                    return;
                }

                watcherToDispose = _driveWatcher;
                _driveWatcher = null;
                _isWatching = 0;
                watcherToDispose.EventArrived -= OnDriveChanged;
            }

            try
            {
                watcherToDispose.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping drive watcher");
            }
            finally
            {
                WaitForPendingDriveNotifications();
                watcherToDispose.Dispose();
                _logger.LogInformation("Drive watcher stopped");
            }
        }

        /// <summary>
        /// Callback handler triggered when WMI delivers a volume change event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event details containing WMI event parameters.</param>
        private void OnDriveChanged(object sender, EventArrivedEventArgs e)
        {
            if (Volatile.Read(ref _isWatching) == 0 || Volatile.Read(ref _isDisposed) != 0)
            {
                return;
            }

            if (Interlocked.Increment(ref _activeDriveChangeCallbacks) == 1)
            {
                _driveChangeCallbacksCompleted.Reset();
            }

            try
            {
                if (Volatile.Read(ref _isWatching) == 0 || Volatile.Read(ref _isDisposed) != 0)
                {
                    return;
                }

                _logger.LogInformation("Drive change detected");
                DrivesChanged?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                if (Interlocked.Decrement(ref _activeDriveChangeCallbacks) == 0)
                {
                    _driveChangeCallbacksCompleted.Set();
                }
            }
        }

        /// <summary>
        /// Blocks until all currently running WMI event arrival callbacks have completed or until a timeout occurs.
        /// </summary>
        private void WaitForPendingDriveNotifications()
        {
            // WMI can still deliver a callback that was already queued when Stop() ran.
            // Waiting briefly avoids tearing down the watcher while that callback unwinds.
            if (!_driveChangeCallbacksCompleted.Wait(TimeSpan.FromSeconds(2)))
            {
                _logger.LogWarning("Timed out waiting for pending drive watcher callbacks to finish.");
            }
        }

        /// <summary>
        /// Disposes unmanaged WMI resources and terminates drive monitoring handlers.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            StopWatching();
            _driveChangeCallbacksCompleted.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Defines operations for validating file transfer operations prior to execution.
    /// </summary>
    public interface IDriveValidationService
    {
        /// <summary>
        /// Validates whether the selected games can be safely copied to the specified target drive and path.
        /// </summary>
        /// <param name="games">The collection of game entries to copy.</param>
        /// <param name="targetDrive">The target drive information.</param>
        /// <param name="destinationBasePath">The destination root directory path.</param>
        /// <returns>A task returning a read-only list of <see cref="ValidationResult"/> objects.</returns>
        Task<IReadOnlyList<ValidationResult>> ValidateTransferAsync(
            IEnumerable<GameEntry> games,
            RemovableDrive targetDrive,
            string destinationBasePath);
    }

    /// <summary>
    /// Validates transfer requirements such as available drive space, FAT32 4GB file limitations, source directory availability, and existing destination folders.
    /// </summary>
    public class DriveValidationService : IDriveValidationService
    {
        /// <summary>
        /// Logger instance used for recording validation operations.
        /// </summary>
        private readonly ILogger<DriveValidationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriveValidationService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for validation logs.</param>
        public DriveValidationService(ILogger<DriveValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously validates selected games against target drive storage capacity, file system limitations, and path accessibility.
        /// </summary>
        /// <param name="games">The games selected for copy.</param>
        /// <param name="targetDrive">The target removable drive.</param>
        /// <param name="destinationBasePath">The destination base folder path on the drive.</param>
        /// <returns>A list of <see cref="ValidationResult"/> entries describing warnings, errors, or success status.</returns>
        public async Task<IReadOnlyList<ValidationResult>> ValidateTransferAsync(
            IEnumerable<GameEntry> games,
            RemovableDrive targetDrive,
            string destinationBasePath)
        {
            return await Task.Run(() =>
            {
                List<ValidationResult> results = [];
                List<GameEntry> gamesList = games.ToList();

                if (gamesList.Count == 0)
                {
                    results.Add(new ValidationResult(false, ValidationSeverity.Error, "No games selected"));
                    return results;
                }

                long totalRequiredBytes = gamesList.Sum(g => g.TotalBytes);

                if (totalRequiredBytes > targetDrive.FreeBytes)
                {
                    results.Add(new ValidationResult(
                        false,
                        ValidationSeverity.Error,
                        $"Insufficient space: Need {Infrastructure.FormattingHelpers.FormatBytes(totalRequiredBytes)}, available {Infrastructure.FormattingHelpers.FormatBytes(targetDrive.FreeBytes)}"));
                }

                if (targetDrive.IsFat32)
                {
                    List<GameEntry> gamesWithLargeFiles = gamesList.Where(g => g.HasLargeFiles).ToList();
                    if (gamesWithLargeFiles.Count > 0)
                    {
                        results.Add(new ValidationResult(
                            false,
                            ValidationSeverity.Error,
                            $"FAT32 drive cannot store files >4GB. {gamesWithLargeFiles.Count} game(s) affected: {string.Join(", ", gamesWithLargeFiles.Select(g => g.Name))}"));
                    }
                }

                if (Directory.Exists(destinationBasePath))
                {
                    foreach (GameEntry game in gamesList)
                    {
                        string destPath = Path.Combine(destinationBasePath, game.Name);
                        if (Directory.Exists(destPath))
                        {
                            results.Add(new ValidationResult(
                                true,
                                ValidationSeverity.Warning,
                                $"'{game.Name}' already exists at destination and will be merged/overwritten"));
                        }
                    }
                }

                foreach (GameEntry game in gamesList)
                {
                    if (!Directory.Exists(game.FolderPath))
                    {
                        results.Add(new ValidationResult(
                            false,
                            ValidationSeverity.Error,
                            $"Source not accessible: {game.Name}"));
                    }
                }

                if (results.All(r => r.Severity != ValidationSeverity.Error))
                {
                    results.Insert(0, new ValidationResult(
                        true,
                        ValidationSeverity.Info,
                        $"Ready to copy {gamesList.Count} game(s) ({Infrastructure.FormattingHelpers.FormatBytes(totalRequiredBytes)})"));
                }

                return results;
            });
        }
    }
}
