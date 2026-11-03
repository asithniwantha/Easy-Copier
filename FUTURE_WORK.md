# 🔮 Easy Copier - Future Work & Roadmap (M1 – M7)

This document outlines the architectural blueprints, technical specifications, and implementation roadmap for capabilities **M1 through M7**.

---

## 📋 Capabilities Matrix

| ID | Capability | Status | Target Layer | Priority |
|:---|:---|:---:|:---|:---:|
| **M1** | Per-Item Transfer Cancellation | **Completed** ✅ | `Services`, `Interop`, `ViewModels`, `Views` | High |
| **M2** | Retry Action for Failed or Cancelled Queue Items | **Completed** ✅ | `Services`, `ViewModels`, `Views` | High |
| **M3** | Cancellation Token for Bulk Cover & Metadata Downloads | **Completed** ✅ | `Services`, `ViewModels`, `Views` | Medium |
| **M4** | Post-Copy Verification (File Size & Cryptographic Hash) | **Completed** ✅ | `Services`, `Models`, `Settings` | Medium |
| **M5** | History Pagination & Database Streaming | **Completed** ✅ | `Services`, `ViewModels`, `Views` | Medium |
| **M6** | Interactive One-Click Duplicate Cleanup Interface | **Completed** ✅ | `Services`, `Views`, `Dialogs` | Medium |
| **M7** | System Tray Integration & Background Transfer Resilience | **Completed** ✅ | `Infrastructure`, `Views`, `AppLifecycle` | Low |

---

## 🛠️ Detailed Specifications & Technical Blueprints

### M1: Per-Item Transfer Cancellation
* **Status:** **Completed** ✅ *(Implemented via U3)*
* **Architecture:**
  - `TransferQueueItem` exposes `CancellationToken` backed by a dedicated `CancellationTokenSource`.
  - `FileOperationProgressSink` listens to the token and returns `COPYENGINE_E_USER_CANCELLED` (`0x80270000`) from `PreCopyItem` and `UpdateProgress` to immediately abort Windows Shell COM `IFileOperation`.
  - Added `CancelQueueItemCommand` in `MainViewModel` and a red Cancel button on active queue cards in `MainPage.xaml`.

---

### M2: Retry Action for Failed or Cancelled Queue Items
* **Status:** **Completed** ✅ *(Implemented)*
* **Objective:** Enable users to re-enqueue or restart failed or user-cancelled queue jobs directly from the transfer queue without needing to re-browse, re-select items, or re-enter target destination paths.
* **Technical Design:**
  1. **Model Updates (`TransferQueueItem.cs`):**
     - Add `public bool CanRetry => Status is TransferQueueItemStatus.Failed or TransferQueueItemStatus.Cancelled;`
     - Add `public void ResetForRetry()`:
       ```csharp
       _cancellationTokenSource?.Dispose();
       _cancellationTokenSource = new CancellationTokenSource();
       Status = TransferQueueItemStatus.Queued;
       StatusMessage = "Waiting in queue...";
       ProgressPercentage = 0;
       SpeedText = string.Empty;
       RemainingTimeText = string.Empty;
       CompletedAt = null;
       ```
  2. **Service Updates (`ITransferQueueService.cs` & `TransferQueueService.cs`):**
     - Add `void RetryItem(TransferQueueItem item);`
     - Verify destination drive accessibility (`Directory.Exists(item.DestinationPath)`).
     - Call `item.ResetForRetry()`, re-enqueue into `_channel.Writer`, and trigger drive processing loop.
  3. **ViewModel Updates (`MainViewModel.Scanning.cs`):**
     - Add `[RelayCommand] private void RetryQueueItem(TransferQueueItem? item) => _transferQueueService.RetryItem(item!);`
  4. **View Updates (`MainPage.xaml`):**
     - Add an inline Retry button (`\uE72C` Refresh icon) on cards where `CanRetry == true`:
       ```xml
       <Button Grid.Column="2"
               Style="{StaticResource SubtleButtonStyle}"
               Padding="4" Margin="6,0,0,0"
               ToolTipService.ToolTip="Retry failed transfer"
               Visibility="{x:Bind CanRetry, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}"
               Command="{Binding ElementName=RootGrid, Path=DataContext.RetryQueueItemCommand}"
               CommandParameter="{x:Bind}">
           <FontIcon Glyph="&#xE72C;" FontSize="11" Foreground="{ThemeResource AccentFillColorDefaultBrush}"/>
       </Button>
       ```

---

