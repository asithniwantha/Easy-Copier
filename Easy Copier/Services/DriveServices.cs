using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface IDriveDiscoveryService : IDisposable
    {
        Task<IReadOnlyList<RemovableDrive>> GetRemovableDrivesAsync();
        event EventHandler? DrivesChanged;
        void StartWatching();
        void StopWatching();
    }

    public sealed class DriveDiscoveryService : IDriveDiscoveryService
    {
        private readonly ILogger<DriveDiscoveryService> _logger;
        private ManagementEventWatcher? _driveWatcher;
        private bool _isWatching;

        public event EventHandler? DrivesChanged;

        public DriveDiscoveryService(ILogger<DriveDiscoveryService> logger)
        {
            _logger = logger;
        }

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

        private (string? Model, bool IsUsb) GetPhysicalDiskInfo(string driveLetterWithColon)
        {
            try
            {
                string escapedLetter = driveLetterWithColon.Replace("\\", "\\\\");

                using ManagementObjectSearcher partitionSearcher = new(
                    $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{escapedLetter}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");

                foreach (ManagementObject partition in partitionSearcher.Get())
                {
                    string? partitionDeviceId = partition["DeviceID"]?.ToString();
                    if (string.IsNullOrEmpty(partitionDeviceId))
                    {
                        continue;
                    }

                    string escapedPartitionId = partitionDeviceId.Replace("\\", "\\\\");

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

        // BusType 7 = USB per MSFT_PhysicalDisk documentation.
        private const ushort UsbBusType = 7;

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
                    ushort busType = Convert.ToUInt16(physicalDisk["BusType"]);
                    return busType == UsbBusType;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error querying MSFT_PhysicalDisk BusType for disk index {Index}", diskIndex);
            }

            return false;
        }

        public void StartWatching()
        {
            if (_isWatching)
            {
                return;
            }

            try
            {
                WqlEventQuery query = new("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3");
                _driveWatcher = new ManagementEventWatcher(query);
                _driveWatcher.EventArrived += OnDriveChanged;
                _driveWatcher.Start();
                _isWatching = true;

                _logger.LogInformation("Drive watcher started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start drive watcher");
            }
        }

        public void StopWatching()
        {
            if (!_isWatching || _driveWatcher == null)
            {
                return;
            }

            try
            {
                _driveWatcher.Stop();
                _driveWatcher.EventArrived -= OnDriveChanged;
                _driveWatcher.Dispose();
                _driveWatcher = null;
                _isWatching = false;

                _logger.LogInformation("Drive watcher stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping drive watcher");
            }
        }

        private void OnDriveChanged(object sender, EventArrivedEventArgs e)
        {
            _logger.LogInformation("Drive change detected");
            DrivesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            StopWatching();
        }
    }

    public interface IDriveValidationService
    {
        Task<IReadOnlyList<ValidationResult>> ValidateTransferAsync(
            IEnumerable<GameEntry> games,
            RemovableDrive targetDrive,
            string destinationBasePath);
    }

    public class DriveValidationService : IDriveValidationService
    {
        private readonly ILogger<DriveValidationService> _logger;

        public DriveValidationService(ILogger<DriveValidationService> logger)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyList<ValidationResult>> ValidateTransferAsync(
            IEnumerable<GameEntry> games,
            RemovableDrive targetDrive,
            string destinationBasePath)
        {
            return await Task.Run(() =>
            {
                List<ValidationResult> results = [];
                List<GameEntry> gamesList = games.ToList();

                if (!gamesList.Any())
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
                    if (gamesWithLargeFiles.Any())
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
