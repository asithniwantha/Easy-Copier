using Microsoft.UI.Dispatching;
using System;
using System.Threading;
using Velopack;

namespace Easy_Copier
{
    /// <summary>
    /// Provides the main entry point and single-instance activation handling for the Easy Copier application.
    /// </summary>
    public static class Program
    {
        // private static extern int SetCurrentProcessExplicitAppUserModelID([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string AppID);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        [STAThread]
        private static void Main(string[] args)
        {
            // Velopack initialization must run before any UI code is created.
            VelopackApp.Build().Run();

            WinRT.ComWrappersSupport.InitializeComWrappers();

            // No longer explicitly setting AppUserModelID; relying on Windows App SDK bootstrapper and NotificationInvoked.

            try
            {
                Microsoft.Windows.AppLifecycle.AppInstance mainInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("EasyCopierMainInstance");

                if (!mainInstance.IsCurrent)
                {
                    Microsoft.Windows.AppLifecycle.AppActivationArguments argsActivated = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
                    mainInstance.RedirectActivationToAsync(argsActivated).AsTask().Wait();
                    return;
                }

                mainInstance.Activated += MainInstance_Activated;
            }
            catch { }

            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                DispatcherQueueSynchronizationContext context = new(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }

        /// <summary>
        /// Handles the <see cref="Microsoft.Windows.AppLifecycle.AppInstance.Activated"/> event when a secondary instance triggers redirection.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">The activation arguments detailing the activation context.</param>
        private static void MainInstance_Activated(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments e)
        {
            _ = (App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    App.MainWindow.Activate();
                    IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                    Easy_Copier.Infrastructure.NativeWindowHelper.SetForeground(hwnd);
                }));
        }
    }
}
