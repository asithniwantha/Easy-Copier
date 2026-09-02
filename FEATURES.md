# 🚀 Easy Copier - Features

## 📚 Library Management
- Scan configured source folders for **Games**, **Apps**, and **Film & TV**.
- Automatically expand folders ending in "collection" to scan their subdirectories.
- Support startup auto-scan and on-demand rescanning.
- Display library items in separate tabs (Games / Apps / Film & TV).
- Search/filter items by name.
- Multi-select items and show combined selection size.
- Exclude folders starting with `$`, `recyclebin`, and `System Volume Information` from scanning.
- Optimized single-pass folder scanning for accurate file sizes and large-file checks.

## 🎨 Visual Presentation
- Cover-art grid view for items.
- Fallback icon for entries without cover images.
- Item size display with human-readable formatting.
- Large-file indicator badge for FAT32 incompatibility risk.
- Right-click context flyout displaying color-formatted system requirements and scrollable folder contents.
- Game pricing tags displayed in the library based on file size thresholds configurable via App Settings.

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
- Warn when destination folders already exist (Merge / Replace / Skip resolution).
- Display validation messages with severity levels (Info / Warning / Error).

## 🚀 Copy Operations
- Copy selected items asynchronously to the target drive.
- Non-blocking transfer queue: add more items to the queue while a copy is in progress.
- Copies are processed in parallel across different target drives.
- Copies targeting the same drive are processed one at a time, in the order they were queued.
- Use the native Windows copy dialog for transfer operations.
- Reserves space for queued/in-progress transfers targeting the same drive so validation reflects true remaining capacity.
- View live queue status (queued, in progress, completed, failed) with per-item details.
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
- Automatically resizing Settings window to fit content perfectly.

## 🏗️ Architecture & Code Quality
- Clean view-model separation enforcing zero View-to-ViewModel UI coupling through rigorous Dependency Injection (completely removing AppServiceLocator).
- Strict adherence to SOLID principles through decoupled, highly-focused service abstractions.
- Proper Dependency Injection flow used for instantiating View Models across pages and windows, eliminating anti-pattern Service Locators.
- Optimized clean code base by simplifying large methods and strictly substituting inefficient queries (e.g. refactoring `.Any()` parameterless checks to `.Count > 0` directly on properties) for enhanced compliance.
- UI elements decoupled from Services by leveraging `IDispatcherService` and `IWindowService` interfaces (with static window context resolution via `WindowService.MainWindow`).
- Centralized Win32 NativeWindow lifecycle hooks reducing duplicated window initialization logic.
- Secondary modal windows explicitly decouple from static main window contexts, accepting owner parameters directly.
- Implements `IDisposable` effectively for unmanaged resource and cancellation token lifecycle management.
- Asynchronous database operations using `IsDBNullAsync` in SQLite readers to prevent synchronous blocking (CA1849).
- CA and MVVM Toolkit analyzer compliant, leveraging modern C# preview features (`partial` properties) to naturally resolve MVVMTK0045 warnings without project-level suppressions.
- Strict MVVM architecture avoiding UI elements (like `Window`) in ViewModel interfaces and consolidating shared business logic (e.g. folder removals).
- Comprehensive event logging utilizing Serilog to ensure troubleshooting is easy and traceable.

## 🛠️ Technical Stack
- WinUI 3 + Windows App SDK.
- C# 14 with .NET 10.
- MVVM pattern via `CommunityToolkit.Mvvm` utilizing source generators (`partial` property observables).
- DI and logging via `Microsoft.Extensions.*` and `Serilog`.
- Storage discovery via `DriveInfo` + WMI.
- GitHub Actions for CI/CD workflows.

## 📅 Future Enhancements (To-Do List)
- [x] Handle copy collisions: if the destination drive already contains the folder, ask to replace everything, merge, or do nothing (comparing size and file count).
- [x] Add option to copy only missing files from the source to the destination if the destination folder already exists ("Merge").
- [x] Add About page with app version, developer info, and links to GitHub repo and issues page.
- [x] Add an Excel-like calculator tool (SmartAdder overlay for dynamic addition and calculation history logging).
- [x] Implement update checking and notification system to inform users of new releases, or automatically download and install updates.
- [ ] Show total price of selected games in the library view, based on configured price tiers. and after that, show the total price of all selected games in the copy queue view after the total size of the selected games is calculated. 

* **📐 Dynamic View Resizing:** The application cleanly abstracts responsive window resizing and UI teardowns (e.g., Settings, History) directly to a unified `NativeWindowHelper`.