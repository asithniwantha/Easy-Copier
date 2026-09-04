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

* **⚡ High-Speed Transfers:** Optimized file I/O operations tailored for handling massive game files and nested directories.
* **🎨 Modern UI:** A beautiful, responsive interface built with WinUI 3 that feels right at home on Windows 11.
* **📐 Dynamic View Resizing:** The application cleanly abstracts responsive window resizing and UI teardowns (e.g., Settings, History) directly to a unified `NativeWindowHelper`.
* **🏗️ MVVM Architecture:** A clean, maintainable codebase with strong separation of logic and presentation.
* **📊 Progress Tracking:** Real-time transfer status and queue visibility with per-item details.
* **🛡️ Reliability:** Built-in validation and conflict resolution (Replace, Merge, Skip) for safer transfers.
* **💸 Game Pricing & Totals:** Size-tier pricing tags plus selected-game totals in both the library and copy queue.
* **🧮 Smart Adder:** Built-in Excel-like calculator support for quick calculations.
* **🔄 App Updates:** Automatic update checking and release notifications, with automatic background downloads and manual checking.
* **📺 Media Support:** Easily browse, select, and copy TV shows and films in addition to games and apps.
* **📜 Logging:** Integrated Serilog logging tracks events and errors with daily rolling files.

---

## 🌟 Highlights

| | |
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
- Ask for conflict resolution (Replace, Merge, Skip) before queuing if destination items exist, comparing size and file count.
- Support "Merge" behavior by intelligently copying only missing files to the destination.
- Copy multiple selected items asynchronously without blocking the UI.
- Process copy jobs in parallel when they target different USB drives.
- Keep copy jobs serialized per drive (one at a time per target drive, in queue order).
- Use the Windows native copy dialog during file transfer operations.
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

## 🛠️ Technology

| Component | Details |
|---|---|
| **Framework** | WinUI 3 / Windows App SDK |
| **Language** | C# 14 with .NET 10 |
| **Pattern** | MVVM with CommunityToolkit.Mvvm (Strict adherence to SOLID principles, dependency injection, and clean view-model separation). Optimized clean code removing inefficient operations. Asynchronous database reads with `IsDBNullAsync`. MVVMTK0045 naturally resolved using preview `partial` properties. |
| **Architecture** | High UI decoupling (e.g. secondary window abstractions without static coupling to App.MainWindow, using static `WindowService.MainWindow` for window context access), safely bridging UI-specific operations via abstractions like `IWindowService`. |
| **Storage Discovery** | `DriveInfo` and Windows Management Instrumentation (WMI) |
| **CI/CD** | GitHub Actions |
| **Target Platform** | x64 |

## 💻 Requirements

- Windows 11, version 24H2 or later
- .NET 10 SDK
- Visual Studio 2026 with Windows App SDK / WinUI development tools

---

## 📅 Future Enhancements

- [ ] Add transfer profiles and presets for one-click queueing of common game/app/media bundles.
- [ ] Add optional post-copy verification (hash/size) to confirm file integrity.
- [ ] Add automatic best-fit suggestions based on selected drive free space.
- [ ] Add duplicate detection across source libraries with cleanup recommendations.
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

## Updated Architecture
* Separated UI interactions in ViewModels using IAppWindowContext.
* Split large ViewModels and Services into partial classes.
