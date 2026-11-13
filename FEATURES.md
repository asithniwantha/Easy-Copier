# 🚀 Easy Copier - Feature & Developer Guide

This document provides a comprehensive technical overview of the features, architectural components, and developer patterns implemented in **Easy Copier**. It serves as a guide for contributors and developers to understand existing capabilities, architectural boundaries, and implementation details.

---

## 📚 Library Management

- **Multi-Category Source Scanning:** Scans user-configured source directories for **Games**, **Apps**, **Films & TV**, and **OS Images**.
- **Collection Folder Expansion:** Automatically scans subdirectories of folders ending in `"collection"` to surface individual bundled titles as distinct library entries.
- **Startup & Rescan Scheduling:** Supports optional automatic scanning at application launch (`AutoScanOnStartup`) and on-demand rescan triggers across individual tabs or all libraries simultaneously.
- **Decoupled Child Tab Architecture:** Child library tabs (`GamesTabView`, `AppsTabView`, `TvAndFilmsTabView`, `OsImagesTabView`) implement `ILibraryTabView`, allowing the shell to manage tab interactions cleanly without hard references to concrete views.
- **OS Images Management & Sorting:**
  - Dedicated tab displaying `.iso` disk images.
  - Sort by **Name**, **Date Added**, and **Size** with ascending and descending direction toggles.
  - Right-click context flyout (`OsImageDetailsFlyout`) displaying creation, modification, and last access timestamps formatted with culture-aware date/time conventions.
  - Seamless integration with Rufus: launches selected ISO images directly via command-line arguments (`rufus.exe -i "<path>"`).
- **Search & AutoSuggest Filtering:**
  - Fast search filtering with real-time suggestion generation powered by `AutoSuggestBoxSearchBehavior`.
  - Visual feedback indicator with dynamic amber/yellow background tint (`TextControlBackgroundFocused`) when an active search query is applied.
  - Accessible clear search button with an enlarged 40x40px touch/hover target.
- **Game & App Categorization:**
  - Automatically queries Steam Store API metadata to extract game genres and categories, falling back to intelligent keyword heuristics.
  - Dynamic category dropdown filtering across both Games and Apps library views.
- **Unified Media Library Naming:** Standardized terminology to **"Films & TV"** across UI headers, settings controls, view models, and domain models.
- **Multi-Selection & Aggregate Metrics:** Multi-select items across library grids to calculate combined transfer size and game pricing totals.
- **Excluded Folder Protections:** Automatically ignores system directories, including folders starting with `$`, `recyclebin`, and `System Volume Information`.
- **Single-Pass Directory Scanning:** Employs optimized single-pass folder scanning in `GameScannerService` to calculate total byte sizes and evaluate FAT32 4 GB single-file compatibility constraints in a single pass.

---

## 🎨 Visual Presentation & UI Architecture

- **Responsive Cover Art Grid:** Displays items in adaptive XAML grid layouts with responsive card resizing and smooth entry transitions.
- **Intelligent Multi-Service Cover Art Retrieval Pipeline:**
  Automatically queries and downloads vertical cover images in priority order across 8 web services:
  1. **Steam CDN:** Vertical grid covers (`600x900_2x`, `600x900`), falling back to hero headers and landscape capsules.
  2. **CheapShark Aggregator:** Key-less store aggregator resolving Steam App IDs and retail thumbnails.
  3. **GOG Catalog API:** Matches DRM-free titles with custom dimension formatters and WebP/JPG fallbacks.
  4. **PCGamingWiki (PCGW):** MediaWiki search generator API finding covers across varied spelling and subtitle variations.
  5. **Wikipedia / Wikimedia Commons:** MediaWiki search API with page images and REST summary endpoints for video games.
  6. **OpenCritic API:** Queries review aggregator metadata with box art and banner image extraction.
  7. **Lutris API & Search:** REST API with title validation and HTML search fallback using a compile-time generated regex.
  8. **GameSystemRequirements (GSR):** Scrapes game search results using a compile-time generated regex and scores candidate titles.
  - Lutris and GSR root-relative image paths are prefixed with the respective site address after checking the leading slash character.
