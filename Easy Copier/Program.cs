using Microsoft.UI.Dispatching;
using System;
using System.Threading;
using Velopack;

namespace Easy_Copier
{
    public static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Velopack initialization must run before any UI code is created.
            VelopackApp.Build().Run();

            WinRT.ComWrappersSupport.InitializeComWrappers();

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
