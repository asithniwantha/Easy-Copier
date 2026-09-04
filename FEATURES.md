9# 🚀 Easy Copier - Features

## 📚 Library Management
- Scan configured source folders for **Games**, **Apps**, and **Film & TV**.
- Automatically expand folders ending in "collection" to scan their subdirectories.
- Support automatic scanning at startup and on-demand rescanning.
- Display library items in separate tabs (Games / Apps / Film & TV).
- Search and filter items by name.
- Multi-select items and show combined selection size.
- Exclude folders starting with `$`, `recyclebin`, and `System Volume Information` from scanning.
- Use optimized single-pass folder scanning for accurate file sizes and large-file checks.

## 🎨 Visual Presentation
- Cover-art grid view for items.
- Fallback icon for entries without cover images.
- Item size display with human-readable formatting.
- Large-file indicator badge for FAT32 incompatibility risk.
- Right-click context flyout displaying color-formatted system requirements and scrollable folder contents.
- Game pricing tags displayed in the library based on file size thresholds configured in App Settings.
- Total price of selected games shown in the library view based on configured price tiers.
- Smart Adder tool for quick Excel-like calculations during selection and pricing workflows.

## 🔌 Drive Discovery and Selection
- Detect connected removable USB drives.
- Detect USB-attached fixed disks (portable HDD/SSD/NVMe enclosures).
- Show drive details:
  - Drive letter.
  - Volume label.
  - Model/brand.
  - File system.
  - Free space and total capacity.
  - Usage percentage.
- Auto-refresh drive list on attach/remove events.
- Open selected drive directly in File Explorer.

## 🛡️ Transfer Validation
- Validate that items are selected before copying.
- Validate destination free space against required size.
- Validate FAT32 single-file constraints (>4 GB).
- Validate source folder accessibility.
- Warn when destination folders already exist and provide Merge / Replace / Skip options.
- Display validation messages with severity levels (Info / Warning / Error).

## 🚀 Copy Operations
- Copy selected items asynchronously to the target drive.
- Non-blocking transfer queue: add more items to the queue while a copy is in progress.
- Copies are processed in parallel across different target drives.
- Copies targeting the same drive are processed one at a time, in the order they were queued.
- Use the native Windows copy dialog for transfer operations.
- Reserve space for queued/in-progress transfers targeting the same drive so validation reflects true remaining capacity.
- View live queue status (queued, in progress, completed, failed) with per-item details.
- Show the total price of selected games in the copy queue after total size calculation.
- Clear finished (completed/failed) items from the queue view.
- Keep UI responsive during transfers.
- Provide transfer status and completion feedback.
- Refresh drive information after each successful copy completes.

## 📊 History and Reporting
- View detailed history of all past copy operations.
- Track success and failure states, including transfer times.
- Generate and export detailed reports (e.g., CSV) for completed and failed operations.
- Automatically track transfer metrics like operation timestamps and destination details.

## ⚙️ Settings and Persistence
- Manage source folders for Games, Apps, and Media (add/remove).
- Toggle auto-scan on startup.
- Toggle application to start automatically on Windows log-on.
- Persist settings in local app data.
- Persist library cache for faster startup experience.
- Validate cache contents against current filesystem state.
- Configurable price tiers for game size categories.
- Navigational sidebar (`NavigationView`) for organized settings categories (General, Games, etc.).
- Modal-like behavior for secondary windows (Settings, History, About) to prevent main window interaction while open.
- About window with app version, developer details, and repository/issue links.
- Automatic update checking and release notifications, with support for automatic background downloads and manual checking.
- Automatically resize the Settings window to fit content.

## 🏗️ Architecture & Code Quality
- Clean view-model separation enforcing zero View-to-ViewModel UI coupling through rigorous Dependency Injection (completely removing AppServiceLocator).
- Strict adherence to SOLID principles through decoupled, highly-focused service abstractions.
- Proper Dependency Injection flow used to instantiate View Models across pages and windows, eliminating service-locator anti-patterns.
- Optimized the codebase by simplifying large methods and replacing inefficient queries (for example, refactoring parameterless `.Any()` checks to direct `.Count > 0` property checks).
- UI elements decoupled from Services by leveraging `IDispatcherService` and `IWindowService` interfaces (with static window context resolution via `WindowService.MainWindow`).
- Centralized Win32 NativeWindow lifecycle hooks reducing duplicated window initialization logic.
- Secondary modal windows explicitly decouple from static main window contexts, accepting owner parameters directly.
- Implements `IDisposable` effectively for unmanaged resource and cancellation token lifecycle management.
- Asynchronous database operations using `IsDBNullAsync` in SQLite readers to prevent synchronous blocking (CA1849).
- CA and MVVM Toolkit analyzer compliant, leveraging modern C# preview features (`partial` properties) to resolve MVVMTK0045 warnings without project-level suppressions.
- Strict MVVM architecture avoiding UI elements (like `Window`) in ViewModel interfaces and consolidating shared business logic (e.g. folder removals).
- Comprehensive event logging utilizing Serilog to ensure troubleshooting is easy and traceable.
- **📐 Dynamic View Resizing:** The application cleanly abstracts responsive window resizing and UI teardowns (e.g., Settings, History) directly to a unified `NativeWindowHelper`.

## 🛠️ Technical Stack
- WinUI 3 + Windows App SDK.
- C# 14 with .NET 10.
- MVVM pattern via `CommunityToolkit.Mvvm` utilizing source generators (`partial` property observables).
- DI and logging via `Microsoft.Extensions.*` and `Serilog`.
- Storage discovery via `DriveInfo` + WMI.
- GitHub Actions for CI/CD workflows.

## 📅 Future Enhancements (To-Do List)
- [ ] Add transfer profiles and presets for one-click queueing of common game/app/media bundles.
- [ ] Add optional post-copy verification (hash/size) to confirm file integrity.
- [ ] Add automatic best-fit suggestions based on selected drive free space.
- [ ] Add duplicate detection across source libraries with cleanup recommendations.
- [ ] Add retry and resume support for transient copy failures.
- [ ] Add advanced reporting dashboards (daily/weekly totals, most-copied items, and failure-rate trends).
- [ ] Add portable backup/restore for settings, price tiers, source folders, and library cache metadata.
- [ ] Add pause and resume capabilities for active transfers.
- [ ] Add speed throttling for copy operations to limit maximum disk read/write speeds.
- [ ] Add parallel small file transfers for directories with many small files.
- [ ] Add receipt generation and exporting for customer transactions.
- [ ] Add customer or drive profiles using volume serial numbers to track previously copied games.
- [ ] Add advanced pricing and promotional discounts support.
- [ ] Add system tray integration to minimize the app during long copies and push toast notifications.
- [ ] Add native drag-and-drop support from Windows File Explorer to the Copy Queue or Library.
- [ ] Add a real-time transfer speed graph showing current MB/s in the active transfer view.
- [ ] Add cloud backup functionality for the SQLite database to secure historical and financial records.
categories games like shooting racing 