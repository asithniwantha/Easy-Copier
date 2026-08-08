# Features

## Library Management
- Scan configured source folders for **Games** and **Apps**.
- Support startup auto-scan and on-demand rescanning.
- Display library items in separate tabs (Games / Apps).
- Search/filter items by name.
- Multi-select items and show combined selection size.

## Visual Presentation
- Cover-art grid view for items.
- Fallback icon for entries without cover images.
- Item size display with human-readable formatting.
- Large-file indicator badge for FAT32 incompatibility risk.

## Drive Discovery and Selection
- Detect connected removable USB drives.
- Detect USB-attached fixed disks (portable HDD/SSD/NVMe enclosures).
- Show drive details:
  - drive letter,
  - volume label,
  - model/brand,
  - file system,
  - free space and total capacity,
  - usage percentage.
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
- Persist settings in local app data.
- Persist library cache for faster startup experience.
- Validate cache contents against current filesystem state.

## Technical Stack
- WinUI 3 + Windows App SDK.
- C# with .NET 10.
- MVVM pattern via `CommunityToolkit.Mvvm`.
- DI and logging via `Microsoft.Extensions.*`.
- Storage discovery via `DriveInfo` + WMI.
