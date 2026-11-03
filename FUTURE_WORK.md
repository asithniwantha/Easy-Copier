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
| **M6** | Interactive One-Click Duplicate Cleanup Interface | **Open** 🟡 | `Services`, `Views`, `Dialogs` | Medium |
| **M7** | System Tray Integration & Background Transfer Resilience | **Open** 🟡 | `Infrastructure`, `Views`, `AppLifecycle` | Low |

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
* **Status:** **Open** 🟡
* **Objective:** Upgrade the current text-based duplicate scan report into an interactive management dialog allowing users to review duplicate installations across source folders and delete unneeded copies in one click.
* **Technical Design:**
  1. **Domain Models (`DomainModels.cs`):**
     ```csharp
     public record DuplicateItemEntry(string FolderPath, string LibraryCategory, long TotalBytes, DateTime DateCreated, bool IsMarkedForDeletion);
     public record DuplicateGroup(string ItemName, IReadOnlyList<DuplicateItemEntry> Entries);
     ```
  2. **Scanner Service (`ILibraryScannerService.cs`):**
     - Add `Task<IReadOnlyList<DuplicateGroup>> GetDuplicateGroupsAsync(AppSettings settings, CancellationToken cancellationToken);`
  3. **New View & Dialog (`DuplicateManagerDialog.xaml`):**
     - WinUI 3 `ContentDialog` or secondary `Window` containing an `ItemsRepeater` of duplicate groups.
     - Provides quick-selection presets: "Select Older Copies", "Select by Folder", "Deselect All".
     - Shows cumulative recoverable space (e.g. "Recover 142.5 GB").
  4. **Safe Deletion Pipeline:**
     - Uses `IFileOperation.DeleteItem` to send deleted folders to the Windows Recycle Bin rather than permanently deleting them without recovery.

---

### M7: System Tray Integration & Background Transfer Resilience
* **Status:** **Open** 🟡
* **Objective:** Prevent unintentional cancellation of multi-gigabyte transfers when closing the application window. Closing the window minimizes the app to the system notification area (system tray) while transfers continue in the background.
* **Technical Design:**
  1. **Infrastructure Service (`ISystemTrayService.cs` & `SystemTrayService.cs`):**
     - Uses Windows App SDK or Win32 `Shell_NotifyIcon` API to place an Easy Copier icon in the Windows taskbar tray.
     - Context menu options:
       - **Open Easy Copier** (restores main window).
       - **Pause / Resume All Transfers**.
       - **Cancel All Transfers**.
       - **Exit Application**.
     - Dynamic tooltip reporting progress: `Easy Copier — Transferring 3 items (84 MB/s) — 65%`.
  2. **Window Lifecycle Interception (`MainWindow.xaml.cs` & `App.xaml.cs`):**
     - Subscribe to `AppWindow.Closing`:
       ```csharp
       private void MainWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
       {
           if (ViewModel.HasActiveTransfers && Settings.MinimizeToTrayOnClose)
           {
               args.Cancel = true;
               this.Hide();
               _systemTrayService.ShowNotification("Easy Copier", "Transfers are continuing in the background.");
           }
       }
       ```
  3. **Toast Notifications on Completion:**
     - When all background batches finish while minimized, surface a notification allowing the user to click and restore the window.

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
├── M6: One-Click Duplicate Cleanup Interface (Ready 🟡)
└── M7: System Tray Minimization & Background Resilience (Ready 🟡)
```

