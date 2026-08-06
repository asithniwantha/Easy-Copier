using Easy_Copier.Infrastructure;
using Easy_Copier.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Easy_Copier.Views
{
    public sealed partial class HistoryWindow : Window
    {
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWLP_HWNDPARENT = -8;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        public HistoryViewModel ViewModel { get; }

        public HistoryWindow(IntPtr ownerHwnd = default)
        {
            ViewModel = AppServiceLocator.GetService<HistoryViewModel>();
            InitializeComponent();

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
                appWindow.Resize(new Windows.Graphics.SizeInt32(1100, 700));

            if (ownerHwnd != default)
            {
                if (IntPtr.Size == 8)
                    SetWindowLongPtr64(hwnd, GWLP_HWNDPARENT, ownerHwnd);
                else
                    SetWindowLong32(hwnd, GWLP_HWNDPARENT, ownerHwnd.ToInt32());

                CenterOnOwner(hwnd, ownerHwnd);
            }

            _ = ViewModel.InitializeAsync();
        }

        private void MonthsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView list && list.SelectedItem is MonthOption option)
                _ = ViewModel.LoadMonthAsync(option);
        }

        private async void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Clear All History",
                Content = "This will permanently delete all copy history records. Continue?",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.ClearAllHistoryCommand.ExecuteAsync(null);
                if (sender is FrameworkElement fe)
                {
                    var list = fe.FindName("MonthsList") as ListView;
                    if (list != null) list.SelectedItem = null;
                }
            }
        }

        private static void CenterOnOwner(IntPtr hwnd, IntPtr ownerHwnd)
        {
            if (!GetWindowRect(hwnd, out var child) || !GetWindowRect(ownerHwnd, out var owner))
                return;
            int childW = child.Right - child.Left;
            int childH = child.Bottom - child.Top;
            int x = owner.Left + (owner.Right - owner.Left - childW) / 2;
            int y = owner.Top + (owner.Bottom - owner.Top - childH) / 2;
            SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }
    }
}
