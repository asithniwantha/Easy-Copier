using System.Diagnostics;
using System.Security.Principal;

namespace Easy_Copier.Infrastructure
{
    public interface IProcessService
    {
        void OpenInExplorer(string path);
        bool IsRunningAsAdministrator();
    }

    public class ProcessService : IProcessService
    {
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
    }
}