- **Title Sanitization (`GameTitleSanitizer`):**
  - Strips release tags (`[FitGirl Repack]`, `[DODI]`, etc.), scene group suffixes (`-CODEX`, `-EMPRESS`, `-RUNE`), version strings (`v1.0.4`, `build 12345`), and non-essential bracketed metadata.
  - Produces multi-candidate search variations (full cleaned title, base title without edition, title without year, title without subtitle) to maximize hit rate.
  - **Installment Number Extraction (`ExtractSequelNumber`):** Extracts Roman and Arabic numerals to strictly prevent cross-installment cover mismatches (e.g., preventing *Dark Souls II* from matching *Dark Souls*).
  - **Negative Keyword Rejection:** Automatically rejects search results containing non-game descriptors (`Soundtrack`, `OST`, `Demo`, `Playtest`, `DLC`, `Expansion`, `Server`, `Artbook`).
- **Payload Validation & Self-Healing:**
  - Inspects file headers to validate magic bytes (JPEG, PNG, WebP, BMP).
  - Enforces a 1,500-byte minimum file size threshold to discard HTTP 404 HTML error bodies or 1x1 spacer GIFs.
  - Detects, deletes, and re-downloads corrupt or 0-byte cover images on subsequent passes.
- **Video Thumbnails & Looping Hover Previews:**
  - Standalone video files generate high-resolution thumbnail images via `IShellItemImageFactory` / `ThumbnailService`.
  - Hovering pointer over a Films & TV card for ~500ms triggers an inline, muted, looping preview via `MediaPlayerElement` inside `LibraryCardControl` (starting at 20% timestamp for long videos), with automatic cleanup on pointer exit.
- **Unified Library Card Architecture (`LibraryCardControl`):**
  - Reusable card component encapsulating cover image rendering, FAT32 warning badge, hover-reveal open-folder button, title tooltip, category tags, item size, and price tag.
  - Category-aware fallback glyphs when cover art is unavailable (`\uE7FC` for Games, `\uE71D` for Apps, `\uE714` for Films & TV, `\uE7C5` for OS Images).
  - Implicit WinUI 3 `BrushTransition` and `ScalarTransition` animations for surface elevation and button reveal on pointer hover.
- **Consolidated Single-Slot Notification Banner:**
  - Prioritizes in-app alerts into a single unified top notification slot (`Grid.Row="0"` on `MainPage`): Update Ready to Install > Update Available > Global In-App Notifications (`CanDisplayGlobalNotification`).
  - Eliminates duplicate or stacked InfoBars, maximizing vertical screen space for the library grid.
- **Responsive Action Center Layout:**
  - Employs WinUI 3 `VisualStateManager` with `AdaptiveTrigger` to dynamically adapt Action Center width (400px on screens >= 1200px; 340px on compact screens; 380px default).
- **Windows 11 Mica Backdrop Translucency:**
  - Configured transparent page backgrounds (`ApplicationPageBackgroundThemeBrush`) across dark and light theme dictionaries, rendering native Windows 11 Mica translucency across all windows.
- **Centralized Design Token System (`Styles.xaml`):**
  - Standardized spacing ramp tokens (`SpacingXS` to `SpacingXXL`) and corner radius tokens (`SmallCornerRadius`, `MediumCornerRadius`, `LargeCornerRadius`).
  - Strict elimination of hardcoded colors in favor of semantic theme brushes (`SystemFillColorCriticalBrush`, `SystemFillColorCautionBrush`, `DividerStrokeColorDefaultBrush`, `TextOnAccentFillColorPrimaryBrush`).
- **CommunityToolkit SettingsControls Integration:**
  - Settings pages utilize Windows 11 `SettingsCard` and `SettingsExpander` controls from `CommunityToolkit.WinUI.Controls.SettingsControls` with themed header glyphs, subheadings, right-aligned controls, and interactive command cards.
- **Diagnostic Version Details Copy:**
  - One-click "Copy Version Details" action in `AboutWindow` (`AboutViewModel.CopyVersionInfoCommand`) copies application version, OS build, and runtime details to the clipboard for support and issue reporting.

