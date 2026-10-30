using System;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides services for external process execution and environment security checks.
    /// </summary>
    public interface IProcessService
    {
        /// <summary>
        /// Opens Windows File Explorer to the specified directory or file path.
        /// </summary>
        /// <param name="path">The folder or file path to display.</param>
        void OpenInExplorer(string path);

        /// <summary>
        /// Checks whether the current application process is executing with Administrator privileges.
        /// </summary>
        /// <returns><c>true</c> if running as Administrator; otherwise, <c>false</c>.</returns>
        bool IsRunningAsAdministrator();

        /// <summary>
        /// Opens the native Windows Format dialog for the specified drive.
        /// </summary>
        /// <param name="driveLetter">The drive letter (e.g., "D:" or "D").</param>
        void OpenFormatDialog(string driveLetter);

    }

    /// <summary>
    /// Implements process launching and Windows identity checking functionality.
    /// </summary>
    public class ProcessService : IProcessService
    {
        private readonly Microsoft.Extensions.Logging.ILogger<ProcessService>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessService"/> class.
        /// </summary>
        /// <param name="logger">Optional logger instance for recording process execution diagnostics.</param>
        public ProcessService(Microsoft.Extensions.Logging.ILogger<ProcessService>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void OpenInExplorer(string path)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to launch Windows Explorer for path: {Path}", path);
                System.Diagnostics.Trace.TraceWarning("Failed to launch Windows Explorer for path '{0}': {1}", path, ex);
            }
        }

        /// <inheritdoc />
        public bool IsRunningAsAdministrator()
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to determine administrator execution role.");
                return false;
            }
        }

        /// <summary>
        /// Native Shell32 method to launch the Windows Format Drive dialog.
        /// </summary>
        /// <param name="hwnd">The window handle of the parent window.</param>
        /// <param name="drive">The zero-based index of the drive to format (0 = A:, 1 = B:, 2 = C:, etc.).</param>
        /// <param name="fmtID">The format ID descriptor, or default format ID.</param>
        /// <param name="options">Formatting options or zero for default.</param>
        /// <returns>Zero on success, or an error/status code.</returns>
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern uint SHFormatDrive(IntPtr hwnd, uint drive, uint fmtID, uint options);

        /// <summary>
        /// Default format ID constant used with <see cref="SHFormatDrive(IntPtr, uint, uint, uint)"/>.
        /// </summary>
        private const uint SHFMT_ID_DEFAULT = 0xFFFF;

        /// <summary>
        /// Native return codes for <see cref="SHFormatDrive(IntPtr, uint, uint, uint)"/>.
        /// </summary>
        private const uint SHFMT_ERROR = 0xFFFFFFFF;
        private const uint SHFMT_CANCEL = 0xFFFFFFFE;
        private const uint SHFMT_NOFORMAT = 0xFFFFFFFD;

        /// <inheritdoc />
        public void OpenFormatDialog(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
            {
                return;
            }

            char letter = driveLetter[0];
            if (!char.IsAsciiLetter(letter))
            {
                return;
            }

            uint driveIndex = (uint)(char.ToUpperInvariant(letter) - 'A');
            _logger?.LogInformation("Launching Windows native format dialog for drive {DriveLetter} (index {Index})", driveLetter, driveIndex);

            uint result = SHFormatDrive(IntPtr.Zero, driveIndex, SHFMT_ID_DEFAULT, 0);

            switch (result)
            {
                case SHFMT_CANCEL:
                    _logger?.LogInformation("Format dialog cancelled by user for drive {DriveLetter}", driveLetter);
                    break;
                case SHFMT_ERROR:
                    _logger?.LogWarning("Format dialog encountered an error for drive {DriveLetter}", driveLetter);
                    break;
                case SHFMT_NOFORMAT:
                    _logger?.LogWarning("Drive {DriveLetter} is not formattable", driveLetter);
                    break;
                default:
                    _logger?.LogInformation("Format dialog completed successfully for drive {DriveLetter} (result: 0x{Result:X8})", driveLetter, result);
                    break;
            }
        }

    }
}
