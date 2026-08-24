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
- Resolved MVVM Toolkit warning MVVMTK0045 by explicitly applying `[ObservableProperty]` to `partial` properties rather than private backing fields, leveraging C# preview (`<LangVersion>preview</LangVersion>`) for native support without causing older MVVMTK0041 conflicts.
- Refactored `SettingsWindow`, `HistoryWindow`, and `AboutWindow` views to decouple view-logic (such as `SizeChanged` event dynamic resizing) into a dedicated `EnableDynamicResizing` method in `NativeWindowHelper`.
- Resolved and cleaned up static code analysis and build warnings (Roslyn rules CA1515, CA1707, CA1052, CA1063, CA5392, CA1062, CA1822, CA1805, CA1305, CA1307, etc.) to ensure strict compliance with quality, security, and performance standards. This included marking the `CreateFolderContentsView` method in `MainPage.xaml.cs` as static (resolving CA1822), suppressing warning CA5392 in the project files to prevent noise from auto-generated NuGet external dependencies (like the Windows App SDK), and removing the explicit default initialization of `StartOnLogon` in `DomainModels.cs` (resolving warning CA1805).
- Addressed nullable compiler warnings CS8600 and CS8602 in `FileTransferService.cs` by implementing robust validation checks on the iterated `TransferItem` and `GameEntry` objects, guaranteeing null safety and type-safe reference conversions.
- Fixed a compilation error (CS1002 missing semicolon) in `WindowService.cs` in `ShowSettingsWindow` and cleaned up DI service resolution syntax across window creation methods.
- Resolved Roslyn warning CA5392 in CI builds by removing `<NoWarn>` from project files and creating `GlobalSuppressions.cs` with assembly-level `[assembly: SuppressMessage(...)]` attributes to cleanly handle external auto-generated code in the WindowsAppSDK NuGet package (`UndockedRegFreeWinRT-AutoInitializer.cs`).
- Resolved CI build compilation errors (CS1061) in `SettingsViewModel.cs` by adding missing `using Microsoft.Extensions.Logging;` directive for `ILogger` extension methods (`LogInformation`, `LogWarning`, `LogError`).
- Resolved Roslyn warnings CA1307 and CA1822 in `AboutViewModel.cs` by specifying `StringComparison.Ordinal` in `version.IndexOf('+')` and suppressing CA1822 on instance ViewModel properties used in XAML data bindings.
- Resolved Roslyn warning CA1806 in `NativeWindowHelper.cs` and `Program.cs` by explicitly discarding return values of `GetWindowThreadProcessId` and `new App()` instantiation.
- Resolved Roslyn warning CA1062 in `LibraryScannerService.cs` by adding explicit `ArgumentNullException.ThrowIfNull(settings);` parameter validation to `ScanAllLibrariesAsync`.

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
- Implemented an update-checking and notification system using Velopack and GitHub Releases, with an option to automatically download and apply updates in the background. Disabling auto-generated XAML `Main` method via `<DISABLE_XAML_GENERATED_MAIN>true</DISABLE_XAML_GENERATED_MAIN>` in `.csproj` to support early Velopack initialization in `Program.cs`. Integrated `vpk pack` into the GitHub Actions release workflow to publish Velopack artifacts natively.
