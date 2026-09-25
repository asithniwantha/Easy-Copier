# 🚀 Easy Copier

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-WinUI%203-blueviolet)
![Architecture](https://img.shields.io/badge/Architecture-MVVM-success)
![License](https://img.shields.io/badge/License-MIT-green)

**Easy Copier** is a modern, blazing-fast application designed to seamlessly copy and manage large game, app, and media directories. Built from the ground up utilizing the latest **C# 14 & WinUI 3** framework, it delivers a sleek native Windows experience powered by a robust **MVVM** (Model-View-ViewModel) architecture.

> 💾 A Windows app for quickly and safely copying game libraries and large application files to USB storage.

Easy Copier helps shop environments prepare customer drives without guessing which disk to use. Select games, apps, or media from a visual library, identify the correct USB drive from its details, and copy everything with storage and file-system validation.

---

## ✨ Features

* **🎮 Game Categorization & OS Image Integration:** Automatically fetches categories from Steam and applies keyword fallbacks. Filter games by category directly within the Games tab, sort OS images by Name, Date Added, and Size, view creation/modification/access dates via right-click flyout on OS image tiles, browse for Rufus executable path in Settings, and automatically launch ISO images in the latest Rufus update found in the folder.
* **⚡ High-Speed Transfers:** Optimized file I/O operations tailored for handling massive game files and nested directories using native Windows Shell `IFileOperation`.
* **🎨 Modern UI:** A beautiful, responsive interface built with WinUI 3 that feels right at home on Windows 11.
* **📐 Dynamic View Resizing:** The application cleanly abstracts responsive window resizing and UI teardowns (e.g., Settings, History) directly to a unified `NativeWindowHelper`.
* **🔔 Completion Sounds & Notifications:** Plays audio notifications and shows native Windows desktop toast notifications upon success or failure of a transfer queue batch (configurable in settings).
* **🏗️ MVVM Architecture:** A clean, maintainable codebase with strong separation of logic and presentation using decoupled Services, Dependency Properties, and decoupled windowing.
* **📊 Progress Tracking:** Real-time transfer status and queue visibility with per-item details.
* **🛡️ Reliability:** Built-in validation and conflict resolution (Replace, Merge, Skip) for safer transfers with dedicated XAML dialog controls.
* **💸 Game Pricing & Totals:** Size-tier pricing tags plus selected-game totals in both the library and copy queue.
* **🧮 Smart Adder:** Built-in Excel-like calculator support for quick calculations.
* **🔄 App Updates:** Automatic update checking and release notifications, with automatic background downloads and manual checking.
* **📺 Media Support:** Easily browse, select, and copy TV shows and films in addition to games and apps.
* **📜 Logging:** Integrated Serilog logging tracks events and errors with daily rolling files.
* **🧯 Shutdown Stability:** Coordinated watcher and background-callback cleanup reduces close-time crashes during app teardown.

---

## 🌟 Highlights

| Feature | Description |
|---|---|
| 🔍 **USB Drive Identification** | Shows the drive letter, volume label, physical model/brand, file system, total capacity, and free space. |
| 🔌 **Broad Portable-Drive Support** | Detects USB flash drives, portable HDDs, and USB NVMe/SSD enclosures—even when Windows reports them as fixed or UASP/SCSI disks. |
| 🛡️ **Safe Transfers** | Checks available capacity, source accessibility, duplicate destinations, and FAT32's 4 GB single-file limit before copying. |
| 🎮 **Visual Library** | Displays games in a cover-art grid with size/large-file indicators and selected-game pricing totals. |
| 🧮 **Smart Adder** | Provides an Excel-like calculator for fast operational calculations. |
| ⚙️ **Settings UI** | Sidebar-based navigation for configuration, size-to-content window logic, and modal protection. |

---

## 📸 Screenshots

<dl>
  <dd>
    <dl>
      <dd>
        <picture>
          <img width="800" height="450" src="Easy Copier/Assets/Screenshot 2026-08-06 211103.jpg"/>
        </picture>
      </dd>
    </dl>
  </dd>
</dl>

---

## 💾 Drive Selection

Connected drives are refreshed automatically when storage is attached or removed. Each target-drive entry is displayed as:

```text
E: Customer Drive
Samsung Portable SSD T7 • exFAT
712 GB free of 931 GB
```

The selected-drive panel also shows a usage bar, free space, total capacity, and the drive's identifying details to help avoid copying to the wrong disk.

## 🗂️ Library & Copy Operations

### 📚 Game & App Library
- Scan configured source folders automatically at startup or on demand.
- Automatically expand folders ending in "collection" to surface individual items.
- Browse games, apps, and media in a responsive cover-art grid.
- Select multiple items and view their combined size before copying.
- Show the total price of selected games based on configured price tiers.
- Highlight items containing files too large for FAT32 drives.
- Right-click cards to view a flyout with color-coded system requirements and folder contents.
- Use Smart Adder for quick Excel-like calculations.

### 🚀 Copy Operations
- Ask for conflict resolution (Replace, Merge, Skip) before queuing if destination items exist, comparing size and file count using a dedicated `ConflictDialogContent` view.
- Support "Merge" behavior by intelligently copying only missing files to the destination.
- Copy multiple selected items asynchronously without blocking the UI.
- Process copy jobs in parallel when they target different USB drives.
- Keep copy jobs serialized per drive (one at a time per target drive, in queue order).
- Use native Windows Shell `IFileOperation` during file transfer operations.
- Validate destination capacity, source availability, existing destination folders, and FAT32 compatibility.
- Show live queue status and display the total selected-game price in the copy queue after size calculation.
- Refresh drive capacity after successful copy operations.

### 📊 History and Reporting
- View a detailed history of past copy operations.
- Track success, failures, and transfer statuses.
- Generate and export reports (e.g., CSV) containing historical transfer data and logs.

### ⚙️ Settings & Updates
- Configure the application to start automatically on Windows log-on.
- Use a sidebar-based settings experience with modal behavior for secondary windows.
- Access an About window with app version, developer details, and repository/issue links.
- Receive automatic update checks and release notifications, with support for automatic background downloads and manual checking.
- Detect and report duplicate items across all configured source libraries, offering a cleanup recommendation.

## 🛠️ Technology

| Component | Details |
|---|---|
| **Framework** | WinUI 3 / Windows App SDK |
| **Language** | C# 14 with .NET 10 |
| **Pattern** | MVVM with CommunityToolkit.Mvvm (Strict adherence to SOLID principles, dependency injection, and clean view-model separation). Optimized clean code removing inefficient operations. Asynchronous database reads with `IsDBNullAsync`. MVVMTK0045 naturally resolved using preview `partial` properties. |
* **Architecture** | High UI decoupling using `ILibraryTabView` contracts, `ILibraryFilterService` for clean query filtering and sorting, `IFileSystemService` for file system metadata and directory inspection, `IFlyoutService` for UI flyout presentation, and `ConflictDialogContent` views, safely bridging UI-specific operations via abstractions like `IWindowService`. |
| **Storage Discovery** | `DriveInfo` and Windows Management Instrumentation (WMI) |
| **CI/CD** | GitHub Actions |
| **Target Platform** | x64 |

## Development Environment
### What each folder is for
* **ViewModels/**: UI behavior/state and commands (example: `SettingsViewModel`, `SmartAdderViewModel`).
* **Views/**: Windows/pages, user controls, and code-behind (example: `AboutWindow.xaml.cs`, `ConflictDialogContent.xaml`).
* **Services/**: Business/application logic and service contracts (copying, scanning, queueing, settings, etc.; example: `TransferQueueService`).
* **Infrastructure/**: Platform/framework glue: picker wrappers, dispatcher, window helpers, DI registration (example: `FolderPickerService`, `ServiceCollectionExtensions`).
* **Models/**: Domain/data types shared across app layers (example: `GameEntry`, `RemovableDrive`, `AppSettings`).
* **Easy Copier.Tests/**: Unit/integration tests for services/viewmodels/helpers (example: `PickerServicesTests`).
* **docs/**: Generated/static documentation assets.

### How to decide where to add something
1. Is it UI layout/window/control? → `Views/`
2. Is it UI state/command handling? → `ViewModels/`
3. Is it reusable app logic or external interaction? → `Services/`
4. Is it app plumbing (WinUI interop, DI, thread/window abstractions)? → `Infrastructure/`
5. Is it a pure data shape/enum/record? → `Models/`
6. Is it verification for behavior? → `Easy Copier.Tests/`

---

## 💻 Requirements

- Windows 11, version 24H2 or later
- .NET 10 SDK
- Visual Studio 2026 with Windows App SDK / WinUI development tools

---

## 📅 Future Enhancements

- [ ] Add transfer profiles and presets for one-click queueing of common game/app/media bundles.
- [ ] Add optional post-copy verification (hash/size) to confirm file integrity.
- [ ] Add automatic best-fit suggestions based on selected drive free space.
- [x] Add duplicate detection across source libraries with cleanup recommendations.
- [ ] Add retry and resume support for transient copy failures.
- [ ] Add advanced reporting dashboards (daily/weekly totals, most-copied items, and failure-rate trends).
- [ ] Add portable backup/restore for settings, price tiers, source folders, and library cache metadata.

---

## 🤝 Contributing
Contributions, issues, and feature requests are welcome!

Feel free to check the [issues page](https://github.com/asithniwantha/Easy-Copier/issues) if you want to contribute or have suggestions.
1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License
Distributed under the MIT License. See `LICENSE` for more information.

⭐️ **If you find this project helpful or interesting, please consider giving it a star!**

---

## 🏗️ Updated Architecture
* Extracted `IRufusService` / `RufusService` to encapsulate Rufus executable resolution and ISO launching, eliminating process management from `MainViewModel`. 📀
* Streamlined child tab ViewModels (`GamesTabViewModel`, `AppsTabViewModel`, `TvAndFilmsTabViewModel`, `OsImagesTabViewModel`) by introducing `ForwardParentPropertyChanges` in `LibraryTabViewModelBase`. 🔄
* Introduced `IFileSystemService` and `FileSystemService` to encapsulate directory listing, size calculations, and path existence checks, removing direct disk I/O calls from ViewModels. 📁
* Introduced `IGameRequirementsService` and `GameRequirementsService` to encapsulate system requirements parsing and formatting, removing formatting responsibility from `MainViewModel`. 🎮
* Decoupled `IFlyoutService` and `FlyoutService` from `MainViewModel`, utilizing `ICommand` and `IGameRequirementsService` to manage flyouts and folder actions without ViewModel coupling. 🪟
* Extracted `ILibraryFilterService` to encapsulate search text filtering, `GameCategory` filtering, and OS image sorting options into a focused, testable service. 🔍
* Refactored cache snapshot creation out of `MainViewModel` into `ILibraryCacheService.CreateAndSaveSnapshotAsync` to reduce ViewModel complexity and improve SOLID single responsibility. 📦
* Introduced `ILibraryTabView` contract implemented across `GamesTabView`, `AppsTabView`, `TvAndFilmsTabView`, and `OsImagesTabView`, eliminating view-to-view tight coupling in `MainPage.xaml.cs` via dynamic Pivot tab retrieval. 🧩
* Extracted `LibraryViewExtensions` (`GetSelectedEntries` and `ClearMultiSelection`) to eliminate duplicated selection and clearing boilerplate across all child library tab views. ⚡
* Replaced programmatic imperative C# UI construction in `DialogService.ShowConflictDialogAsync` with a dedicated XAML UserControl `ConflictDialogContent.xaml` and clean data bindings. 🎨
* Centralized flyout setup, positioning, and style configuration in `FlyoutHelper.cs` with clean pattern-matching guard clauses. 🛠️
* Applied modern C# features including pattern matching, switch expressions, collection expressions, and event handler lifecycle safety in `MainPage` and `MainViewModel`. ⚡
* Eliminated code-behind event handlers in Windows and Pages (e.g., `Click="Close_Click"`) and replaced them with strongly-typed `ICommand` bindings utilizing the `CommunityToolkit.Mvvm` framework. Event callbacks like `CloseRequested` decouple the ViewModel logic from direct UI window management. 🧹
