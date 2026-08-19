using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Easy_Copier.Infrastructure
{
    public static class NativeWindowHelper
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

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static IntPtr GetActiveWindowHandle()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != IntPtr.Zero)
            {
                GetWindowThreadProcessId(foregroundWindow, out uint processId);
                if (processId == Environment.ProcessId)
                {
                    return foregroundWindow;
                }
            }

            return GetActiveWindow();
        }

        public static void SetOwner(IntPtr childHwnd, IntPtr ownerHwnd)
        {
            _ = IntPtr.Size == 8
                ? SetWindowLongPtr64(childHwnd, GWLP_HWNDPARENT, ownerHwnd)
                : SetWindowLong32(childHwnd, GWLP_HWNDPARENT, ownerHwnd.ToInt32());
        }

        public static void EnableWindowInput(IntPtr hwnd, bool enable)
        {
            _ = EnableWindow(hwnd, enable);
        }

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

        public static void SetForeground(IntPtr hwnd)
        {
            _ = SetForegroundWindow(hwnd);
        }

        public static void InitializeWindow(Microsoft.UI.Xaml.Window window, int width, int height)
        {
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

        public static void ShowAsModal(Microsoft.UI.Xaml.Window childWindow, Microsoft.UI.Xaml.Window? ownerWindow)
        {
            IntPtr childHwnd = WindowNative.GetWindowHandle(childWindow);
            IntPtr ownerHwnd = ownerWindow != null ? WindowNative.GetWindowHandle(ownerWindow) : IntPtr.Zero;

            if (ownerHwnd != IntPtr.Zero)
            {
                SetOwner(childHwnd, ownerHwnd);
                EnableWindowInput(ownerHwnd, false);
                CenterWindow(childHwnd, ownerHwnd);
            }
        }

        public static void RestoreOwnerInput(Microsoft.UI.Xaml.Window? ownerWindow)
        {
            IntPtr ownerHwnd = ownerWindow != null ? WindowNative.GetWindowHandle(ownerWindow) : IntPtr.Zero;
            if (ownerHwnd != IntPtr.Zero)
            {
                EnableWindowInput(ownerHwnd, true);
                SetForeground(ownerHwnd);
            }
        }
    }
}
