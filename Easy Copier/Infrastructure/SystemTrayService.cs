using Easy_Copier.Interop;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Implements system tray integration using Win32 <c>Shell_NotifyIcon</c> and window subclassing,
    /// enabling background transfer resilience, interactive tray menu, and live progress tooltips.
    /// </summary>
    public sealed class SystemTrayService : ISystemTrayService
    {
        /// <summary>
        /// The transfer queue service tracking active and pending copy operations.
        /// </summary>
        private readonly ITransferQueueService _transferQueueService;

        /// <summary>
        /// The dispatcher service for marshaling operations to the UI thread.
        /// </summary>
        private readonly IDispatcherService _dispatcherService;

        /// <summary>
        /// The logger instance for recording diagnostic and error messages.
        /// </summary>
        private readonly ILogger<SystemTrayService> _logger;

        /// <summary>
        /// The native window handle (<c>HWND</c>) of the main application window.
        /// </summary>
        private IntPtr _hWnd = IntPtr.Zero;

        /// <summary>
        /// The native icon handle (<c>HICON</c>) loaded for the system tray notification icon.
        /// </summary>
        private IntPtr _hIcon = IntPtr.Zero;

        /// <summary>
        /// Indicates whether the notification icon has been added to the system tray.
        /// </summary>
        private bool _isIconAdded;

        /// <summary>
        /// Indicates whether the main window procedure is currently subclassed for tray messages.
        /// </summary>
        private bool _isSubclassed;

        /// <summary>
        /// Indicates whether this instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The unique identifier assigned to the window subclass procedure.
        /// </summary>
        private const nuint SUBCLASS_ID = 4040;

        /// <summary>
        /// The menu command identifier for opening or restoring the application window.
        /// </summary>
        private const uint MENU_OPEN_ID = 5001;

        /// <summary>
        /// The menu command identifier for canceling all active file transfers.
        /// </summary>
        private const uint MENU_CANCEL_ID = 5002;

        /// <summary>
        /// The menu command identifier for cleanly exiting the application.
        /// </summary>
        private const uint MENU_EXIT_ID = 5003;

        /// <summary>
        /// Retains a rooted reference to the subclass callback delegate to prevent garbage collection while registered with comctl32.
        /// </summary>
        private SystemTrayInterop.SUBCLASSPROC? _subclassProc;

        /// <inheritdoc />
        public bool IsTrayIconVisible => _isIconAdded;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemTrayService"/> class.
        /// </summary>
        public SystemTrayService(
            ITransferQueueService transferQueueService,
            IDispatcherService dispatcherService,
            ILogger<SystemTrayService> logger)
        {
            _transferQueueService = transferQueueService ?? throw new ArgumentNullException(nameof(transferQueueService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void Initialize(Window mainWindow)
        {
            ArgumentNullException.ThrowIfNull(mainWindow);

            try
            {
                _hWnd = WindowNative.GetWindowHandle(mainWindow);
                if (_hWnd == IntPtr.Zero)
                {
                    _logger.LogWarning("Failed to acquire HWND from main window for SystemTrayService.");
                    return;
                }

                LoadAppIcon();

                // Intent: Subclass the main window procedure so Win32 tray messages (click, context menu)
                // sent to WM_TRAYICON are captured directly by this service without altering XAML window routing.
                _subclassProc = WindowSubclassProc;
                _isSubclassed = SystemTrayInterop.SetWindowSubclass(_hWnd, _subclassProc, SUBCLASS_ID, 0);

                // Observe transfer queue collection and item properties to update tray tooltip and notifications dynamically
                _transferQueueService.QueueItems.CollectionChanged += OnQueueItemsCollectionChanged;
                foreach (TransferQueueItem item in _transferQueueService.QueueItems)
                {
                    item.PropertyChanged += OnQueueItemPropertyChanged;
                }

                _logger.LogInformation("SystemTrayService initialized successfully for HWND 0x{Hwnd:X8}.", _hWnd.ToInt64());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SystemTrayService.");
            }
        }

        /// <inheritdoc />
        public void ShowTrayIcon()
        {
            if (_hWnd == IntPtr.Zero || _hIcon == IntPtr.Zero)
            {
                return;
            }

            try
            {
                SystemTrayInterop.NOTIFYICONDATA nid = CreateNotifyIconData();
                nid.uFlags = SystemTrayInterop.NIF_MESSAGE | SystemTrayInterop.NIF_ICON | SystemTrayInterop.NIF_TIP;
                nid.szTip = GetCurrentTooltipText();

                if (!_isIconAdded)
                {
                    bool result = SystemTrayInterop.Shell_NotifyIcon(SystemTrayInterop.NIM_ADD, ref nid);
                    if (result)
                    {
                        _isIconAdded = true;
                        // Set modern V4 version behavior
                        nid.uTimeoutOrVersion = 4;
                        _ = SystemTrayInterop.Shell_NotifyIcon(SystemTrayInterop.NIM_SETVERSION, ref nid);
                    }
                    else
                    {
                        _logger.LogWarning("Shell_NotifyIcon(NIM_ADD) returned false.");
                    }
                }
                else
                {
                    _ = SystemTrayInterop.Shell_NotifyIcon(SystemTrayInterop.NIM_MODIFY, ref nid);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while displaying system tray icon.");
            }
        }

        /// <inheritdoc />
        public void RemoveTrayIcon()
        {
            if (!_isIconAdded || _hWnd == IntPtr.Zero)
            {
                return;
            }

            try
            {
                SystemTrayInterop.NOTIFYICONDATA nid = CreateNotifyIconData();
                _ = SystemTrayInterop.Shell_NotifyIcon(SystemTrayInterop.NIM_DELETE, ref nid);
                _isIconAdded = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing system tray icon.");
            }
        }

        /// <inheritdoc />
        public void ShowNotification(string title, string message)
        {
            if (_hWnd == IntPtr.Zero)
            {
                return;
            }

            if (!_isIconAdded)
            {
                ShowTrayIcon();
            }

            try
            {
                SystemTrayInterop.NOTIFYICONDATA nid = CreateNotifyIconData();
                nid.uFlags = SystemTrayInterop.NIF_INFO;
                nid.szInfoTitle = title ?? "Easy Copier";
                nid.szInfo = message ?? string.Empty;
                nid.dwInfoFlags = SystemTrayInterop.NIIF_INFO;

                _ = SystemTrayInterop.Shell_NotifyIcon(SystemTrayInterop.NIM_MODIFY, ref nid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying tray balloon notification.");
            }
        }

        /// <inheritdoc />
        public void MinimizeToTray()
        {
            if (_hWnd == IntPtr.Zero)
            {
                return;
            }

            _ = _dispatcherService.TryEnqueue(() =>
            {
                ShowTrayIcon();
                _ = SystemTrayInterop.ShowWindow(_hWnd, SystemTrayInterop.SW_HIDE);

                int activeCount = _transferQueueService.QueueItems.Count(i => i.IsActive);
                if (activeCount > 0)
                {
                    ShowNotification(
                        "Easy Copier",
                        $"{activeCount} transfer(s) are running in the background. Click the icon to restore.");
                }
            });
        }

        /// <inheritdoc />
        public void RestoreMainWindow()
        {
            if (_hWnd == IntPtr.Zero)
            {
                return;
            }

            _ = _dispatcherService.TryEnqueue(() =>
            {
                _ = SystemTrayInterop.ShowWindow(_hWnd, SystemTrayInterop.SW_RESTORE);
                _ = SystemTrayInterop.SetForegroundWindow(_hWnd);
            });
        }

        /// <summary>
        /// Subclass window procedure intercepting tray messages and routing menu actions.
        /// </summary>
        private nint WindowSubclassProc(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam, nuint uIdSubclass, nuint dwRefData)
        {
            if (uMsg == SystemTrayInterop.WM_TRAYICON)
            {
                uint mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);

                if (mouseMsg == SystemTrayInterop.WM_LBUTTONUP)
                {
                    // Left click restores the application window
                    RestoreMainWindow();
                    return 0;
                }

                if (mouseMsg is SystemTrayInterop.WM_RBUTTONUP or SystemTrayInterop.WM_CONTEXTMENU)
                {
                    // Right click displays the native Win32 context popup menu
                    ShowContextMenu();
                    return 0;
                }
            }

            return SystemTrayInterop.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        /// <summary>
        /// Displays the Win32 system tray popup context menu at the current cursor position.
        /// </summary>
        private void ShowContextMenu()
        {
            if (_hWnd == IntPtr.Zero)
            {
                return;
            }

            IntPtr hMenu = SystemTrayInterop.CreatePopupMenu();
            if (hMenu == IntPtr.Zero)
            {
                return;
            }

            try
            {
                bool hasActiveTransfers = _transferQueueService.QueueItems.Any(i => i.IsActive);

                // Option 1: Open Easy Copier (bold / default)
                _ = SystemTrayInterop.AppendMenu(hMenu, SystemTrayInterop.MF_STRING | SystemTrayInterop.MF_DEFAULT, MENU_OPEN_ID, "Open Easy Copier");
                _ = SystemTrayInterop.AppendMenu(hMenu, SystemTrayInterop.MF_SEPARATOR, 0, string.Empty);

                // Option 2: Cancel Active Transfers (disabled if no active transfers)
                uint cancelFlags = hasActiveTransfers ? SystemTrayInterop.MF_STRING : (SystemTrayInterop.MF_STRING | SystemTrayInterop.MF_GRAYED);
                _ = SystemTrayInterop.AppendMenu(hMenu, cancelFlags, MENU_CANCEL_ID, "Cancel Active Transfers");
                _ = SystemTrayInterop.AppendMenu(hMenu, SystemTrayInterop.MF_SEPARATOR, 0, string.Empty);

                // Option 3: Exit Easy Copier
                _ = SystemTrayInterop.AppendMenu(hMenu, SystemTrayInterop.MF_STRING, MENU_EXIT_ID, "Exit Easy Copier");

                if (!SystemTrayInterop.GetCursorPos(out SystemTrayInterop.POINT pt))
                {
                    return;
                }

                // Intent: Setting the window foreground before TrackPopupMenuEx is required by Win32
                // so clicking outside the menu dismisses it properly.
                _ = SystemTrayInterop.SetForegroundWindow(_hWnd);

                uint chosenCommand = SystemTrayInterop.TrackPopupMenuEx(
                    hMenu,
                    SystemTrayInterop.TPM_RETURNCMD | SystemTrayInterop.TPM_RIGHTBUTTON,
                    pt.X,
                    pt.Y,
                    _hWnd,
                    IntPtr.Zero);

                HandleMenuCommand(chosenCommand);
            }
            finally
            {
                _ = SystemTrayInterop.DestroyMenu(hMenu);
            }
        }

        /// <summary>
        /// Executes the action corresponding to the selected menu item identifier.
        /// </summary>
        private void HandleMenuCommand(uint commandId)
        {
            switch (commandId)
            {
                case MENU_OPEN_ID:
                    RestoreMainWindow();
                    break;

                case MENU_CANCEL_ID:
                    _ = _dispatcherService.TryEnqueue(() =>
                    {
                        var activeItems = _transferQueueService.QueueItems.Where(i => i.IsActive).ToList();
                        foreach (TransferQueueItem item in activeItems)
                        {
                            _transferQueueService.CancelItem(item);
                        }
                    });
                    break;

                case MENU_EXIT_ID:
                    _ = _dispatcherService.TryEnqueue(() =>
                    {
                        RemoveTrayIcon();
                        if (Application.Current is App app)
                        {
                            app.DisposeServices();
                        }
                        Application.Current.Exit();
                    });
                    break;
            }
        }

        /// <summary>
        /// Constructs a base <see cref="SystemTrayInterop.NOTIFYICONDATA"/> structure for this service.
        /// </summary>
        private SystemTrayInterop.NOTIFYICONDATA CreateNotifyIconData()
        {
            return new SystemTrayInterop.NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<SystemTrayInterop.NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1,
                uCallbackMessage = SystemTrayInterop.WM_TRAYICON,
                hIcon = _hIcon,
                szTip = string.Empty,
                szInfo = string.Empty,
                szInfoTitle = string.Empty
            };
        }

        /// <summary>
        /// Loads the application window icon resource for the tray notification area.
        /// </summary>
        private void LoadAppIcon()
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "easy copier ico.ico");
            if (File.Exists(iconPath))
            {
                _hIcon = SystemTrayInterop.LoadImage(
                    IntPtr.Zero,
                    iconPath,
                    SystemTrayInterop.IMAGE_ICON,
                    0,
                    0,
                    SystemTrayInterop.LR_LOADFROMFILE | SystemTrayInterop.LR_DEFAULTSIZE);
            }
        }

        /// <summary>
        /// Calculates the dynamic hover tooltip reflecting active transfer status.
        /// </summary>
        private string GetCurrentTooltipText()
        {
            var activeItems = _transferQueueService.QueueItems.Where(i => i.IsActive).ToList();
            if (activeItems.Count == 0)
            {
                return "Easy Copier \u2014 Idle";
            }

            TransferQueueItem inProgress = activeItems.FirstOrDefault(i => i.Status == TransferQueueItemStatus.InProgress) ?? activeItems[0];
            int pct = (int)Math.Round(inProgress.ProgressPercentage);
            string speed = !string.IsNullOrWhiteSpace(inProgress.SpeedText) ? $" ({inProgress.SpeedText})" : string.Empty;

            string text = $"Easy Copier \u2014 {activeItems.Count} transfer(s){speed} \u2014 {pct}%";
            // Tooltip max length is 127 characters
            return text.Length > 120 ? text[..120] + "..." : text;
        }

        /// <summary>
        /// Handles collection changes in the transfer queue items to hook or unhook property notifications and update tray status.
        /// </summary>
        /// <param name="sender">The sender of the collection changed event.</param>
        /// <param name="e">Event data providing details about added or removed transfer items.</param>
        private void OnQueueItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (TransferQueueItem item in e.NewItems)
                {
                    item.PropertyChanged += OnQueueItemPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (TransferQueueItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnQueueItemPropertyChanged;
                }
            }

            UpdateTrayProgress();
        }

        /// <summary>
        /// Handles property changes on individual transfer queue items to refresh the tray icon tooltip during progress updates.
        /// </summary>
        /// <param name="sender">The transfer queue item whose property changed.</param>
        /// <param name="e">Event data containing the property name that changed.</param>
        private void OnQueueItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(TransferQueueItem.Status)
                or nameof(TransferQueueItem.ProgressPercentage)
                or nameof(TransferQueueItem.SpeedText))
            {
                UpdateTrayProgress();
            }
        }

        /// <summary>
        /// Modifies the notification icon in the taskbar status area with the updated tooltip text.
        /// </summary>
        private void UpdateTrayProgress()
        {
            if (_isIconAdded)
            {
                SystemTrayInterop.NOTIFYICONDATA nid = CreateNotifyIconData();
                nid.uFlags = SystemTrayInterop.NIF_TIP;
                nid.szTip = GetCurrentTooltipText();
                _ = SystemTrayInterop.Shell_NotifyIcon(SystemTrayInterop.NIM_MODIFY, ref nid);
            }
        }

        /// <summary>
        /// Releases all managed and unmanaged resources, removing the tray icon and unsubclassing window.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            RemoveTrayIcon();

            if (_isSubclassed && _hWnd != IntPtr.Zero && _subclassProc != null)
            {
                _ = SystemTrayInterop.RemoveWindowSubclass(_hWnd, _subclassProc, SUBCLASS_ID);
                _isSubclassed = false;
            }

            _transferQueueService.QueueItems.CollectionChanged -= OnQueueItemsCollectionChanged;
            foreach (TransferQueueItem item in _transferQueueService.QueueItems)
            {
                item.PropertyChanged -= OnQueueItemPropertyChanged;
            }

            if (_hIcon != IntPtr.Zero)
            {
                _ = SystemTrayInterop.DestroyIcon(_hIcon);
                _hIcon = IntPtr.Zero;
            }
        }
    }
}