---

## 🔌 Drive Discovery & Selection

- **Removable Drive Discovery:** Detects connected USB flash drives and external hard drives via `DriveInfo` and Windows Management Instrumentation (WMI).
- **Fixed USB Disk & Enclosure Support:** Identifies USB-connected fixed disks (portable HDD, SSD, and NVMe enclosures) even when Windows reports them as fixed or UASP/SCSI storage.
- **Drive Detail Visualizations:**
  - Drive letter and volume label.
  - Physical model and manufacturer brand name.
  - File system format (NTFS, exFAT, FAT32).
  - Total capacity, available free space, and usage percentage bar.
- **Real-Time Volume Change Monitoring:** Detects drive attachment and removal events automatically via `IDriveCoordinatorService`, complemented by an inline manual refresh button in the drive selection toolbar.
- **Direct Explorer Launch:** Direct action button to open the selected destination drive in Windows File Explorer.
- **Capacity Reservation:** Accounts for queued and in-progress transfers targeting the same drive to calculate true remaining storage capacity before accepting new jobs.

---

## 🛡️ Transfer Validation & Safety

- **Pre-Transfer Checks:**
  - Validates that one or more library entries are selected.
  - Validates destination drive accessibility and available free space against required selection byte size.
  - Validates FAT32 compatibility, checking if any individual file exceeds the 4 GB limit.
  - Validates source folder existence and read permissions.
- **Conflict Detection & Dedicated Dialog (`ConflictDialogContent.xaml`):**
  - Detects if target folders or files already exist at the destination drive before queuing.
  - Presents a modal conflict dialog comparing source and destination items side-by-side (folder size, file count, and timestamps).
  - Supports three resolution actions:
    - **Replace:** Overwrites existing destination files with source files.
    - **Merge:** Copies only missing files into the destination directory without overwriting identical existing files.
    - **Skip:** Leaves existing destination files untouched.

---

## 🚀 Copy Operations & Transfer Engine

- **Native Windows Shell COM Engine:**
  - Utilizes Windows Shell `IFileOperation` COM interfaces (`CLSID_FileOperation`) for native high-throughput file transfers.
  - Implements `IFileOperationProgressSink` for progress callbacks and transfer lifecycle tracking.
- **Parallel Multi-Drive Processing:**
  - Copy queues targeting different destination drives execute concurrently in parallel.
  - Copy queues targeting the same destination drive execute sequentially in strict FIFO queue order to prevent disk thrashing.
- **Per-Item Copy Cancellation (M1):**
  - Active transfer queue items expose a `CancellationToken` backed by a dedicated `CancellationTokenSource`.
  - When cancelled by the user, `FileOperationProgressSink` intercepts the token state and returns `COPYENGINE_E_USER_CANCELLED` (`0x80270000`), immediately halting native Windows Shell file I/O cleanly.
- **Queue Item Retry Action (M2):**
  - Queue cards for failed or user-cancelled transfers display an inline Retry button (`\uE72C`).
  - Calling `RetryItem` validates target drive accessibility, resets transfer metrics via `TransferQueueItem.ResetForRetry()`, generates fresh cancellation tokens, and re-enqueues the item for processing.
- **Real-Time Queue Telemetry:**
  - Displays transfer status, overall percentage progress, current transfer speed (MB/s), remaining time estimates, and pricing tags.
  - Post-transfer capacity refresh updates available drive space upon job completion.

---

## 🛡️ Post-Copy Integrity Verification (M4)

- **Verification Pipeline (`IVerificationService` & `VerificationService`):**
  - Configurable in `AppSettings` via `GeneralSettingsPage.xaml`:
    - **None:** Bypasses post-copy verification.
    - **QuickSizeAndCount:** Verifies destination file existence and matching byte lengths against the source directory.
    - **FastXxHash64:** Hardware-accelerated SIMD hashing using `System.IO.Hashing.XxHash64` streamed in 8 MB chunks for high throughput on large files.
    - **FullSha256:** Cryptographic SHA-256 hash validation for auditing and strict data assurance.
  - Discrepancies are logged with file paths to SQLite `CopyHistoryRecord.ErrorLog`, flagging affected jobs as failed for immediate retry.

