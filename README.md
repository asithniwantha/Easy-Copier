# Easy Copier




> A Windows app for quickly and safely copying game libraries and large application files to USB storage.

Easy Copier helps shop environments prepare customer drives without guessing which disk to use. Select games from a visual library, identify the correct USB drive from its details, and copy everything with storage and file-system validation.

## Highlights

| | |
|---|---|
| **USB drive identification** | Shows the drive letter, volume label, physical model/brand, file system, total capacity, and free space. |
| **Broad portable-drive support** | Detects USB flash drives, portable HDDs, and USB NVMe/SSD enclosures—even when Windows reports them as fixed or UASP/SCSI disks. |
| **Safe transfers** | Checks available capacity, source accessibility, duplicate destinations, and FAT32's 4 GB single-file limit before copying. |
| **Visual library** | Displays games in a cover-art grid with size and large-file indicators. |

## Drive Selection

Connected drives are refreshed automatically when storage is attached or removed. Each target-drive entry is displayed as:

```text
E: Customer Drive
Samsung Portable SSD T7 • exFAT
712 GB free of 931 GB
```

The selected-drive panel also shows a usage bar, free space, total capacity, and the drive's identifying details to help avoid copying to the wrong disk.

## Features

### Game library

- Scan configured source folders automatically at startup or on demand.
- Automatically expand folders ending in "collection" to surface individual items.
- Browse games in a responsive cover-art grid.
- Select multiple games and view their combined size before copying.
- Highlight games containing files too large for FAT32 drives.
- Right-click game cards to view a flyout with color-coded system requirements and folder contents.

### Copy operations

- Copy multiple selected games asynchronously without blocking the UI.
- Process copy jobs in parallel when they target different USB drives.
- Keep copy jobs serialized per drive (one at a time per target drive, in queue order).
- Use the Windows native copy dialog during file transfer operations.
- Validate destination capacity, source availability, existing destination folders, and FAT32 compatibility.
- Show transfer status and refresh drive capacity after a successful copy.

### Drive management

- Detect connected USB flash drives and portable USB storage.
- Support portable hard drives and NVMe/SSD USB enclosures that Windows classifies as fixed, SCSI, or UASP devices.
- Display drive letter, label, model/brand, file system, free space, and total size.
- Warn when FAT32 cannot store a selected game's files larger than 4 GB.

### History and Reporting

- View a detailed history of past copy operations.
- Track success, failures, and transfer statuses.
- Generate and export reports (e.g., CSV) containing historical transfer data and logs.

## Technology

| Component | Details |
|---|---|
| Framework | WinUI 3 / Windows App SDK |
| Language | C# 14 with .NET 10 |
| Pattern | MVVM with CommunityToolkit.Mvvm |
| Storage discovery | `DriveInfo` and Windows Management Instrumentation (WMI) |
| CI/CD | GitHub Actions |
| Target platform | x64 |

## Requirements

- Windows 11, version 24H2 or later
- .NET 10 SDK
- Visual Studio 2022 with Windows App SDK / WinUI development tools

![Screenshot](<Easy Copier/Assets/Screenshot 2026-08-06 211103.jpg>)