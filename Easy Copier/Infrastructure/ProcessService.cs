using System;
using System.Diagnostics;
using System.Security.Principal;

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
        /// <summary>
        /// Opens Windows File Explorer to the specified directory or file path.
        /// </summary>
        /// <param name="path">The folder or file path to display.</param>
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
            catch
            {
                // Ignore failure to open explorer
            }
        }

        /// <summary>
        /// Checks whether the current application process is executing with Administrator privileges.
        /// </summary>
        /// <returns><c>true</c> if running as Administrator; otherwise, <c>false</c>.</returns>
        public bool IsRunningAsAdministrator()
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern uint SHFormatDrive(IntPtr hwnd, uint drive, uint fmtID, uint options);

        private const uint SHFMT_ID_DEFAULT = 0xFFFF;

        /// <summary>
        /// Opens the native Windows Format dialog for the specified drive.
        /// </summary>
        /// <param name="driveLetter">The drive letter (e.g., "D:" or "D").</param>
        public void OpenFormatDialog(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return;

            char letter = driveLetter[0];
            if (!char.IsAsciiLetter(letter))
                return;

            uint driveIndex = (uint)(char.ToUpperInvariant(letter) - 'A');

            _ = SHFormatDrive(IntPtr.Zero, driveIndex, SHFMT_ID_DEFAULT, 0);
        }

    }
}