---

## 🗑️ Duplicate Manager & Safe Deletion (M6)

- **Interactive Cleanup Window (`DuplicateManagerWindow` & `DuplicateManagerViewModel`):**
  - Replaces legacy static text duplicate prompts with an interactive cleanup dialog launched from Settings or notification prompts.
  - Scans across all configured source directories to identify redundant copies grouped by title via `ILibraryScannerService.GetDuplicateGroupsAsync`.
  - Visual cards display item category glyphs, folder paths, total byte sizes, and creation dates.
- **Smart Selection Presets:**
  - **Keep Newest:** Preserves the most recently created copy and selects older duplicates for removal.
  - **Keep Oldest:** Preserves the baseline original copy and selects newer duplicates for removal.
  - **Deselect All:** Clears all duplicate selections.
  - Live metric computation updates total recoverable disk space and selected item count in real time.
- **Non-Destructive Recycle Bin Deletion (`ISafeDeletionService`):**
  - Executes deletions using Windows Shell `IFileOperation` configured with `FILEOP_FLAGS.FOF_ALLOWUNDO`.
  - Deleted folders are sent to the Windows Recycle Bin rather than permanently deleted, allowing user restoration if needed.
  - Shell COM calls execute within isolated background STA threads to ensure UI responsiveness.

---

## 🔔 System Tray Resilience & Lifecycle (M7)

- **Background Transfer Minimization (`ISystemTrayService` & `SystemTrayInterop`):**
  - When transfers are actively running or queued, clicking the window close button (`[X]`) intercepts closing (`args.Cancel = true`) if `MinimizeToTrayOnClose` is enabled in Settings, minimizing the app into the Windows notification area.
  - Uses native Win32 `Shell_NotifyIconW` and window subclassing via `comctl32.dll` (`SetWindowSubclass`, `RemoveWindowSubclass`) on `MainWindow`'s HWND without disrupting WinUI 3 message routing.
- **System Tray Message Interception (`WM_TRAYICON`):**
  - **Left-Click (`WM_LBUTTONUP`):** Restores and foregrounds `MainWindow` (`ShowWindow(SW_RESTORE)`, `SetForegroundWindow`).
  - **Right-Click (`WM_RBUTTONUP` / `WM_CONTEXTMENU`):** Displays a native Win32 popup menu (`CreatePopupMenu`, `TrackPopupMenuEx`):
    - **Open Easy Copier** (default / bold)
    - *Separator*
    - **Cancel Active Transfers** (enabled when active transfers exist)
    - *Separator*
    - **Exit Easy Copier** (terminates application cleanly)
- **Live Tooltip Telemetry & Notifications:**
  - Observes `TransferQueueService.QueueItems` mutations to update hover tooltip text:
    `Easy Copier — X transfer(s) (Y MB/s) — Z%` or `Easy Copier — Idle`.
  - Emits native Windows balloon notifications when minimized to alert users that background transfers are proceeding.

---

## 📊 History, Analytics & Reporting

- **Database-Backed Transfer Logging:** Persists every completed, failed, or cancelled copy operation to a local SQLite database (`history.db`).
- **Server-Side SQL Pagination & Streaming (M5):**
  - Queries records using parameterized SQL range filters with `LIMIT` and `OFFSET` via `ICopyHistoryService.GetPagedRecordsAsync`.
  - Prevents UI freezes and memory spikes when navigating through extensive history records.
  - Responsive pagination toolbar in `HistoryPage.xaml` with page navigation controls and page size options (25, 50, 100, 200).
- **Inline Record Details Drawer:**
  - `HistoryPage` utilizes a `SplitView` in `DisplayMode="Inline"` so selecting a record opens details alongside the list without obscuring active rows.
  - Inspect transferred byte count, duration, destination drive details, sub-files JSON breakdown, and error logs.
