using Microsoft.UI.Dispatching;
using System;
using System.Threading;
using Velopack;

namespace Easy_Copier
{
    public static class Program
    {
        [System.Runtime.InteropServices.DllImport("shell32.dll", SetLastError = true)]
        [System.Runtime.InteropServices.DefaultDllImportSearchPaths(System.Runtime.InteropServices.DllImportSearchPath.System32)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string AppID);

        [STAThread]
        private static void Main(string[] args)
        {
            // Velopack initialization must run before any UI code is created.
            VelopackApp.Build().Run();

            WinRT.ComWrappersSupport.InitializeComWrappers();

            try
            {
                // Explicitly set the AppUserModelID for unpackaged execution (e.g. running from VS or direct exe).
                // This is required for Windows Toast Notifications to display properly.
                // Store the returned HRESULT to satisfy Roslyn rule CA1806.
                int hresult = SetCurrentProcessExplicitAppUserModelID("EasyCopier.App");
            }
            catch { }

            bool isRedirect = false;
            try
            {
                // This call might throw if another instance has already redirected activation,
                // but we typically don't use AppInstance redirection in this simple app right now.
                // We keep standard initialization.
                _ = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            }
            catch { }

            if (!isRedirect)
            {
                Microsoft.UI.Xaml.Application.Start((p) =>
                {
                    DispatcherQueueSynchronizationContext context = new(
                        DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                });
            }
        }
    }
}
