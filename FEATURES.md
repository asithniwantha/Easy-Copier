# Features

## Library Management
- Scan configured source folders for **Games** and **Apps**.
- Automatically expand folders ending in "collection" to scan their subdirectories.
- Support startup auto-scan and on-demand rescanning.
- Display library items in separate tabs (Games / Apps).
- Search/filter items by name.
- Multi-select items and show combined selection size.
- Exclude folders starting with `$`, `recyclebin`, and `System Volume Information` from scanning.

## Visual Presentation
- Cover-art grid view for items.
- Fallback icon for entries without cover images.
- Item size display with human-readable formatting.
- Large-file indicator badge for FAT32 incompatibility risk.
- Right-click context flyout displaying color-formatted system requirements and scrollable folder contents.
- Game pricing tags displayed in the library based on file size thresholds configurable via App Settings.

## Drive Discovery and Selection
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

## Transfer Validation
- Validate that items are selected before copying.
- Validate destination free space against required size.
- Validate FAT32 single-file constraints (>4 GB).
- Validate source folder accessibility.
- Warn when destination folders already exist (merge/overwrite scenario).
- Display validation messages with severity levels (Info/Warning/Error).

## Copy Operations
- Copy selected items asynchronously to target drive.
- Non-blocking transfer queue: add more items to the queue while a copy is in progress.
- Copies are processed in parallel across different target drives.
- Copies targeting the same drive are processed one at a time, in the order they were queued.
- Use the native Windows copy dialog for transfer operations.
- Reserves space for queued/in-progress transfers targeting the same drive so validation reflects true remaining capacity.
- View live queue status (queued, in progress, completed, failed) with per-item details.
- Clear finished (completed/failed) items from the queue view.
- Keep UI responsive during transfer.
- Provide transfer status and completion feedback.
- Refresh drive information after each successful copy completes.

## History and Reporting
- View detailed history of all past copy operations.
- Track success and failure states, including transfer times.
- Generate and export detailed reports (e.g., CSV) for completed and failed operations.
- Automatically track transfer metrics like operation timestamps and destination details.

## Settings and Persistence
- Manage source folders for Games and Apps (add/remove).
- Toggle auto-scan on startup.
- Toggle application to start automatically on Windows log-on.
- Persist settings in local app data.
- Persist library cache for faster startup experience.
- Validate cache contents against current filesystem state.
- Configurable price tiers for game size categories.
- Navigational sidebar (NavigationView) for organized settings categories (General, Games, etc.).
- Modal-like behavior for secondary windows (Settings, History) to prevent main window interaction while open.

## Architecture & Code Quality
- Clean view-model separation enforcing zero View-to-ViewModel UI coupling.
- Strict adherence to SOLID principles through decoupled, highly-focused service abstractions.
- Centralized Win32 NativeWindow lifecycle hooks reducing duplicated window initialization logic.
- Implements `IDisposable` effectively for unmanaged resource and cancellation token lifecycle management.
- CA and MVVM Toolkit analyzer compliant, leveraging modern C# static methods and configure awaits.

## Technical Stack
- WinUI 3 + Windows App SDK.
- C# 14 with .NET 10.
- MVVM pattern via `CommunityToolkit.Mvvm` utilizing source generators (`partial` property observables).
- DI and logging via `Microsoft.Extensions.*`.
- Storage discovery via `DriveInfo` + WMI.
- GitHub Actions for CI/CD workflows.

## Future Enhancements (to do list)
- Run the provided python script to download the cover images and minimum requirements for the games/apps that don't have cover images and requirement file.-added
- Add github actions to build and publish the app to github releases automatically with versioning number based on git tags.-added
- add tv series and films tab -added
- add excel like calculator 
- if the destination drive already contain the folder we tring to coping, ask replace everytinh or merge or do nothing. (compair full size and file count also)
- add option to copy only the missing files from the source folder to the destination folder if the destination folder already exist.
- Right-click context flyout display 
  - Color-formatted system requirements.
  - Scrollable folder contents with their details(size).
  - with bigger flyout window to show more details.
-add about page with app version, developer info, and links to github repo and issues page.
-implement update checking and notification system to inform users of new releases. or automatically download and install updates.