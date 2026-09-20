using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides Win32 P/Invoke utilities and WinUI 3 AppWindow helpers for managing windows, modal dialog ownership, positioning, and sizing.
    /// </summary>
    public static partial class NativeWindowHelper
    {
        private const int GWLP_HWNDPARENT = -8;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [LibraryImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EnableWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial IntPtr GetForegroundWindow();

        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial IntPtr GetActiveWindow();

        [LibraryImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Gets the handle (HWND) of the currently active window belonging to the running application process.
        /// </summary>
        /// <returns>The window handle <see cref="IntPtr"/>, or <see cref="IntPtr.Zero"/> if none is found or on non-Windows platforms.</returns>
        public static IntPtr GetActiveWindowHandle()
        {
            if (!OperatingSystem.IsWindows())
            {
                return IntPtr.Zero;
            }

            try
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != IntPtr.Zero)
                {
                    _ = GetWindowThreadProcessId(foregroundWindow, out uint processId);
                    if (processId == Environment.ProcessId)
                    {
                        return foregroundWindow;
                    }
                }

                return GetActiveWindow();
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Sets the parent/owner window for a child window handle using Win32 <c>SetWindowLongPtr</c>.
        /// </summary>
        /// <param name="childHwnd">The child window handle.</param>
        /// <param name="ownerHwnd">The owner window handle.</param>
        public static void SetOwner(IntPtr childHwnd, IntPtr ownerHwnd)
        {
            _ = IntPtr.Size == 8
                ? SetWindowLongPtr64(childHwnd, GWLP_HWNDPARENT, ownerHwnd)
                : SetWindowLong32(childHwnd, GWLP_HWNDPARENT, ownerHwnd.ToInt32());
        }

        /// <summary>
        /// Enables or disables input interaction for the specified window handle.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="enable"><c>true</c> to enable input; <c>false</c> to disable input.</param>
        public static void EnableWindowInput(IntPtr hwnd, bool enable)
        {
            _ = EnableWindow(hwnd, enable);
        }

        /// <summary>
        /// Centers a child window over its owner window on screen.
        /// </summary>
        /// <param name="childHwnd">The child window handle.</param>
        /// <param name="ownerHwnd">The owner window handle.</param>
        public static void CenterWindow(IntPtr childHwnd, IntPtr ownerHwnd)
        {
            if (!GetWindowRect(ownerHwnd, out RECT ownerRect) || !GetWindowRect(childHwnd, out RECT selfRect))
            {
                return;
            }

            int ownerWidth = ownerRect.Right - ownerRect.Left;
            int ownerHeight = ownerRect.Bottom - ownerRect.Top;
            int selfWidth = selfRect.Right - selfRect.Left;
            int selfHeight = selfRect.Bottom - selfRect.Top;

            int x = ownerRect.Left + ((ownerWidth - selfWidth) / 2);
            int y = ownerRect.Top + ((ownerHeight - selfHeight) / 2);

            _ = SetWindowPos(childHwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }

        /// <summary>
        /// Brings the specified window handle to the foreground.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        public static void SetForeground(IntPtr hwnd)
        {
            _ = SetForegroundWindow(hwnd);
        }

        /// <summary>
        /// Hooks into the size changed events of a root element to dynamically resize the WinUI 3 application window.
        /// </summary>
        /// <param name="window">The target window to adjust.</param>
        /// <param name="rootElement">The root content element whose desired size dictates window dimensions.</param>
        /// <param name="minWidth">Minimum allowed width in pixels. Defaults to 960.</param>
        /// <param name="minHeight">Minimum allowed height in pixels. Defaults to 640.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> or <paramref name="rootElement"/> is null.</exception>
        public static void EnableDynamicResizing(Microsoft.UI.Xaml.Window window, Microsoft.UI.Xaml.FrameworkElement rootElement, int minWidth = 960, int minHeight = 640)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(rootElement);

            rootElement.SizeChanged += (s, e) =>
            {
                rootElement.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

                int targetWidth = System.Math.Max(minWidth, (int)System.Math.Ceiling(rootElement.DesiredSize.Width));
                int targetHeight = System.Math.Max(minHeight, (int)System.Math.Ceiling(rootElement.DesiredSize.Height) + 40);

                nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                if (appWindow != null && (appWindow.Size.Width != targetWidth || appWindow.Size.Height != targetHeight))
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32(targetWidth, targetHeight));
                }
            };
        }

        /// <summary>
        /// Sets the minimum window size preferences taking high DPI display scaling into account.
        /// </summary>
        /// <param name="window">The target window.</param>
        /// <param name="minWidth">The minimum width in logical pixels.</param>
        /// <param name="minHeight">The minimum height in logical pixels.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> is null.</exception>
        public static void SetMinimumSize(Microsoft.UI.Xaml.Window window, int minWidth, int minHeight)
        {
            ArgumentNullException.ThrowIfNull(window);

            nint hwnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                double scale = GetRasterizationScale(hwnd);
                presenter.PreferredMinimumWidth = (int)Math.Round(minWidth * scale);
                presenter.PreferredMinimumHeight = (int)Math.Round(minHeight * scale);
            }
        }

        private static double GetRasterizationScale(nint hwnd)
        {
            const int defaultDpi = 96;
            uint dpi = GetDpiForWindow(hwnd);
            return dpi <= 0 ? 1.0 : dpi / (double)defaultDpi;
        }

        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial uint GetDpiForWindow(IntPtr hWnd);

        /// <summary>
        /// Initializes standard window parameters including window dimensions and default window icons.
        /// </summary>
        /// <param name="window">The window to initialize.</param>
        /// <param name="width">The initial width in pixels.</param>
        /// <param name="height">The initial height in pixels.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> is null.</exception>
        public static void InitializeWindow(Microsoft.UI.Xaml.Window window, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(window);

            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "easy copier ico.ico");
                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }

                appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            }
        }

        /// <summary>
        /// Configures and shows a child window modally with respect to an owner window.
        /// </summary>
        /// <param name="childWindow">The child window to display modally.</param>
        /// <param name="ownerWindow">The owner window to disable interaction for while the modal is open.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="childWindow"/> is null.</exception>
        public static void ShowAsModal(Microsoft.UI.Xaml.Window childWindow, Microsoft.UI.Xaml.Window? ownerWindow)
        {
            ArgumentNullException.ThrowIfNull(childWindow);

            IntPtr childHwnd = WindowNative.GetWindowHandle(childWindow);
            IntPtr ownerHwnd = ownerWindow != null ? WindowNative.GetWindowHandle(ownerWindow) : IntPtr.Zero;

            if (ownerHwnd != IntPtr.Zero)
            {
                SetOwner(childHwnd, ownerHwnd);
                EnableWindowInput(ownerHwnd, false);
                CenterWindow(childHwnd, ownerHwnd);
            }
        }

        /// <summary>
        /// Re-enables input interaction and restores foreground focus for an owner window after a modal window closes.
        /// </summary>
        /// <param name="ownerWindow">The owner window to restore.</param>
        public static void RestoreOwnerInput(Microsoft.UI.Xaml.Window? ownerWindow)
        {
            IntPtr ownerHwnd = ownerWindow != null ? WindowNative.GetWindowHandle(ownerWindow) : IntPtr.Zero;
            if (ownerHwnd != IntPtr.Zero)
            {
                EnableWindowInput(ownerHwnd, true);
                SetForeground(ownerHwnd);
            }
        }

        /// <summary>
        /// Centralized helper method to initialize a modal dialog window, setting data context, dimensions, modality ownership, and close cleanup callbacks.
        /// </summary>
        /// <param name="window">The modal window instance.</param>
        /// <param name="owner">The owner window instance.</param>
        /// <param name="viewModel">The view model to assign as DataContext.</param>
        /// <param name="width">The window width in pixels.</param>
        /// <param name="height">The window height in pixels.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> or <paramref name="owner"/> is null.</exception>
        public static void InitializeModalWindow(Microsoft.UI.Xaml.Window window, Microsoft.UI.Xaml.Window owner, object viewModel, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(owner);

            // Allow the viewmodel to bind
            if (window.Content is Microsoft.UI.Xaml.FrameworkElement fe)
            {
                fe.DataContext = viewModel;
            }

            InitializeWindow(window, width, height);
            ShowAsModal(window, owner);

            // Ensure cleanup on close
            window.Closed += (s, args) =>
            {
                RestoreOwnerInput(owner);
                if (s is Microsoft.UI.Xaml.Window w)
                {
                    w.Content = null;
                }
            };
        }
    }
}