### M3: Cancellation Token for Bulk Cover & Metadata Downloads
* **Status:** **Completed** ✅ *(Implemented via U2)*
* **Architecture:**
  - Added `_downloadGameInfoCts` `CancellationTokenSource` and `CancelDownloadGameInfoCommand` in `SettingsViewModel.cs`.
  - Passes cancellation token through `IGameInfoDownloadService.DownloadGameInfoAsync`.
  - Added `Cancel Download` button in `SettingsWindow.xaml` visible only during active downloads.

---

### M4: Post-Copy Verification (File Size & Cryptographic Hash)
* **Status:** **Completed** ✅ *(Implemented)*
* **Objective:** Verify integrity of copied game/media files to guard against corrupted sectors, USB disconnect glitching, or incomplete writes.
* **Technical Design:**
  1. **Configuration (`AppSettings.cs` & `SettingsViewModel.cs`):**
     - Add `public enum CopyVerificationMode { None, QuickSizeAndCount, FastXxHash64, FullSha256 }`
     - Add `public CopyVerificationMode VerificationMode { get; set; } = CopyVerificationMode.QuickSizeAndCount;`
     - Add UI selector in `SettingsWindow.xaml` under General or Transfer settings.
  2. **Service Layer (`IVerificationService.cs` & `VerificationService.cs`):**
     - `Task<VerificationResult> VerifyCopyAsync(string sourcePath, string destPath, CopyVerificationMode mode, IProgress<double>? progress = null, CancellationToken cancellationToken = default);`
     - **Quick Mode:** Validates that every source file exists at the destination with identical byte lengths.
     - **Fast XxHash64 Mode:** Leverages `System.IO.Hashing.XxHash64` (.NET 10 hardware-accelerated SIMD hashing) using streamed 8 MB buffers.
     - **Full SHA256 Mode:** Computes standard SHA-256 hashes for high-compliance auditing.
  3. **Pipeline Integration (`WindowsShellTransferService.cs`):**
     - After `CopyItemWithFileOperation` succeeds, invoke verification before marking `item.IsSuccess = true`.
     - Surface verification progress (e.g. "Verifying data integrity: 78%...").
     - If mismatch occurs, log exact corrupted file names to SQLite `CopyHistoryRecord.ErrorLog`.

---

### M5: History Pagination & Database Streaming
* **Status:** **Completed** ✅ *(Implemented)*
* **Objective:** Scale the copy history database to hundreds of thousands of operations without UI sluggishness or memory pressure by implementing server-side SQL pagination.
* **Technical Design:**
  1. **SQLite Service (`ICopyHistoryService.cs` & `CopyHistoryService.cs`):**
     - Replace unbounded queries with parameterized range + pagination queries:
       ```sql
       SELECT * FROM CopyHistory
       WHERE (@startDate IS NULL OR Timestamp >= @startDate)
         AND (@endDate IS NULL OR Timestamp <= @endDate)
         AND (@isSuccess IS NULL OR IsSuccess = @isSuccess)
         AND (@search IS NULL OR GameName LIKE @searchPattern)
       ORDER BY Timestamp DESC
       LIMIT @pageSize OFFSET @offset;
       ```
     - Add `Task<int> GetRecordCountAsync(...)` for total page computation.
  2. **ViewModel (`HistoryViewModel.cs`):**
     - Expose `CurrentPage`, `TotalPages`, `PageSize` (default: 50 records), `HasNextPage`, `HasPreviousPage`.
     - Expose commands: `NextPageCommand`, `PreviousPageCommand`, `FirstPageCommand`, `LastPageCommand`.
  3. **View (`HistoryWindow.xaml`):**
     - Add standard bottom pagination toolbar:
       ```xml
       <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Spacing="8" Margin="0,12,0,0">
           <Button Content="&#xE72B;" FontFamily="Segoe MDL2 Assets" Command="{x:Bind ViewModel.PreviousPageCommand}" IsEnabled="{x:Bind ViewModel.HasPreviousPage, Mode=OneWay}"/>
           <TextBlock Text="{x:Bind ViewModel.PaginationSummaryText, Mode=OneWay}" VerticalAlignment="Center" FontWeight="SemiBold"/>
           <Button Content="&#xE72A;" FontFamily="Segoe MDL2 Assets" Command="{x:Bind ViewModel.NextPageCommand}" IsEnabled="{x:Bind ViewModel.HasNextPage, Mode=OneWay}"/>
       </StackPanel>
       ```

---

