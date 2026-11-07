using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Easy_Copier.Interop
{
    /// <summary>
    /// Provides Win32 P/Invoke interop declarations for Windows Shell notification icons, subclassing, and popup context menus.
    /// </summary>
    internal static partial class SystemTrayInterop
    {
        /// <summary>
        /// Shell notification icon message to add an icon to the notification area.
        /// </summary>
        public const uint NIM_ADD = 0x00000000;

        /// <summary>
        /// Shell notification icon message to modify an existing icon in the notification area.
        /// </summary>
        public const uint NIM_MODIFY = 0x00000001;

        /// <summary>
        /// Shell notification icon message to delete an icon from the notification area.
        /// </summary>
        public const uint NIM_DELETE = 0x00000002;

        /// <summary>
        /// Shell notification icon message to instruct the notification area to behave according to the specified version.
        /// </summary>
        public const uint NIM_SETVERSION = 0x00000004;

        /// <summary>
        /// <see cref="NOTIFYICONDATA"/> flag indicating that <see cref="NOTIFYICONDATA.uCallbackMessage"/> is valid.
        /// </summary>
        public const uint NIF_MESSAGE = 0x00000001;

        /// <summary>
        /// <see cref="NOTIFYICONDATA"/> flag indicating that <see cref="NOTIFYICONDATA.hIcon"/> is valid.
        /// </summary>
        public const uint NIF_ICON = 0x00000002;

        /// <summary>
        /// <see cref="NOTIFYICONDATA"/> flag indicating that <see cref="NOTIFYICONDATA.szTip"/> is valid.
        /// </summary>
        public const uint NIF_TIP = 0x00000004;

        /// <summary>
        /// <see cref="NOTIFYICONDATA"/> flag indicating that balloon notification members (<see cref="NOTIFYICONDATA.szInfo"/>, <see cref="NOTIFYICONDATA.szInfoTitle"/>, <see cref="NOTIFYICONDATA.dwInfoFlags"/>, and <see cref="NOTIFYICONDATA.uTimeoutOrVersion"/>) are valid.
        /// </summary>
        public const uint NIF_INFO = 0x00000010;

        /// <summary>
        /// Balloon notification flag indicating no icon is displayed in the balloon notification.
        /// </summary>
        public const uint NIIF_NONE = 0x00000000;

        /// <summary>
        /// Balloon notification flag indicating an information icon is displayed in the balloon notification.
        /// </summary>
        public const uint NIIF_INFO = 0x00000001;

        /// <summary>
        /// The base value for private window messages defined by applications.
        /// </summary>
        public const uint WM_USER = 0x0400;

        /// <summary>
        /// Custom window message sent by the Shell notification icon when a mouse event occurs over the icon.
        /// </summary>
        public const uint WM_TRAYICON = WM_USER + 0x07A1;

        /// <summary>
        /// Window message posted when the user releases the left mouse button.
        /// </summary>
        public const uint WM_LBUTTONUP = 0x0202;

        /// <summary>
        /// Window message posted when the user releases the right mouse button.
        /// </summary>
        public const uint WM_RBUTTONUP = 0x0205;

        /// <summary>
        /// Window message sent to notify a window that the user clicked the right mouse button or context menu key in the window.
        /// </summary>
        public const uint WM_CONTEXTMENU = 0x007B;

        /// <summary>
        /// Menu flag indicating that the menu item displays a text string.
        /// </summary>
        public const uint MF_STRING = 0x00000000;

        /// <summary>
        /// Menu flag indicating that the menu item is disabled and grayed so that it cannot be selected.
        /// </summary>
        public const uint MF_GRAYED = 0x00000001;

        /// <summary>
        /// Menu flag indicating that the menu item is a horizontal dividing line.
        /// </summary>
        public const uint MF_SEPARATOR = 0x00000800;

        /// <summary>
        /// Menu flag indicating that the menu item is the default item in the menu (displayed in bold).
        /// </summary>
        public const uint MF_DEFAULT = 0x00001000;

        /// <summary>
        /// Popup menu flag instructing <see cref="TrackPopupMenuEx"/> to return the menu item identifier instead of posting a message.
        /// </summary>
        public const uint TPM_RETURNCMD = 0x0100;

        /// <summary>
        /// Popup menu flag indicating that the user can track the popup menu with the right mouse button.
        /// </summary>
        public const uint TPM_RIGHTBUTTON = 0x0002;

        /// <summary>
        /// Window display command flag to hide the window and activate another window.
        /// </summary>
        public const int SW_HIDE = 0;

        /// <summary>
        /// Window display command flag to activate and display a window in its current size and position.
        /// </summary>
        public const int SW_SHOW = 5;

        /// <summary>
        /// Window display command flag to activate and display a window, restoring it to its original size and position if minimized.
        /// </summary>
        public const int SW_RESTORE = 9;

        /// <summary>
        /// Image type specifier indicating an icon image resource for <see cref="LoadImage"/>.
        /// </summary>
        public const uint IMAGE_ICON = 1;

        /// <summary>
        /// Image loading flag instructing <see cref="LoadImage"/> to load the image from a standalone file rather than a module resource.
        /// </summary>
        public const uint LR_LOADFROMFILE = 0x00000010;

        /// <summary>
        /// Image loading flag instructing <see cref="LoadImage"/> to use the default width and height specified by the system metrics.
        /// </summary>
        public const uint LR_DEFAULTSIZE = 0x00000040;

        /// <summary>
        /// Defines the callback procedure for window subclass message processing.
        /// </summary>
        /// <param name="hWnd">A handle to the subclassed window.</param>
        /// <param name="uMsg">The message identifier being processed.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <param name="uIdSubclass">The subclass ID passed when registering the subclass.</param>
        /// <param name="dwRefData">Reference data passed when registering the subclass.</param>
        /// <returns>The return value is the result of the message processing and depends on the message.</returns>
        public delegate nint SUBCLASSPROC(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam, nuint uIdSubclass, nuint dwRefData);

        /// <summary>
        /// Structure containing notification icon attributes for <see cref="Shell_NotifyIcon"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NOTIFYICONDATA
        {
            /// <summary>
            /// The size of this structure, in bytes.
            /// </summary>
            public uint cbSize;

            /// <summary>
            /// A handle to the window that receives notification messages associated with the icon.
            /// </summary>
            public IntPtr hWnd;

            /// <summary>
            /// The application-defined identifier of the taskbar icon.
            /// </summary>
            public uint uID;

            /// <summary>
            /// Flags that indicate which of the other structure members contain valid data or provide additional configuration.
            /// </summary>
            public uint uFlags;

            /// <summary>
            /// An application-defined message identifier sent to the window specified by <see cref="hWnd"/> when a mouse event occurs.
            /// </summary>
            public uint uCallbackMessage;

            /// <summary>
            /// A handle to the icon to be added, modified, or deleted.
            /// </summary>
            public IntPtr hIcon;

            /// <summary>
            /// A null-terminated string that specifies the text for a standard tooltip.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;

            /// <summary>
            /// The state of the notification icon.
            /// </summary>
            public uint dwState;

            /// <summary>
            /// A mask that specifies which bits of the <see cref="dwState"/> member are retrieved or modified.
            /// </summary>
            public uint dwStateMask;

            /// <summary>
            /// A null-terminated string that specifies the text for a balloon notification.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;

            /// <summary>
            /// The timeout value for a balloon notification, or the notification icon behavior version when used with <see cref="NIM_SETVERSION"/>.
            /// </summary>
            public uint uTimeoutOrVersion;

            /// <summary>
            /// A null-terminated string that specifies a title for a balloon notification.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;

            /// <summary>
            /// Flags that can be set to modify the behavior and appearance of a balloon notification.
            /// </summary>
            public uint dwInfoFlags;

            /// <summary>
            /// A registered GUID that identifies the notification icon.
            /// </summary>
            public Guid guidItem;

            /// <summary>
            /// A handle to a custom balloon icon provided by the application.
            /// </summary>
            public IntPtr hBalloonIcon;
        }

        /// <summary>
        /// Defines the screen coordinates of a cursor point.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// The x-coordinate of the point.
            /// </summary>
            public int X;

            /// <summary>
            /// The y-coordinate of the point.
            /// </summary>
            public int Y;
        }

        /// <summary>
        /// Sends a message to the taskbar status area to add, modify, or delete a notification icon.
        /// </summary>
        /// <param name="dwMessage">A value that specifies the action to be taken (e.g. <see cref="NIM_ADD"/>, <see cref="NIM_MODIFY"/>, or <see cref="NIM_DELETE"/>).</param>
        /// <param name="lpData">A reference to a <see cref="NOTIFYICONDATA"/> structure that specifies icon attributes.</param>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        [SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = "NOTIFYICONDATA contains ByValTStr fixed-size string arrays which are not supported by the LibraryImport source generator.")]
        public static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        /// <summary>
        /// Installs a subclass callback on a window.
        /// </summary>
        /// <param name="hWnd">A handle to the window being subclassed.</param>
        /// <param name="pfnSubclass">A pointer to the window subclass procedure.</param>
        /// <param name="uIdSubclass">The subclass ID.</param>
        /// <param name="dwRefData">Subclass reference data.</param>
        /// <returns><see langword="true"/> if the subclass callback was successfully installed; otherwise, <see langword="false"/>.</returns>
        [LibraryImport("comctl32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, nuint uIdSubclass, nuint dwRefData);

        /// <summary>
        /// Removes a subclass callback from a window.
        /// </summary>
        /// <param name="hWnd">A handle to the window being subclassed.</param>
        /// <param name="pfnSubclass">A pointer to the window subclass procedure.</param>
        /// <param name="uIdSubclass">The subclass ID.</param>
        /// <returns><see langword="true"/> if the subclass callback was successfully removed; otherwise, <see langword="false"/>.</returns>
        [LibraryImport("comctl32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, nuint uIdSubclass);

        /// <summary>
        /// Calls the next handler in a window's subclass chain.
        /// </summary>
        /// <param name="hWnd">A handle to the window being subclassed.</param>
        /// <param name="uMsg">The message identifier being processed.</param>
        /// <param name="wParam">Additional message information.</param>
        /// <param name="lParam">Additional message information.</param>
        /// <returns>The return value is the result of the message processing and depends on the message sent.</returns>
        [LibraryImport("comctl32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint DefSubclassProc(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam);

        /// <summary>
        /// Retrieves the position of the mouse cursor in screen coordinates.
        /// </summary>
        /// <param name="lpPoint">When this method returns, contains the screen coordinates of the cursor.</param>
        /// <returns><see langword="true"/> if successful; otherwise, <see langword="false"/>.</returns>
        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetCursorPos(out POINT lpPoint);

        /// <summary>
        /// Creates a drop-down menu, submenu, or shortcut menu.
        /// </summary>
        /// <returns>If the function succeeds, the return value is a handle to the newly created menu; otherwise, <see cref="IntPtr.Zero"/>.</returns>
        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial IntPtr CreatePopupMenu();

        /// <summary>
        /// Appends a new item to the end of the specified menu bar, drop-down menu, submenu, or shortcut menu.
        /// </summary>
        /// <param name="hMenu">A handle to the menu to be changed.</param>
        /// <param name="uFlags">Controls the appearance and behavior of the new menu item.</param>
        /// <param name="uIDNewItem">The identifier of the new menu item or, if <paramref name="uFlags"/> contains MF_POPUP, a handle to the drop-down menu or submenu.</param>
        /// <param name="lpNewItem">The content of the new menu item.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AppendMenu(IntPtr hMenu, uint uFlags, nuint uIDNewItem, string lpNewItem);

        /// <summary>
        /// Displays a shortcut menu at the specified location and tracks the selection of items on the menu.
        /// </summary>
        /// <param name="hMenu">A handle to the shortcut menu to be displayed.</param>
        /// <param name="uFlags">Specifies positioning and tracking options.</param>
        /// <param name="x">The horizontal location of the shortcut menu, in screen coordinates.</param>
        /// <param name="y">The vertical location of the shortcut menu, in screen coordinates.</param>
        /// <param name="hWnd">A handle to the window that owns the shortcut menu.</param>
        /// <param name="lptpm">A pointer to a structure that specifies an area of the screen the menu should not overlap, or <see cref="IntPtr.Zero"/>.</param>
        /// <returns>If <see cref="TPM_RETURNCMD"/> is specified, the return value is the menu-item identifier of the item that the user selected; otherwise, 0.</returns>
        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lptpm);

        /// <summary>
        /// Destroys the specified menu and frees any memory that the menu occupies.
        /// </summary>
        /// <param name="hMenu">A handle to the menu to be destroyed.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool DestroyMenu(IntPtr hMenu);

        /// <summary>
        /// Brings the thread that created the specified window into the foreground and activates the window.
        /// </summary>
        /// <param name="hWnd">A handle to the window that should be activated and brought to the foreground.</param>
        /// <returns><see langword="true"/> if the window was brought to the foreground; otherwise, <see langword="false"/>.</returns>
        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Sets the specified window's show state.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nCmdShow">Controls how the window is to be shown (e.g. <see cref="SW_HIDE"/>, <see cref="SW_SHOW"/>, or <see cref="SW_RESTORE"/>).</param>
        /// <returns><see langword="true"/> if the window was previously visible; <see langword="false"/> if previously hidden.</returns>
        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Loads an icon, cursor, animated cursor, or bitmap.
        /// </summary>
        /// <param name="hInst">A handle to an instance of the module that contains the image to be loaded, or <see cref="IntPtr.Zero"/> when loading from file.</param>
        /// <param name="name">The path or resource identifier of the image to load.</param>
        /// <param name="type">The type of image to be loaded (e.g. <see cref="IMAGE_ICON"/>).</param>
        /// <param name="cx">The width, in pixels, of the icon or image.</param>
        /// <param name="cy">The height, in pixels, of the icon or image.</param>
        /// <param name="fuLoad">Image loading flags (e.g. <see cref="LR_LOADFROMFILE"/> and <see cref="LR_DEFAULTSIZE"/>).</param>
        /// <returns>If the function succeeds, the return value is the handle of the newly loaded image; otherwise, <see cref="IntPtr.Zero"/>.</returns>
        [LibraryImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

        /// <summary>
        /// Destroys an icon and frees any memory the icon occupied.
        /// </summary>
        /// <param name="hIcon">A handle to the icon to be destroyed.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        [LibraryImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool DestroyIcon(IntPtr hIcon);
    }
}
