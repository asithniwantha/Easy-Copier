# Development History

## Overview
Easy Copier was developed as a WinUI 3 desktop application targeting .NET 10 to make USB-based game/app library copying safer and faster in shop-style workflows.

## Major Development Actions
- Set up a WinUI 3 app architecture with MVVM using `CommunityToolkit.Mvvm`.
- Added dependency injection and app-wide logging infrastructure.
- Implemented library management for two categories: **Games** and **Apps**.
- Built asynchronous scanning for configured source folders.
- Added folder size calculation and cover image discovery.
- Added large-file detection to identify FAT32-incompatible items (>4 GB).
- Implemented persistent settings storage in `%LocalAppData%\EasyCopier\appsettings.json`.
- Added library cache persistence in `%LocalAppData%\EasyCopier\library_cache.json`.
- Implemented cache validation to detect:
  - configuration/source-folder changes,
  - missing folders,
  - added/removed/changed library items.
- Implemented USB drive discovery and automatic refresh on device changes.
- Added enhanced USB detection for portable SSD/HDD/NVMe enclosures that may appear as fixed/SCSI drives.
- Implemented transfer validation before copy operations.
- Implemented copy operations via Windows shell API (`SHFileOperation`).
- Added selection summaries, validation messages, transfer status feedback, and empty-state UX.
- Added settings window behavior with owner-window centering and modal-like input blocking.
- Refined Settings window navigation by implementing a sidebar (NavigationView) to categorize options cleanly.
- Implemented configurable game pricing tags that calculate estimated prices based on per-GB file size thresholds.

## Key Development Decisions
- **Safety-first transfer pipeline**: run validation before copy; block operation on hard errors.
- **Use shell copy API** instead of manual stream copy to align with Windows-native file copy behavior.
- **Cache + fingerprint model** to improve startup speed while still detecting source changes.
- **Path normalization strategy** for consistent cache comparisons.
- **Treat inaccessible/errored cache items as changed** to avoid stale library state.
- **Support broader USB device classes** beyond `DriveType.Removable` for real-world portable media.
- **Non-blocking UX**: keep scanning/transfers asynchronous and update status incrementally.

## Platform and Packaging
- Target framework: `.NET 10` (`net10.0-windows10.0.26100.0`).
- Platform: `x64`.
- Windows App SDK + MSIX tooling enabled for packaging/publishing workflows.
