using System;
using System.Runtime.InteropServices;

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
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

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
    }
}
