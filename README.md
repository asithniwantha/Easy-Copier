# 🚀 Easy Copier

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-WinUI%203-blueviolet)
![Architecture](https://img.shields.io/badge/Architecture-MVVM-success)
![License](https://img.shields.io/badge/License-MIT-green)

**Easy Copier** is a modern, blazing-fast Windows application designed to seamlessly copy and manage large game, app, and media libraries. Built from the ground up utilizing **C# 14 & WinUI 3** on **.NET 10**, it delivers a sleek native Windows experience powered by a robust **MVVM** (Model-View-ViewModel) architecture.

> 💾 A Windows app for quickly and safely copying game libraries and large application files to USB storage.

Easy Copier helps retail and tech shop environments prepare customer drives without guessing which disk to use. Select games, apps, or media from a visual library, identify the correct target USB drive from its physical and volume details, and copy everything with automated storage validation and file-system compatibility checks.

---

## ✨ Features

* **🎮 Game & App Categorization:** Automatically fetches categories from Steam and applies keyword fallbacks for games, with category filtering dropdowns provided across both Games and Apps tabs.
* **📀 OS Images & Rufus Integration:** Dedicated OS Images tab with sorting by Name, Date Added, and Size, tile creation/modification/access timestamp flyouts, Rufus executable path auto-resolution in Settings, and one-click ISO launching in the latest Rufus update found.
* **🖼️ Intelligent Multi-Service Cover Art Retrieval:** Automatically fetches high-resolution vertical cover art from Steam CDN (with resolution and capsule fallbacks), CheapShark, GOG, PCGamingWiki, Wikipedia, OpenCritic, Lutris, and GSR. Includes automated folder name cleaning (`GameTitleSanitizer` stripping repack/scene groups, build numbers, and language tags), edition fallbacks, title match scoring (preventing wrong covers from soundtracks, demos, or DLCs), payload magic-byte validation, automatic self-healing for corrupted covers, cancellation support, and an "Overwrite existing covers" toggle in Settings.
* **⚡ High-Speed Transfers:** Optimized file I/O operations tailored for handling massive game files and nested directories using native Windows Shell `IFileOperation`. Supports per-item cancellation and one-click retry for failed or cancelled jobs.
* **🛡️ Post-Copy Integrity Verification:** Optional post-copy integrity checking configurable in Settings, supporting Quick (size & file count), Fast (`XxHash64` hardware-accelerated SIMD hashing), and Full (`SHA-256`) verification modes.
* **🎨 Modern UI & Component Architecture:** A beautiful, responsive interface built with WinUI 3 that feels right at home on Windows 11. Features authentic Mica backdrop translucency, design tokens for spacing and radii, unified reusable card controls (`LibraryCardControl`) with hover-reveal folder actions, title tooltips, and category-aware fallback glyphs (`\uE7FC` for Games, `\uE71D` for Apps, `\uE714` for Films & TV, `\uE7C5` for OS Images). Integrates Windows Community Toolkit `SettingsCard` and `SettingsExpander` controls across all settings pages (`SettingsWindow` with slide navigation transitions), unified Add/Remove folder management, one-click version diagnostics copy in About, adaptive Action Center width, and consolidated single-slot notifications.
* **🗑️ Interactive Duplicate Manager:** Interactive cleanup interface (`DuplicateManagerWindow`) with dynamic recoverable space calculation, smart presets ("Keep Newest", "Keep Oldest", "Deselect All"), and safe, undoable recycling to the Windows Recycle Bin via native Windows Shell `IFileOperation`.
* **🔔 System Tray Resilience:** Minimizes to the Windows system notification area when closing the window with active transfers (`MinimizeToTrayOnClose`), featuring real-time progress tooltips, balloon notifications, and a native Win32 context menu with restore, cancel, and exit options.
* **📐 Dynamic View Resizing:** Responsive window resizing and modal UI teardowns (e.g., Settings, History, Duplicate Manager) abstracted cleanly through a unified `NativeWindowHelper`.
* **🔔 Completion Sounds & Notifications:** Plays audio alerts and shows native Windows desktop toast notifications upon success or failure of a transfer queue batch (configurable in Settings).
* **🏗️ MVVM Architecture:** Clean, decoupled MVVM architecture utilizing `CommunityToolkit.Mvvm`, dependency injection, and centralized coordinators like `ILibrarySelectionCoordinator` and `ISettingsNavigationService`.
* **📊 Progress Tracking & History Streaming:** Real-time transfer status and queue visibility with per-item details, paired with server-side paginated and streamed transfer history with CSV export.
* **🛡️ Reliability & Conflict Handling:** Built-in validation and conflict resolution (Replace, Merge, Skip) for safer transfers with dedicated XAML dialog controls (`ConflictDialogContent`).
* **💸 Game Pricing & Totals:** Size-tier pricing tags plus selected-game totals in both the library and copy queue.
* **🧮 Smart Adder:** Built-in Excel-like calculator support for quick calculations with persistent SQLite history.
* **🔄 App Updates:** Automatic update checking and release notifications via Velopack, with background downloads and manual checking.
* **📺 Media Support:** Easily browse, select, and copy Films & TV series in addition to games and apps, complete with hover video previews and asynchronous file-list inspection.
* **📜 Logging:** Integrated Serilog logging tracks events and errors with daily rolling files.
* **🧯 Shutdown Stability:** Coordinated watcher, background-callback, and Win32 tray subclassing cleanup ensures crash-free app teardown.

---

## 🌟 Highlights

| Feature | Description |
|---|---|
| 🔍 **USB Drive Identification** | Shows the drive letter, volume label, physical model/brand, file system, total capacity, and free space. |
| 🔌 **Broad Portable-Drive Support** | Detects USB flash drives, portable HDDs, and USB NVMe/SSD enclosures—even when Windows reports them as fixed or UASP/SCSI disks. |
| 🛡️ **Safe Transfers & Verification** | Checks available capacity, source accessibility, duplicate destinations, and FAT32's 4 GB limit before copying, with optional `XxHash64` or `SHA-256` post-copy data verification. |
| 🎮 **Visual Library** | Displays games in a cover-art grid with size/large-file indicators and selected-game pricing totals. |
| 🧮 **Smart Adder** | Provides an Excel-like calculator for fast operational calculations with calculation history. |
| ⚙️ **Settings UI** | Modular WinUI 3 page navigation with cached views, folder list empty-state guidance, deep linking, and modal window management. |
| 🗑️ **Duplicate Manager** | Interactive cleanup interface with dynamic recoverable space calculations, smart presets ("Keep Newest", "Keep Oldest"), and safe undoable recycling to the Windows Recycle Bin. |
| 🔔 **System Tray Resilience** | Minimizes to system tray when closing the window with active transfers, featuring real-time progress tooltips, balloon notifications, and a native context menu. |

---

## 📸 Screenshots

<p align="center">
  <img width="800" src="Easy Copier/Assets/Screenshot 2026-08-06 211103.jpg" alt="Easy Copier Interface Screenshot" />
</p>

---

## 💾 Drive Selection

Connected drives are refreshed automatically when storage is attached or removed. Each target-drive entry is displayed as:

```text
E: Customer Drive
Samsung Portable SSD T7 • exFAT
712 GB free of 931 GB
```

The selected-drive panel also shows a usage bar, free space, total capacity, and the drive's identifying details to help avoid copying to the wrong disk.

---

## 🗂️ Library & Copy Operations

### 📚 Game & App Library
- Scan configured source folders automatically at startup or on demand.
- Automatically expand folders ending in "collection" to surface individual items.
- Browse games, apps, and media in a responsive cover-art grid.
- Select multiple items and view their combined size before copying.
- Show the total price of selected games based on configured price tiers.
- Highlight items containing files too large for FAT32 drives.
- Right-click cards to view a flyout with color-coded system requirements and folder contents.
- Right-click a Films & TV card to see its folder path, total size, and every file with its size.
- Hover over Films & TV cards for an automatic looping video preview.
- Use Smart Adder for quick Excel-like calculations.

### 🚀 Copy Operations
- Quickly open the native Windows Format dialog for a selected removable drive directly from the Action Center.
- Ask for conflict resolution (Replace, Merge, Skip) before queuing if destination items exist, comparing size and file count using a dedicated `ConflictDialogContent` view.
- Support "Merge" behavior by intelligently copying only missing files to the destination.
- Copy multiple selected items asynchronously without blocking the UI.
- Process copy jobs in parallel when they target different USB drives.
- Keep copy jobs serialized per drive (one at a time per target drive, in queue order).
- Use native Windows Shell `IFileOperation` during file transfer operations.
- Cancel any queued or active copy item with immediate COM operation abort.
- Retry failed or cancelled transfer queue items with one click directly from the queue card.
- Perform post-copy data verification (Quick, XxHash64 SIMD, or SHA-256) to ensure data integrity.
- Validate destination capacity, source availability, existing destination folders, and FAT32 compatibility.
- Show live queue status and display the total selected-game price in the copy queue after size calculation.
- Refresh drive capacity after successful copy operations.

### 📊 History and Reporting
- View a detailed history of past copy operations with server-side SQL pagination and streaming.
- Track success, failures, error logs, and transfer durations.
- Interactive analytics dashboard: daily volume bar chart, weekly totals, most-copied items, and failure rate over 7/30/90 days.
- Filter records by date range, success/failure status, and search keywords.
- Generate and export CSV reports containing filtered or complete transfer logs.

### ⚙️ Settings, Hygiene & Updates
- Manage source folders for Games, Apps, Films & TV, and OS Images with empty-state guidance.
- Configure app file extensions (`.exe`, `.msi`, `.rar`, `.zip`, etc.) to scan standalone installation packages in Apps.
- Toggle auto-scan on startup and automatic startup on Windows log-on.
- Detect and manage duplicate items across all configured source libraries via `DuplicateManagerWindow`.
- Access an About window with app version, developer details, diagnostic version details copy, and repository links.
- Receive automatic update checks and release notifications via Velopack with background download support.
- Force refresh or re-download covers with "Overwrite existing covers" toggle and download cancellation support.

---

## 🛠️ Technology

| Component | Details |
|---|---|
| **Framework** | WinUI 3 / Windows App SDK 2.5.1 |
| **Language & Runtime** | C# 14 on .NET 10 (`net10.0-windows10.0.26100.0`) |
| **Pattern** | MVVM with `CommunityToolkit.Mvvm` (Strict adherence to SOLID principles, dependency injection, and clean view-model separation) |
| **Architecture** | Highly decoupled architecture utilizing `ILibrarySelectionCoordinator` for multi-tab selection, `ISettingsNavigationService` for page routing, `AutoSuggestBoxSearchBehavior` for search interactions, `IFileSystemService` for disk abstraction, and `ISystemTrayService` for native tray lifecycle management |
| **Storage Discovery** | `DriveInfo` and Windows Management Instrumentation (WMI) |
| **Data Persistence** | SQLite via `Microsoft.Data.Sqlite` with indexed queries and streaming pagination |
| **Packaging & Updates** | Velopack installer with GitHub Releases integration |
| **CI/CD** | GitHub Actions |
| **Target Platform** | x64 (Windows 10 build 17763+ / Windows 11) |

---

## 💻 Requirements

- Windows 10 (version 1809 / build 17763 or later) or Windows 11 (version 21H2 or later, 24H2 recommended)
- .NET 10 SDK
- Visual Studio 2022 (v17.12 or later) or Visual Studio Preview with Windows App SDK and WinUI 3 development workloads

---

## 📁 Development Environment

### What each folder is for

* **ViewModels/**: UI behavior, state management, and commands (e.g., `MainViewModel`, `SettingsViewModel`, `DuplicateManagerViewModel`, `HistoryViewModel`, `SmartAdderViewModel`).
* **Views/**: Windows, pages, user controls, and flyouts (e.g., `MainWindow.xaml`, `SettingsWindow.xaml`, `HistoryPage.xaml`, `DuplicateManagerWindow.xaml`, `LibraryCardControl.xaml`, `ConflictDialogContent.xaml`).
* **Services/**: Application logic and service contracts (e.g., `TransferQueueService`, `CopyHistoryService`, `VerificationService`, `LibraryScannerService`, `RufusService`).
* **Infrastructure/**: Platform and framework glue: window helpers (`NativeWindowHelper`), tray lifecycle (`SystemTrayService`), dialogs (`DialogService`), flyouts (`FlyoutService`), formatting, and dependency injection (`ServiceCollectionExtensions`).
* **Interop/**: Native Win32 and Windows Shell COM interop definitions (e.g., `SystemTrayInterop.cs`, `IFileOperation`, progress sinks).
* **Behaviors/**: Reusable WinUI 3 XAML behaviors (e.g., `AutoSuggestBoxSearchBehavior`).
* **Converters/**: XAML value converters for data binding (e.g., boolean to visibility, date formatters).
* **Models/**: Domain models, entities, and data contracts (e.g., `DomainModels.cs`, `AppSettings`, `TransferQueueItem`, `NumberCell`).
* **Resources/**: Application styles, design tokens, and XAML theme dictionaries (`Styles.xaml`).
* **Assets/**: Icons, splash screens, notification audio files, and visual media assets.
* **docs/**: Architectural documentation, schemas, and design specifications.

### How to decide where to add something

1. Is it UI layout, a window, page, control, or flyout? → `Views/`
2. Is it UI state, selection handling, or command execution? → `ViewModels/`
3. Is it reusable domain logic, file transfers, scanning, or external APIs? → `Services/`
4. Is it platform glue, WinUI interop, dialogs, or dependency injection? → `Infrastructure/`
5. Is it native Win32 P/Invoke or COM interface definitions? → `Interop/`
6. Is it an attached XAML behavior or trigger? → `Behaviors/`
7. Is it a pure data model, record, or enum? → `Models/`

---

## 📅 Future Enhancements

> 🔮 Detailed technical specifications and architecture blueprints for completed roadmap milestones (M1 through M7) are documented in [FUTURE_WORK.md](FUTURE_WORK.md).

- [x] Add per-item copy cancellation for active and queued transfers (M1).
- [x] Add retry action for failed or cancelled transfer queue items (M2).
- [x] Add cancellation token for bulk cover and metadata downloads (M3).
- [x] Add optional post-copy verification (hash/size) to confirm file integrity (M4).
- [x] Add history pagination and database streaming for large history stores (M5).
- [x] Add duplicate detection across source libraries with cleanup recommendations via Duplicate Manager (M6).
- [x] Add system tray integration to minimize the app during long copies with toast and balloon notifications (M7).
- [x] Add advanced reporting dashboards (daily volume chart, weekly totals, most-copied items, and failure-rate trends).
- [x] Add an OS images tab and instruct Rufus to open with a selected ISO image file.
- [x] Show notifications and audio alerts for batch copy completions.
- [ ] Add transfer profiles and presets for one-click queueing of common game/app/media bundles.
- [ ] Add automatic best-fit suggestions based on selected drive free space.
- [ ] Add portable backup and restore for settings, price tiers, and source folders.
- [ ] Add pause and resume capabilities for active transfers.
- [ ] Add speed throttling for copy operations to limit maximum disk read/write throughput.
- [ ] Add parallel small file transfers for directories with many small files.
- [ ] Add receipt generation and exporting for customer transactions.
- [ ] Add customer or drive profiles using volume serial numbers to track previously copied games.
- [ ] Add native drag-and-drop support from Windows File Explorer into the Copy Queue or Library.
- [ ] Add a real-time transfer speed graph showing current MB/s over time.
- [ ] Add cloud backup functionality for the SQLite database.

---

## 🏗️ Updated Architecture

The Easy Copier codebase has undergone continuous architectural hardening to eliminate technical debt, uphold SOLID principles, and ensure maximum responsiveness and stability. Key architectural subsystems include:

### 🧩 1. UI & View-Model Decoupling
* **`ILibrarySelectionCoordinator`:** Extracted multi-tab selection tracking, active tab exclusivity rules (e.g., isolating OS Images from media selections), and aggregate byte/pricing calculation into a dedicated service, completely removing view-to-view coupling and direct child control traversal in `MainPage`.
* **`AutoSuggestBoxSearchBehavior`:** Encapsulated search text tracking, autocomplete suggestion population, query commits, and active query background brush formatting into an attached XAML behavior, removing duplicated code-behind handlers across child tab views.
* **`LibraryCardControl`:** Unified cover art presentation, hover-reveal folder buttons, FAT32 warning badges, and title tooltips into a single reusable control with smooth implicit transitions (`BrushTransition`, `ScalarTransition`).
* **`ICloseableViewModel` & `CloseableViewModelBase`:** Standardized window closing commands and lifecycle hooks across modal dialogs (`SettingsWindow`, `HistoryWindow`, `DuplicateManagerWindow`), managed automatically by `NativeWindowHelper.InitializeModalWindow`.
* **`IClipboardService`:** Decoupled platform-specific WinRT clipboard operations from `AboutViewModel` for improved testability.
* **`ConflictDialogContent`:** Replaced imperative C# dialog layout generation with a declarative XAML user control and side-by-side comparison cards.

### ⚙️ 2. Settings & Modular Navigation
* **`ISettingsNavigationService`:** Decoupled `SettingsWindow` from concrete page classes, resolving navigation tags dynamically to `Frame` page types (`GeneralSettingsPage`, `GamesSettingsPage`, `AppsSettingsPage`, `FilmAndTvSettingsPage`, `OsImagesSettingsPage`, `LogsSettingsPage`).
* **CommunityToolkit SettingsControls:** Implemented modern Windows 11 `SettingsCard` and `SettingsExpander` controls across all settings views, with cached navigation (`NavigationCacheMode.Required`) for instant page switching.
* **Context-Aware Deep Linking:** Supported opening Settings directly to the relevant category tab when triggered by empty library action buttons via `SettingsOpenAction`.
* **Unified Source Folder Management:** Standardized folder list binding, empty states, and deletion actions across all library categories.

### 📁 3. File System & Discovery Services
* **`IFileSystemService`:** Encapsulated file existence checks, directory enumeration, and recursive size calculations into a testable service, removing direct disk I/O from ViewModels.
* **`FileSystemHelpers.CalculateDirectorySize`:** Iterative queue-based directory traversal with reparse point (symlink/junction) loop protection to eliminate stack overflow hazards on deep directory trees.
* **`IRufusService`:** Encapsulated Rufus executable path discovery and command-line ISO launching (`rufus.exe -i "<path>"`), removing process management from `MainViewModel`.
* **`IGameRequirementsService`:** Decoupled game system requirements retrieval and formatted presentation from `MainViewModel`.
* **`ThumbnailService`:** Managed thread-pool thumbnail extraction using `SemaphoreSlim` concurrency throttling (max 4 concurrent extractions) and `IDisposable` lifecycle safety.

### 🖼️ 4. Cover Art & Metadata Retrieval Pipeline
* **`GameTitleSanitizer`:** Sanitizes raw directory names by stripping scene group tags (`-CODEX`, `[FitGirl Repack]`, etc.), version strings, build numbers, and language flags to generate prioritized search candidates.
* **Multi-Service Search:** Cascades across 8 cover services: Steam CDN, CheapShark, GOG API, PCGamingWiki, Wikipedia, OpenCritic, Lutris, and GSR.
* **Negative Keyword Rejection & Sequel Detection:** Rejects non-game matches (OSTs, DLCs, Demos, Artbooks) and extracts installment numbers (`ExtractSequelNumber`) to avoid cross-installment mismatches (e.g., *Dark Souls II* matching *Dark Souls*).
* **Payload Validation & Self-Healing:** Validates magic bytes (JPEG, PNG, WebP, BMP) and enforces a 1,500-byte minimum payload to reject HTML error pages, automatically purging and re-downloading corrupted covers.
* **Cancellation Support:** Thread-safe bulk cover download cancellation backed by `CancellationTokenSource`.

### 🚀 5. Transfer Engine & System Resilience
* **Windows Shell `IFileOperation`:** Native shell COM file operations provide optimal throughput for multi-gigabyte transfers and complex directory hierarchies.
* **Per-Item Cancellation (M1):** Individual cancellation tokens passed to `FileOperationProgressSink` return `COPYENGINE_E_USER_CANCELLED` to abort transfers cleanly on demand.
* **Queue Item Retry (M2):** One-click retry action directly on queue cards allows users to re-enqueue failed or cancelled jobs with fresh cancellation tokens and destination accessibility verification.
* **Post-Copy Verification (M4):** Integrated `IVerificationService` supporting Quick (size/count), Fast (`XxHash64` SIMD), and Full (`SHA-256`) verification passes, logging corruptions to SQLite.
* **System Tray Resilience (M7):** Window subclassing via Win32 `comctl32.dll` and `Shell_NotifyIconW` intercepts closing when transfers are active, providing real-time tooltip progress, balloon notifications, and an interactive context menu.

### 📊 6. Data Persistence, Reporting & Cleanup
* **`CopyHistoryService` (M5):** Server-side SQLite pagination (`LIMIT`/`OFFSET`) and streaming prevent memory bloat on large history stores, paired with responsive UI page navigation and CSV export.
* **`IHistoryAnalysisService`:** Aggregates transfer statistics (7/30/90 days), daily volume bar charts, and failure trends, keeping presentation logic completely out of the database layer.
* **`ISafeDeletionService` & `DuplicateManagerViewModel` (M6):** Replaces static duplicate text alerts with an interactive duplicate manager window, providing live recoverable space calculation, smart presets, and safe undoable deletion to the Windows Recycle Bin using `FILEOP_FLAGS.FOF_ALLOWUNDO`.

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the [issues page](https://github.com/asithniwantha/Easy-Copier/issues) if you want to contribute or have suggestions.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'feat: Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📝 License

Distributed under the MIT License. See `LICENSE` for more information.

⭐️ **If you find this project helpful or interesting, please consider giving it a star!**