- **Analytics Dashboard (`IHistoryAnalysisService`):**
  - Aggregates operational metrics across 7, 30, and 90-day timeframes.
  - Interactive Daily Volume bar chart with date tooltips and legible day labels.
  - Key Performance Indicators (KPIs): weekly transferred volume, most frequently copied items, and failure rate percentages.
- **CSV Data Export:** Exports filtered or all-time history records to formatted CSV files.

---

## ⚙️ Settings, State & Persistence

- **Modular Settings Architecture:**
  - `SettingsWindow` leverages a WinUI 3 `Frame`-navigated architecture decoupling settings into dedicated pages:
    - `GeneralSettingsPage.xaml`: Startup options, system tray resilience, post-copy verification mode, sound alerts.
    - `GamesSettingsPage.xaml`: Game source folder management, metadata downloading, cover refresh toggles.
    - `AppsSettingsPage.xaml`: App source folder management, configurable standalone app file extensions (`.exe`, `.msi`, `.rar`, etc.).
    - `FilmAndTvSettingsPage.xaml`: Media source folder management, video scanning options.
    - `OsImagesSettingsPage.xaml`: OS image source folder management, Rufus executable resolution and browse picker.
    - `LogsSettingsPage.xaml`: Serilog log viewing and diagnostic log file access.
  - Navigation caching (`NavigationCacheMode.Required`) ensures instantaneous tab switching and retained scroll positions.
- **Context-Aware Deep Linking:** `SettingsOpenAction` parameter routing allows library empty-state buttons to open Settings directly to the matching category configuration page.
- **Persistence:** Settings and library cache snapshots are persisted in `%AppData%\EasyCopier\settings.json` and `%AppData%\EasyCopier\library_cache.json`.
- **In-Memory Settings Caching:** `MainViewModel` caches settings in memory during selection summary calculations to eliminate synchronous disk reads on UI interaction.

---

## 🗄️ Database Schema & Storage

The SQLite database file is located at `%AppData%\EasyCopier\history.db`.

### `CopyHistory` Table

Tracks detailed transfer records for all past copy operations.

| Column Name | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `INTEGER` | `PRIMARY KEY AUTOINCREMENT` | Unique identifier for each transfer record. |
| `Timestamp` | `TEXT` | `NOT NULL` | ISO 8601 formatted timestamp of the transfer operation. |
| `GameName` | `TEXT` | `NOT NULL` | Name of the copied game, application, or media item. |
| `TargetDriveLetter` | `TEXT` | `NOT NULL` | Destination drive letter (e.g., `E:`). |
| `TargetDriveLabel` | `TEXT` | `NOT NULL` | Destination volume label. |
| `BytesTransferred` | `INTEGER` | `NOT NULL` | Total transferred data size in bytes. |
| `IsSuccess` | `INTEGER` | `NOT NULL` | Integer boolean representation (`1` = Success, `0` = Failure). |
| `Amount` | `INTEGER` | `NOT NULL DEFAULT 0` | Price tier value associated with the copied item. |
| `SourcePath` | `TEXT` | `NOT NULL DEFAULT ''` | Full path to the source folder or file on disk. |
| `DestinationPath` | `TEXT` | `NOT NULL DEFAULT ''` | Full path to the target folder or file at the destination. |
| `ErrorLog` | `TEXT` | `NOT NULL DEFAULT ''` | Captured error descriptions, HRESULT codes, or verification failure details. |
| `SubFilesJson` | `TEXT` | `NOT NULL DEFAULT ''` | JSON-serialized array of individual files transferred within the directory. |

#### Database Indices
```sql
CREATE INDEX IF NOT EXISTS idx_copyhistory_timestamp ON CopyHistory (Timestamp DESC);
```

---

### `SmartAdderHistory` Table

Stores calculation logs produced by the Smart Adder tool.

| Column Name | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `INTEGER` | `PRIMARY KEY AUTOINCREMENT` | Unique identifier for each calculation entry. |
| `Timestamp` | `TEXT` | `NOT NULL` | ISO 8601 formatted timestamp of the calculation. |
| `EntriesJson` | `TEXT` | `NOT NULL` | JSON-serialized list of numeric cell inputs and operators. |
| `Total` | `REAL` | `NOT NULL` | Total computed sum of the calculation session. |