### M6: Interactive One-Click Duplicate Cleanup Interface
* **Status:** **Completed** ✅ *(Implemented)*
* **Objective:** Upgrade the current text-based duplicate scan report into an interactive management dialog allowing users to review duplicate installations across source folders and delete unneeded copies in one click.
* **Technical Design & Implementation:**
  1. **Domain Models (`DomainModels.cs`):**
     - Added `DuplicateItemEntry(string Name, string FolderPath, LibraryCategory Category, long TotalBytes, DateTime DateCreated)`.
     - Added `DuplicateGroup(string ItemName, IReadOnlyList<DuplicateItemEntry> Entries)`.
  2. **Scanner Service (`ILibraryScannerService.cs` & `LibraryScannerService.cs`):**
     - Added `Task<IReadOnlyList<DuplicateGroup>> GetDuplicateGroupsAsync(AppSettings settings, IProgress<string>? progress = null, CancellationToken cancellationToken = default);`.
     - Refactored `FindDuplicatesReportAsync` to consume structured groups maintaining DRY principle.
  3. **Safe Recycle Bin Deletion Pipeline (`ISafeDeletionService.cs` & `SafeDeletionService.cs`):**
     - Uses native Windows Shell COM `IFileOperation` with `FILEOP_FLAGS.FOF_ALLOWUNDO | FILEOP_FLAGS.FOF_NOCONFIRMATION | FILEOP_FLAGS.FOFX_SHOWELEVATIONPROMPT | FILEOP_FLAGS.FOF_NOERRORUI`.
     - Isolated to background STA threads for thread-safe COM operation.
     - Moves items to the Windows Recycle Bin rather than permanently deleting them.
  4. **ViewModel & View (`DuplicateManagerViewModel.cs` & `DuplicateManagerWindow.xaml`):**
     - Dynamic space calculation updating total recoverable space (e.g. "Recoverable: 142.8 GB • 14 selected").
     - Selection presets: "Keep Newest (Select Older)", "Keep Oldest (Select Newer)", "Deselect All", plus per-group quick buttons.
     - Confirmation dialog with count and size before recycling.
     - Modal host integration via `WindowService.ShowDuplicateManagerWindow()`.

---

### M7: System Tray Integration & Background Transfer Resilience
* **Status:** **Completed** ✅ *(Implemented)*
* **Objective:** Prevent unintentional cancellation of multi-gigabyte transfers when closing the application window. Closing the window minimizes the app to the system notification area (system tray) while transfers continue in the background.
* **Technical Design & Implementation:**
  1. **Infrastructure Service (`ISystemTrayService.cs` & `SystemTrayService.cs`):**
     - Interop via Win32 `Shell_NotifyIcon`, `NOTIFYICONDATA`, and subclassing via `comctl32.dll` (`SetWindowSubclass`, `RemoveWindowSubclass`).
     - Subclasses the main window HWND to process `WM_TRAYICON` messages.
     - Left-click restores the main window using `ShowWindow(SW_RESTORE)` and `SetForegroundWindow`.
     - Right-click shows native popup context menu:
       - **Open Easy Copier** (default / bold)
       - *Separator*
       - **Cancel Active Transfers** (enabled when active transfers exist)
       - *Separator*
       - **Exit Easy Copier**
     - Dynamic tooltip updates on transfer queue changes (e.g. `Easy Copier — 2 transfer(s) (45 MB/s) — 65%` or `Easy Copier — Idle`).
     - Balloon notifications alert user when transfers continue in the background.
  2. **Window Lifecycle Interception (`MainWindow.xaml.cs` & `App.xaml.cs`):**
     - `AppWindow.Closing` checks if active transfers exist (`transferQueueService.QueueItems.Any(i => i.IsActive)`) and `settings.MinimizeToTrayOnClose`.
     - If true: cancels closing (`args.Cancel = true`) and calls `_systemTrayService.MinimizeToTray()`.
     - Clean application shutdown available via tray context menu "Exit Easy Copier".
  3. **Settings Integration (`AppSettings.cs` & `GeneralSettingsPage.xaml`):**
     - Added `MinimizeToTrayOnClose` setting (default `true`) under "Startup & Windows Integration".

---

## 📅 Suggested Implementation Phasing

```
Phase 1: Transfer Robustness & Control
├── M1: Per-Item Transfer Cancellation (Done ✅)
└── M2: Retry Button for Failed/Cancelled Queue Items (Done ✅)

Phase 2: Data Integrity & Storage
├── M3: Cover Download Cancellation (Done ✅)
├── M4: Post-Copy Hash & Size Verification (Done ✅)
└── M5: History Pagination & SQLite Streaming (Done ✅)

Phase 3: Library Hygiene & System Integration
├── M6: One-Click Duplicate Cleanup Interface (Done ✅)
└── M7: System Tray Minimization & Background Resilience (Done ✅)
```

