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
            if (IntPtr.Size == 8)
            {
                SetWindowLongPtr64(childHwnd, GWLP_HWNDPARENT, ownerHwnd);
            }
            else
            {
                SetWindowLong32(childHwnd, GWLP_HWNDPARENT, ownerHwnd.ToInt32());
            }
        }

        public static void EnableWindowInput(IntPtr hwnd, bool enable)
        {
            EnableWindow(hwnd, enable);
        }

        public static void CenterWindow(IntPtr childHwnd, IntPtr ownerHwnd)
        {
            if (!GetWindowRect(ownerHwnd, out var ownerRect) || !GetWindowRect(childHwnd, out var selfRect))
                return;

            var ownerWidth = ownerRect.Right - ownerRect.Left;
            var ownerHeight = ownerRect.Bottom - ownerRect.Top;
            var selfWidth = selfRect.Right - selfRect.Left;
            var selfHeight = selfRect.Bottom - selfRect.Top;

            var x = ownerRect.Left + (ownerWidth - selfWidth) / 2;
            var y = ownerRect.Top + (ownerHeight - selfHeight) / 2;

            SetWindowPos(childHwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }

        public static void SetForeground(IntPtr hwnd)
        {
            SetForegroundWindow(hwnd);
        }
    }
}