---

## 🏗️ Architectural Subsystems & Developer Guide

Easy Copier strictly follows the MVVM pattern and clean architecture principles. The following key abstractions coordinate interactions between layers:

### 🧩 1. Multi-Tab Selection Coordination (`ILibrarySelectionCoordinator`)
- **Problem:** `MainPage.xaml.cs` previously inspected child tab controls directly to aggregate selected items, causing tight view coupling and breaking MVVM boundaries.
- **Solution:** `ILibrarySelectionCoordinator` encapsulates multi-tab selection state, active tab isolation rules (isolating OS Images from games and media), and real-time total byte/price summation. Child tab ViewModels synchronize selections via `LibraryTabViewModelBase.UpdateSelection` and receive clear requests via `ClearRequested` events without parent view intervention.

### ⚡ 2. Declarative Search Behavior (`AutoSuggestBoxSearchBehavior`)
- **Problem:** Search text change, query submission, suggestion population, and background styling logic were duplicated across 12 code-behind methods in tab views.
- **Solution:** A unified WinUI 3 attached XAML behavior attaches to `AutoSuggestBox` controls, binds to `ISearchableTabViewModel`, and handles autocomplete evaluation and visual search styling declaratively.

### 📁 3. File System Abstraction (`IFileSystemService`)
- **Problem:** Direct invocations of `DirectoryInfo`, `FileInfo`, and `Directory.GetFiles` throughout ViewModels impeded unit testing and introduced synchronous disk I/O on UI threads.
- **Solution:** `IFileSystemService` encapsulates path checks, file enumeration, and directory size calculations. File operations are performed asynchronously or offloaded to background threads.

### 🧭 4. Decoupled Navigation Routing (`ISettingsNavigationService`)
- **Problem:** `SettingsWindow.xaml.cs` previously used hardcoded switch statements with concrete page class constructors to handle navigation.
- **Solution:** `ISettingsNavigationService` resolves target navigation tags from `SettingsOpenAction` enums and dynamically resolves matching Page types, decoupling `SettingsWindow` from concrete page views.

### 🪟 5. Modal Window Lifecycle (`ICloseableViewModel` & `CloseableViewModelBase`)
- **Problem:** Modal windows required manual event subscription and unsubscription to handle ViewModel close commands, risking event handler memory leaks.
- **Solution:** `CloseableViewModelBase` implements `ICloseableViewModel` exposing `CloseRequested` and `CloseWindowCommand`. `NativeWindowHelper.InitializeModalWindow` wires up close handling and unhooks automatically upon window closure.

### 📋 6. Platform Clipboard Abstraction (`IClipboardService`)
- **Problem:** `AboutViewModel` directly called WinRT `Windows.ApplicationModel.DataTransfer.Clipboard` APIs, preventing unit testing without a WinRT window context.
- **Solution:** Extracted `IClipboardService` in `Services/` implemented by `ClipboardService` in `Infrastructure/`, allowing clipboard interactions to be mocked.

### 🗑️ 7. Safe Deletion Pipeline (`ISafeDeletionService`)
- **Problem:** File deletion must never cause accidental data loss in shop environments.
- **Solution:** `SafeDeletionService` invokes native Windows Shell `IFileOperation.DeleteItem` with `FILEOP_FLAGS.FOF_ALLOWUNDO | FILEOP_FLAGS.FOF_NOCONFIRMATION | FILEOP_FLAGS.FOFX_SHOWELEVATIONPROMPT`. Deleted items are preserved in the Windows Recycle Bin, and all COM calls run on isolated STA threads.

### 🔔 8. Win32 System Tray Subclassing (`ISystemTrayService` & `SystemTrayInterop`)
- **Problem:** Closing the main window during multi-gigabyte transfers would cancel copy operations.
- **Solution:** Win32 window subclassing intercepts `WM_TRAYICON` and window close events on `MainWindow`, seamlessly minimizing the app to the Windows notification area with dynamic progress tooltips and context menus.

---

## 📅 Future Enhancements Roadmap

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
