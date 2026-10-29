# Changelog

All notable changes to the **InternetMeter** Windows traffic monitor project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.9.0] — 2026-10-29

### Added
- **Monthly Data Cap & Hotspot Quota Alerts with Toast Warnings**:
  - **Leap-Safe Billing Cycle Range Engine**:
    - Automatically computes cycle start and end boundaries handling varying month lengths (28, 29, 30, 31 days) and leap years.
    - Accurately clamps configurable reset day ($1 - 31$) against month boundaries (e.g. Day 31 resets safely on Feb 28/29 or Apr 30).
    - Aggregates historical traffic usage from [`TrafficJournalService`](file:///c:/GitHub/InternetMeter/Services/TrafficJournalService.cs) spanning the exact billing window.
  - **Multi-Tiered Action Center Toast Notifications**:
    - Dispatches Windows Action Center toast notifications at configurable consumption milestones:
      - **Warning Alert** (Default: $80\%$): Amber warning notification.
      - **Critical Alert** (Default: $95\%$): High-priority warning notification.
      - **Exceeded Alert** ($100\%$): Crimson quota breach notification.
    - Built-in single-fire deduplication state machine (`LastNotifiedThreshold`, `LastNotifiedCycleStart`) ensuring each alert tier triggers exactly once per billing period without annoying repeated popups.
  - **HUD Frosted Glass Progress Gauge & Micro Ribbon Alert**:
    - **Full HUD**: Frosted glass horizontal progress bar placed cleanly between upload metrics and session statistics, displaying formatted usage (e.g. `68.4 / 100 GB (68.4%) — 16 days left`) with dynamic status coloring (`#10B981` Emerald $\to$ `#F59E0B` Amber $\to$ `#EF4444` Crimson).
    - **Compact Ribbon**: Subtle warning indicator badge (`⚠ Cap`) appearing automatically when usage crosses warning or critical thresholds.
    - **Interactive 50ms Frosted Diagnostics Flyout Card**: Hovering over the data cap gauge displays full cycle details: Cycle Date Range, Total Quota, Used Volume, Remaining Allowance, Current Usage %, Days Remaining in Cycle, Recommended Daily Pace Budget, and the Top Transmitting Application.
  - **Dedicated Configuration Modal Dialog ([`Views/DataCapConfigWindow.xaml`](file:///c:/GitHub/InternetMeter/Views/DataCapConfigWindow.xaml))**:
    - Modern dark acrylic frosted glass dialog with preset quota chips (`25 GB`, `50 GB`, `100 GB`, `250 GB`, `500 GB`, `1 TB`, `2 TB`).
    - Custom billing cycle reset day slider ($1 - 31$) with dynamic live preview calculating exact start and end calendar dates.
    - Configurable warning ($50\% - 90\%$) and critical ($85\% - 99\%$) slider thresholds.
    - Toggle switch for Windows Action Center toast notifications.
    - Draggable window header, keyboard escape shortcut, and instant settings persistence.
  - **Context Menu & System Tray Integration**:
    - Added `Monthly Data Cap & Quota...` menu option in both Widget Right-Click Context Menu and Windows System Tray Menu.
    - Direct click on the Full HUD data cap bar instantly opens the configuration dialog.
  - **Unit Test Coverage**:
    - Added [`Tests/DataCapServiceTests.cs`](file:///c:/GitHub/InternetMeter/Tests/DataCapServiceTests.cs) validating leap-year cycle math, threshold escalations, single-fire alert deduplication, volume unit formatting, and ViewModel command bindings (**105 passing tests**).

---

## [1.8.0] — 2026-10-29

### Added
- **Gaming & VoIP Network Health Monitor (Jitter + Packet Loss + Bufferbloat)**:
  - **RFC 3550 Interarrival Jitter Smoothing**:
    - Calculates packet delay variation using IETF standard low-pass exponential smoothing:
      $$J_i = J_{i-1} + \frac{|D(i-1, i)| - J_{i-1}}{16}$$
    - Categorizes jitter in real time: `Smooth (Optimal)` ($\le 2.5\text{ ms}$), `Moderate (Acceptable)` ($2.5 - 8.0\text{ ms}$), or `Spiking (Noticeable)` ($> 8.0\text{ ms}$).
  - **20-Sample Sliding Window Packet Loss Tracker**:
    - Continuously tracks probe timeout percentages across the last 20 probes ($0.0\%$ to $100\%$) with dynamic tier coloring.
  - **Real-Time Bufferbloat Early Warning Engine**:
    - Calibrates baseline resting latency during idle traffic periods ($< 1.0\text{ MB/s}$).
    - Detects router queue saturation and bufferbloat under heavy upload/download bandwidth ($\ge 2.5\text{ MB/s}$), computing exact latency penalties (e.g. `+52 ms`) and rendering a warning indicator (`⚠ 82 ms`) directly on the HUD badge.
  - **Composite Network Quality Grading**:
    - Evaluates connection health against competitive gaming standards: **A+** (Esports Ready), **A** (Gaming & 4K Streaming), **B** (VoIP & Web), **C** (Noticeable Lag), **D** (Severe Stutter), **F** (Connection Dropped).
  - **Interactive Frosted Telemetry Flyout Card**:
    - Hovering over the latency badge in either the **Full HUD** or **Compact Micro Ribbon** displays a dark frosted glass diagnostic flyout showing Current Latency, Min/Max/Avg RTT, Jitter ($\pm\text{ms}$), Packet Loss ($\% 0/20$), Bufferbloat queue status, Target Host (`1.1.1.1`), and the composite Grade Badge.
  - **Unit Test Coverage**:
    - Added [`Tests/NetworkHealthTests.cs`](file:///c:/GitHub/InternetMeter/Tests/NetworkHealthTests.cs) verifying RFC 3550 jitter calculation convergence, sliding window packet loss, bufferbloat heuristic triggers, and grade classification (suite expanded to **95 passing tests**).

---

## [1.7.0] — 2026-10-29

### Added
- **Rolling 60-Minute Detailed Bandwidth Analytics Window**:
  - **Multi-Resolution Timeframes**:
    - Interactive timeline switching between `15 Minutes` (900s), `30 Minutes` (1,800s), and `60 Minutes` (3,600s).
    - Responsive X-axis markers dynamically recalibrate labels based on selected window (`-60m` to `Now`, `-30m` to `Now`, or `-15m` to `Now`).
  - **High-Performance In-Memory Ring Buffer**:
    - Engineered [`RollingHistoryService`](file:///c:/GitHub/InternetMeter/Services/RollingHistoryService.cs) maintaining a thread-safe circular array of up to 3,600 one-second bandwidth snapshots ($< 1.5\text{ MB}$ footprint).
    - Ingests physical NIC throughput, per-second byte counts, and correlates the top active transmitting application at every sampling tick.
  - **Hardware-Accelerated Vector Canvas**:
    - Built [`Views/BandwidthAnalyticsWindow.xaml`](file:///c:/GitHub/InternetMeter/Views/BandwidthAnalyticsWindow.xaml) utilizing WPF `StreamGeometry` vector paths with semi-transparent vertical area fills (`LinearGradientBrush`).
    - Dynamic adaptive Y-axis scaling algorithm ([`CalculateNiceYMax`](file:///c:/GitHub/InternetMeter/ViewModels/BandwidthAnalyticsViewModel.cs)) snapping to human-readable grid intervals (`10 MB/s`, `25 MB/s`, `50 MB/s`, `100 MB/s`, etc.).
    - Inherits user's chosen theme palette (`Emerald & Sky`, `Cyberpunk Neon`, `Monochrome Stealth`, `Solar Amber`) with full frosted glass backdrop acrylic support.
  - **Interactive Crosshair Scrubber & Inspection Cursor**:
    - Hovering over the graph displays a vertical dashed crosshair with dual highlight anchor dots tracking download and upload coordinates.
    - Floating frosted glass tooltip displays exact timestamp (`HH:mm:ss`), download rate, upload rate, and the top transmitting application (e.g., `brave.exe (48.1 MB/s)`).
  - **Hero Telemetry Metric Strip**:
    - Four real-time summary cards: **Peak Download** (with timestamp and app attribution), **Peak Upload**, **Total Volume** (cumulative download + upload data moved in the timeframe), and **Average Throughput**.
  - **Instant CSV Snapshot Export**:
    - One-click RFC 4180 CSV export generating detailed per-second diagnostics dumped directly to `%USERPROFILE%\Downloads` with automatic Windows Explorer file reveal.
  - **Global Hotkey & Menus**:
    - Registered Win32 global hotkey `Ctrl+Shift+A` to summon or restore the analytics window from any desktop screen.
    - Integrated into both Widget Right-Click Context Menu and Windows System Tray Menu.
  - **Unit Test Coverage**:
    - Added [`Tests/RollingHistoryServiceTests.cs`](file:///c:/GitHub/InternetMeter/Tests/RollingHistoryServiceTests.cs) covering circular buffer bounds, timeline duration slicing, peak calculations, RFC 4180 CSV generation, and nice Y-max calculations (suite expanded to **90 passing tests**).

---

## [1.6.0] — 2026-10-29

### Added
- **Cyberpunk & Windows 11 Acrylic Theme Customizer**:
  - **4 Curated Designer Palettes**:
    - **Emerald & Sky (Default)**: `#10B981` download $\downarrow$ with `#38BDF8` upload $\uparrow$ on `#0D0F18` OLED Dark Slate.
    - **Cyberpunk Neon**: High-energy electric `#F43F5E` (Hot Magenta $\downarrow$) and `#06B6D4` (Electric Cyan $\uparrow$) with `#A855F7` accents on `#0B0C16` Midnight Void.
    - **Monochrome Stealth**: High-contrast pure `#F8FAFC` (Crisp White $\downarrow$) and `#94A3B8` (Muted Slate Silver $\uparrow$) with `#64748B` dim text on `#090A0F` Pitch Carbon.
    - **Solar Amber**: Warm `#F59E0B` (Amber $\downarrow$) and `#EAB308` (Golden Sun $\uparrow$) optimized for night workflows and blue-light reduction on `#14100D` Smoked Obsidian.
  - **Vector Layout Scaling Engine**:
    - Crisp, native-DPI scaling across 3 ergonomic presets: `85% (Compact)`, `100% (Standard)`, `125% (Large)`.
    - Implemented via WPF `LayoutTransform` (`ScaleTransform`) on root `WidgetCard`, recalculating window boundaries and edge snapping in real time.
    - Clamped strictly between $0.70\times$ and $1.50\times$ in [`MainViewModel`](file:///c:/GitHub/InternetMeter/ViewModels/MainViewModel.cs).
  - **Windows 11 Hardware-Accelerated Frosted Glass (Acrylic)**:
    - Direct Win32 DWM integration using `DwmSetWindowAttribute` with `DWMWA_SYSTEMBACKDROP_TYPE = 38` and `DWMSBT_TRANSIENTWINDOW = 3` (Acrylic blur).
    - Blurs underlying desktop wallpaper and active windows through the floating HUD.
    - High-contrast optical base tint (`0.78`–`0.90` alpha) prevents text wash-out over bright backgrounds, guaranteeing WCAG AAA contrast ($\ge 11:1$).
    - Gracefully disables with zero artifacts on Windows 10 or unsupported builds.
  - **Settings Persistence & Dual Menus**:
    - `Theme`, `UiScale`, and `EnableAcrylicBlur` seamlessly persist to `%APPDATA%\InternetMeter\settings.json`.
    - Configurable on the fly from both the Widget Right-Click Context Menu and Windows System Tray Menu with live checkmarks.
  - **Dynamic Theme Binding**:
    - Speed labels, sparklines, ribbon indicators, and live process inspector drawer items automatically update their brushes to match the selected theme.
  - **Unit Test Coverage**:
    - Added comprehensive unit tests in [`ThemeCustomizerTests.cs`](file:///c:/GitHub/InternetMeter/Tests/ThemeCustomizerTests.cs) verifying palette token generation, frozen brush performance, JSON serialization, and scale clamping (suite expanded to **83 passing tests**).

---

## [1.5.2] — 2026-10-29

### Fixed
- **Network Adapter Context Menu & Submenu Flyout**:
  - Resolved issue where clicking or hovering over `Network Adapter` in the widget context menu did nothing and showed no flyout chevron arrow.
  - **Root Cause**: In WPF, any `MenuItem` with `Items.Count == 0` is treated as a leaf action item rather than a submenu header. As a consequence, WPF omitted the flyout chevron and never fired the `SubmenuOpened` event on click or hover.
  - **Fix**:
    - Pre-populated the `AdaptersMenu` in XAML with a placeholder item so WPF recognizes it as a submenu header on initial render.
    - Added [`ContextMenu_Opened`](file:///c:/GitHub/InternetMeter/MainWindow.xaml.cs) and [`MainWindow_Loaded`](file:///c:/GitHub/InternetMeter/MainWindow.xaml.cs) triggers to populate all available adapters dynamically (`All Sources`, `Wi-Fi`, `Ethernet`) with live checkmarks.
    - Added a matching `Network Adapter` submenu to the Windows System Tray menu with dynamic selection.

---

## [1.5.1] — 2026-10-29

### Fixed
- **Process Network Accounting & Disk Cache Write Inflation**:
  - Resolved root cause of inflated upload statistics in the **Application Data Usage Report** where web browsers (e.g., `brave.exe`, `chrome.exe`) and local applications reported tens or hundreds of megabytes of upload despite zero outgoing network traffic.
  - **Root Cause**: Windows kernel API `GetProcessIoCounters` combines both File (Disk) I/O and Network Socket I/O inside `IO_COUNTERS.WriteTransferCount` and `ReadTransferCount`. When modern web browsers download web pages or media, they write large cache files (`%LOCALAPPDATA%\...\Cache\Cache_Data\`), IndexedDB, LevelDB, and SQLite databases to the SSD. Previously, every byte written to disk cache was falsely categorized as network upload.
  - **Physical NIC Calibration & Bandwidth Conservation**:
    - Reconciled process I/O deltas against the physical network interface ([`NetworkService`](file:///c:/GitHub/InternetMeter/Services/NetworkService.cs#L60-L145)) ground-truth telemetry.
    - When physical network upload delta is zero or minimal (TCP ACKs/keepalives), process SSD cache writes are filtered out completely and not recorded as network upload.
    - When physical network upload is active, process uploads are bounded and proportionally scaled so the sum of process traffic never exceeds physical NIC throughput.
    - Applied identical calibration to process downloads to prevent local disk reads (file copies, cached textures) from misreporting as internet download.
  - **Socket-Level Remote Endpoint Filtering**:
    - Enhanced [`NativeMethods.GetActiveNetworkPids()`](file:///c:/GitHub/InternetMeter/Common/NativeMethods.cs#L95-L168) to ignore `LISTEN` (2) and `CLOSED` (1) sockets, as well as local loopback IPC endpoints (`127.0.0.0/8` and IPv6 `::1`), ensuring background local RPC daemons are not tracked as internet processes.
  - **Continuous Background Process Tracking**:
    - Replaced lazy on-drawer-expand polling with timer-driven background updates on every sampling tick, ensuring the cumulative session, daily, and monthly usage report logs all applications continuously even when the UI drawer is collapsed.
  - **Unit Test Suite Expansion**:
    - Added verification tests in [`ProcessNetworkServiceTests.cs`](file:///c:/GitHub/InternetMeter/Tests/ProcessNetworkServiceTests.cs) (suite expanded to **75 passing tests**).

---

## [1.5.0] — 2026-10-29

### Added
- **Ultra-Compact Taskbar Docking Mode (Micro Ribbon)**:
  - Single-row micro ribbon profile ($168 \times 26\text{ px}$) displaying vital real-time metrics:
    - Emerald download speed (`↓ 48.2 MB/s`)
    - Sky blue upload speed (`↑ 6.4 MB/s`)
    - Dynamic color-coded latency badge (`18ms`)
  - Double-click gesture on widget background face toggles between Full HUD Mode ($274\text{ px}$) and Compact Ribbon Mode ($168\text{ px}$).
  - Hardware-accelerated WPF `DoubleAnimation` with `CubicEaseOut` for a 180ms smooth morphing transition.
  - Upward expansion compensation: automatically anchors and expands upwards when docked near the bottom taskbar boundary to prevent clipping off-screen.
- **Magnetic Edge & Taskbar Snapping**:
  - $16\text{ px}$ proximity magnetic snap targeting screen work areas.
  - Snaps directly onto the top border of the Windows Taskbar when dragged within threshold.
  - Snaps cleanly to desktop top, left, and right margins.
- **Global Hotkey & Menus**:
  - Global hotkey `Ctrl + Shift + D` registered system-wide to toggle Compact Mode instantly.
  - Added checkable `Compact Ribbon Mode` menu item in Widget Context Menu and System Tray Menu.
- **Settings Persistence**:
  - Remembers `IsCompactMode` and edge snapping preferences in `AppSettings`.
- **Unit Test Suite Expansion**:
  - Added [`CompactModeTests.cs`](file:///c:/GitHub/InternetMeter/Tests/CompactModeTests.cs) covering mode toggles, settings serialization, and corner/taskbar snapping boundary calculations (suite expanded to **74 passing tests**).

### Fixed
- **Taskbar Containment & Z-Order Priority**:
  - Resolved issue where clicking the Windows Taskbar caused the bottom of the widget to be swallowed underneath the taskbar.
  - Added strict working area containment clamping ([`EnsureWithinWorkingArea()`](file:///c:/GitHub/InternetMeter/MainWindow.xaml.cs#L221-L265)) on window load, drag release, and size changes so the widget never encroaches into the taskbar coordinate space.
  - Implemented multi-monitor and DPI-aware screen work area resolution ([`GetCurrentWorkArea()`](file:///c:/GitHub/InternetMeter/MainWindow.xaml.cs#L195-L220)).
  - Implemented Win32 [`SetWindowPos(HWND_TOPMOST)`](file:///c:/GitHub/InternetMeter/MainWindow.xaml.cs#L295-L310) re-assertion on window deactivation and size changes to maintain foremost Z-order priority.

---

## [1.4.0] — 2026-10-29

### Added
- **Cumulative Process Traffic Reporting System & Dedicated Modal Window**:
  - Standalone [`UsageReportWindow.xaml`](file:///c:/GitHub/InternetMeter/Views/UsageReportWindow.xaml) modal window styled in Dark Glass (`#0D0F18` / `#161926` / `#1E2337`).
  - Multi-timeframe reporting tabs: `Current Session`, `Today (24h)`, and `This Month`.
  - Search filter input box with instant incremental query filtering and clear button (`✕`).
  - Overview telemetry cards: Total Tracked Data, Download & Upload breakdown, Active & Closed application counts.
  - Interactive DataGrid showing Application icon, process name, `ACTIVE` / `CLOSED` status badges, total download/upload volume in monospace Consolas, total combined data, and proportional traffic share progress bars.
  - Double-click any row to reveal and highlight the executable directly in Windows File Explorer (`explorer.exe /select,...`).
  - CSV export button generating structured reports with full timestamps, raw bytes, formatted sizes, status, and file paths.
  - In-memory session reset button with confirmation prompt preserving rolling daily history.
  - Global hotkey `Ctrl + Shift + R` and context/tray menu entries to summon report window instantly.
- **Dual-Mode Inline Process Inspector Drawer**:
  - Segmented toggle inside floating widget flyout drawer: `[Live]` ⟷ `[Total]`.
  - `Live` mode displays real-time instantaneous speeds (`↓ 12.4 MB/s`, `↑ 1.2 MB/s`).
  - `Total` mode displays accumulated session data volume and percentage of total traffic (`142 MB (38%)`).
  - Direct shortcut button `↗ Report` inside drawer header and footer link.
- **Multi-PID Process Aggregation & Short-Lived Process Survivability**:
  - [`TrafficJournalService.cs`](file:///c:/GitHub/InternetMeter/Services/TrafficJournalService.cs) consolidates multiple child worker PIDs (e.g. Chrome sandboxes, Discord workers) under their parent executable name.
  - Transient processes (e.g. `git.exe`, `curl.exe`, short-lived updaters) retain their cumulative session data even after their PID terminates and are marked with a `CLOSED` badge.
  - Persistent 30-day rolling daily JSON storage at `%APPDATA%\InternetMeter\usage_history.json` with auto-pruning.
- **Immediate Opacity Application Guarantee**:
  - Re-anchored WPF `SetCurrentValue(Window.OpacityProperty, ...)` upon menu item selection and hotkey toggling, providing zero-latency rendering of opacity percentage changes.
- **Unit Test Suite Expansion**:
  - Added [`TrafficJournalServiceTests.cs`](file:///c:/GitHub/InternetMeter/Tests/TrafficJournalServiceTests.cs) and expanded [`ProcessNetworkServiceTests.cs`](file:///c:/GitHub/InternetMeter/Tests/ProcessNetworkServiceTests.cs) (suite expanded to **68 passing tests**).

---

## [1.3.1] — 2026-10-29

### Fixed
- **Instantaneous Opacity Application**:
  - Resolved WPF DependencyProperty local value precedence defect where code-behind local assignment (`Opacity = ...`) clobbered the XAML `Window.Opacity` data-binding, which caused percentage changes (100%, 80%, 60%, 40%) to stall until `Ctrl + Shift + G` was pressed.
  - Introduced unified `CurrentOpacity` reactive property on [`MainViewModel.cs`](file:///c:/GitHub/InternetMeter/ViewModels/MainViewModel.cs) calculating effective opacity dynamically between `WindowOpacity` and `GhostOpacity`.
  - Bound `MainWindow.xaml` directly to `CurrentOpacity` and completely removed code-behind local opacity mutations from `ApplyGhostMode`.
  - Added auto-disengagement of Ghost Mode whenever an explicit opacity level is selected from context or tray menus.
- **Opacity Checkmark Synchronization**:
  - Implemented `OpacityToBooleanConverter` with tolerance checks in [`Converters.cs`](file:///c:/GitHub/InternetMeter/Converters/Converters.cs) to display active checkmarks on the selected opacity percentage across both the Context Menu and System Tray Menu.
  - Added dedicated `Opacity` flyout submenu with live checkmarks in the Taskbar System Tray context menu.
- **Unit Tests**:
  - Added unit test coverage for `CurrentOpacity`, `OpacityToBooleanConverter`, and `InverseBooleanToVisibilityConverter` (suite expanded to **58 passing tests**).

---

## [1.3.0] — 2026-10-29

### Added
- **Per-Process Bandwidth Breakdown (Process Inspector)**:
  - Implemented `ProcessNetworkService.cs` providing real-time visibility into application-level bandwidth consumers.
  - Queries active TCP and UDP socket tables via `iphlpapi.dll` (`GetExtendedTcpTable` & `GetExtendedUdpTable`) to map active network connections to Process IDs without administrative privileges.
  - Computes differential per-process I/O transfer rates ($\Delta\text{bytes} / \Delta t$) via `kernel32.dll` `GetProcessIoCounters`.
  - Filters and ranks top network-consuming applications (e.g. `chrome.exe`, `steam.exe`, `discord.exe`).
- **Collapsible Process Inspector Flyout Drawer**:
  - Integrated a sleek expandable dark drawer (`#161824`) at the bottom of the widget card.
  - Added interactive footer toggle pill: `[▾ Apps]` / `[▴ Apps]`.
  - Displays high-quality 14x14 application icons, executable names, PIDs, download rates in emerald (`↓ 12.4 MB/s`), and upload rates in sky blue (`↑ 1.2 MB/s`).
  - Added subtle empty-state indicator when no application traffic is transmitting.
- **Global Hotkey & Menu Controls**:
  - System-wide keyboard shortcut `Ctrl + Shift + P` registered in `MainWindow.xaml.cs` to quickly toggle the inspector drawer from anywhere.
  - Added checkable `Process Inspector (Top Apps)` items in Widget Context Menu and Taskbar System Tray Menu.
- **Process Icon & Name Caching**:
  - Implemented 30-second TTL in-memory cache for resolved process names and extracted system icons to prevent repeated handle allocations.
- **Resource Governance**:
  - Polling is active only when the inspector drawer is expanded; when closed, process table interrogation is completely bypassed for zero extra CPU overhead.
- **Automated Tests**:
  - Added `Tests/ProcessNetworkServiceTests.cs` covering top $N$ truncation, settings serialization, glyph toggling, and speed formatting (suite expanded to **49 passing tests**).

---

## [1.2.0] — 2026-10-29

### Added
- **Ghost / Click-Through Mode (`WS_EX_TRANSPARENT`)**:
  - Implemented 64-bit safe Win32 extended window style toggle using `GetWindowLongPtr` and `SetWindowLongPtr` in `Common/NativeMethods.cs`.
  - Enables transparent click-through: mouse clicks, double-clicks, scroll events, and drags pass seamlessly through the floating HUD to underlying applications, IDEs, browsers, or games.
- **Global Hotkey Shroud Listener (`Ctrl + Shift + G`)**:
  - System-wide keyboard shortcut registered via `RegisterHotKey` in `MainWindow.xaml.cs`.
  - Instantly toggles into and out of Ghost Mode from any application without needing to focus the widget.
  - Intercepted cleanly in `WndProc` via `WM_HOTKEY` and unregistered on window closure.
- **Anti-Lockout System Tray Safeguard**:
  - Because click-through widgets cannot receive mouse clicks, a dedicated `Click-Through (Ghost Mode)\tCtrl+Shift+G` item was added to the Taskbar System Tray context menu.
  - Allows restoring full interactivity via system tray right-click or tray icon double-click.
- **Visual Ghost Shroud & Balloon Notifications**:
  - Automatically transitions widget opacity to `0.65` (`GhostOpacity`) when Ghost Mode engages to visually signify non-interactive status.
  - Restores full configured opacity (`WindowOpacity`) when disengaged.
  - Dispatches informative Windows system tray balloon tip reminding users how to exit Ghost Mode.
- **State Persistence**:
  - Persisted `IsGhostMode`, `GhostOpacity`, and `GhostHotkeyText` to `%APPDATA%\InternetMeter\settings.json`.
- **Automated Tests**:
  - Added `Tests/GhostModeTests.cs` verifying extended style bitmask manipulations, settings serialization, and ViewModel event dispatching (suite expanded to **45 passing tests**).

---

## [1.1.2] — 2026-10-29

### Added
- **Windows Auto-Start on Boot**:
  - Implemented `AutoStartService.cs` managing non-administrative registry registration in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
  - Added quotes around executable paths to guarantee safe execution on paths containing whitespace.
  - Automatic path-drift healing: synchronizes the registry key to the active executable path if `InternetMeter.exe` is relocated.
- **Silent Boot Routing (`--autostart`)**:
  - `App.xaml.cs` inspects startup arguments for `--autostart` or `--minimized`.
  - When booting with Windows and `StartMinimizedOnBoot` is true, the widget initializes directly to the system tray without rendering on screen.
- **UI & Menu Toggles**:
  - Added checkable `Start with Windows` toggle in Widget Context Menu.
  - Added checkable `Start with Windows` toggle in Taskbar System Tray Menu.
  - Persisted user preference to `%APPDATA%\InternetMeter\settings.json`.
- **Automated Tests**:
  - Added `Tests/AutoStartServiceTests.cs` validating argument formatting and real registry CRUD against isolated sandbox subkeys (suite expanded to **42 passing tests**).

---

## [1.1.1] — 2026-10-29

### Added
- **Single-Instance Enforcement**:
  - Implemented system-wide named Mutex (`Global\InternetMeter_SingleInstance_Mutex_9D4F28A1` with fallback to `Local\`) in `App.xaml.cs`.
  - Blocks opening or running duplicate instances of `InternetMeter.exe`.
- **Inter-Process Activation & Wake-up**:
  - Implemented Win32 IPC messaging via `NativeMethods.SignalExistingInstance()` and `RegisterWindowMessage`.
  - When a second instance is launched, it automatically signals the running widget to unhide (if minimized to system tray), restore from minimized state, and bring itself to the foreground before the secondary process cleanly exits via `Environment.Exit(0)`.
  - Configured `ChangeWindowMessageFilter` in `MainWindow.xaml.cs` to ensure broadcast messages cross Windows UIPI privilege boundaries.
- **Unit Tests**:
  - Added `Tests/SingleInstanceTests.cs` asserting mutex acquisition, conflict detection, and cleanup across 39 passing tests.

---

## [1.1.0] — 2026-10-29

### Added
- **Live Ping & Latency Heartbeat Engine**:
  - Implemented asynchronous non-blocking RTT probe worker (`Services/PingService.cs`) using `System.Net.NetworkInformation.Ping`.
  - Probes high-speed public DNS (`1.1.1.1`) every 3 seconds with fallback handling for local default gateway.
  - Graceful absorption of `PingException`, DNS drops, and timeout states without UI contention.
- **Dynamic Visual Latency Health Chip**:
  - Embedded high-contrast latency badge directly in the widget header bar beside the network adapter button.
  - Tabular typography (`Segoe UI Variable Display, Consolas`) prevents layout jitter.
  - Dynamic color feedback:
    - **Emerald (`#10B981`)**: $< 40$ ms (Optimal / Gaming / Low Jitter)
    - **Amber (`#F59E0B`)**: $40 - 120$ ms (Moderate)
    - **Rose (`#F43F5E`)**: $> 120$ ms or Timeout/Loss/Unreachable (Degraded / Drop)
    - **Slate (`#64748B`)**: Standby / Offline / Disabled
- **Full Toggle on/off Support**:
  - Context menu toggle: `Show Ping Latency` (checkable).
  - Taskbar system tray menu toggle: `Show Ping Latency` (checkable).
  - Toggling off immediately cancels background ICMP ping tasks, eliminating background packet emission and CPU usage.
  - Setting persisted to `%APPDATA%\InternetMeter\settings.json`.
- **Test Suite Expansion**:
  - Added unit test suite in `Tests/PingServiceTests.cs` verifying latency tiers, timeout/unreachable states, and service start/stop state machine.
  - Expanded automated test coverage to **37 tests passing**.

---

## [1.0.2] — 2026-10-29

### Changed
- **Unit Hierarchy Alignment**: Standardized speed unit sorting across all UI menus, models, and cycling loops to:
  1. `MBps` (Megabyte per second)
  2. `Mbps` (Megabit per second)
  3. `KBps` (Kilobyte per second)
  4. `Kbps` (Kilobit per second)
- **Context Menu Ordering**: Re-sequenced submenu items in `MainWindow.xaml` to strictly match the 1–4 rank.
- **Model Re-ordering**: Updated `SpeedUnit` enum in `Models/SpeedUnit.cs` to reflect the new primary rank.
- **State Machine Cycle**: `MainViewModel.CycleUnit()` updated to advance strictly: `MBps` → `Mbps` → `KBps` → `Kbps` → `MBps`.
- **System Tray Ordering**: Taskbar tray context menu dynamically enumerates units in the updated sorted order.
- **Interactive Prototype Sync**: Updated `widget_preview.html` control select and simulation array to mirror the new hierarchy.

---

## [1.0.1] — 2026-10-29

### Fixed
- **Submenu Contrast & Legibility**: Solved washed-out white text bug where Windows Aero2 default style forced a light-gray system popup container (`#F0F0F0`) while child text inherited `#F8FAFC`, producing near 1:1 unreadable contrast.
- **Dark HUD Submenu Template**: Forged explicit dark `ControlTemplate` for `MenuItem.SubmenuHeaderTemplateKey` with `#161822` surface and `#334155` border, yielding **14.8:1 WCAG AAA** contrast ratio.
- **Byte vs. bit Unit Collisions**: Fixed defect where selecting `Mbps` inadvertently checked both `MBps` and `Mbps` simultaneously. Root cause was `StringComparison.OrdinalIgnoreCase` treating uppercase `'B'` (Byte = 8 bits) and lowercase `'b'` (bit) as equal. Switched to strict `StringComparison.Ordinal` in `EqualityToBooleanConverter` and tray menu handlers.
- **Aesthetic Checkmark Indicators**: Replaced default Windows light-blue check boxes with dark-slate containers (`#1E2436`) and vibrant emerald checkmarks (`✓` in `#10B981`).
- **Dark System Tray Menu**: Implemented `DarkColorTable` and `ToolStripProfessionalRenderer` for Windows Forms tray menu, preventing default gray OS chrome.

### Added
- **Unit Equality Regression Tests**: Added `ConverterTests.cs` covering 8 distinct case-sensitive permutations between `MBps`, `Mbps`, `KBps`, and `Kbps` (suite total expanded to 24 passing tests).

---

## [1.0.0] — 2026-10-29

### Added
- **Core Engine & Architecture**:
  - Real-time network traffic monitor built with C# / WPF on .NET 8.
  - Native differential traffic sampling engine (`NetworkService.cs`) tracking IPv4 and IPv6 counters via `System.Net.NetworkInformation`.
  - Non-blocking delta time calculation using high-resolution `Stopwatch`.
- **Floating HUD Widget**:
  - Frameless, borderless glass window (`WindowStyle="None"`, `AllowsTransparency="True"`).
  - Always-on-Top toggle (`📌` button + ContextMenu + Tray).
  - Tabular monospace typography (`Segoe UI Variable Display, Consolas`) preventing layout jitter.
  - Directional traffic coloring: Emerald (`#10B981`) for download (Rx) and Sky (`#38BDF8`) for upload (Tx).
  - Rolling 24-sample live canvas sparkline for throughput history.
  - Left-click drag anywhere on widget surface with automatic coordinate saving.
- **Network Interface Selection**:
  - Dynamic discovery and hot-plug handling of physical and virtual network adapters.
  - Global aggregation mode ("All Internet Sources") combining all active non-loopback adapters.
- **Multi-Unit Conversion Engine**:
  - Mathematical converter and formatter (`UnitFormatter.cs`) supporting `KBps`, `MBps`, `Kbps`, and `Mbps`.
  - Live session throughput accumulator (Total Downloaded / Total Uploaded).
- **Settings & Persistence**:
  - Configuration engine (`SettingsService.cs`) persisting state to `%APPDATA%\InternetMeter\settings.json`.
  - Remembers window position, opacity, selected adapter, topmost flag, and unit preferences.
- **System Tray Integration**:
  - Taskbar notification tray icon with custom drawn dynamic network arrow icon.
  - Minimize-to-tray on close with balloon tooltips.
  - Complete right-click management menu.
- **Testing & Packaging**:
  - Comprehensive unit test suite covering `UnitFormatter`, `NetworkService`, and `SettingsService`.
  - Single-file self-contained deployment configuration producing lightweight 192 KB executable (`InternetMeter.exe`).

