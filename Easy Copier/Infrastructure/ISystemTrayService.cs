using Microsoft.UI.Xaml;
using System;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Defines services for managing the application notification icon in the Windows taskbar system tray.
    /// </summary>
    public interface ISystemTrayService : IDisposable
    {
        /// <summary>
        /// Initializes the system tray service with the primary application window handle and hooks message subclassing.
        /// </summary>
        /// <param name="mainWindow">The primary application window.</param>
        void Initialize(Window mainWindow);

        /// <summary>
        /// Displays or updates the notification icon in the system tray.
        /// </summary>
        void ShowTrayIcon();

        /// <summary>
        /// Removes the notification icon from the system tray area.
        /// </summary>
        void RemoveTrayIcon();

        /// <summary>
        /// Displays an informational balloon tip or desktop notification from the system tray icon.
        /// </summary>
        /// <param name="title">The notification title string.</param>
        /// <param name="message">The notification body message string.</param>
        void ShowNotification(string title, string message);

        /// <summary>
        /// Minimizes the main window to the system tray and presents a notification if background transfers are active.
        /// </summary>
        void MinimizeToTray();

        /// <summary>
        /// Restores and activates the main window from the system tray to the foreground.
        /// </summary>
        void RestoreMainWindow();

        /// <summary>
        /// Gets a value indicating whether the system tray icon is currently active in the taskbar.
        /// </summary>
        bool IsTrayIconVisible { get; }
    }
}

