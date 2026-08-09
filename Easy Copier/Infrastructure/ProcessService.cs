using System.Diagnostics;

namespace Easy_Copier.Infrastructure
{
    public interface IProcessService
    {
        void OpenInExplorer(string path);
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
    }
}
