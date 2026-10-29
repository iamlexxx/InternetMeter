using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.ViewModels;
using Forms = System.Windows.Forms;

namespace InternetMeter;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private Forms.NotifyIcon? _notifyIcon;
    private Views.UsageReportWindow? _reportWindow;
    private Views.BandwidthAnalyticsWindow? _analyticsWindow;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();

        _viewModel.RequestResetPosition += ResetToTopRight;
        _viewModel.RequestOpenUsageReport += OpenUsageReport;
        _viewModel.RequestOpenAnalytics += OpenAnalyticsWindow;
        _viewModel.RequestOpenDataCapConfig += OpenDataCapConfig;
        _viewModel.QuotaToastRequested += OnQuotaToastRequested;
        _viewModel.GhostModeChanged += OnGhostModeChanged;
        _viewModel.CompactModeToggled += OnCompactModeToggled;
        _viewModel.UiScaleChanged += OnUiScaleChanged;
        _viewModel.AcrylicBlurChanged += OnAcrylicBlurChanged;

        Loaded += MainWindow_Loaded;
        SizeChanged += MainWindow_SizeChanged;
        LocationChanged += MainWindow_LocationChanged;
        Deactivated += MainWindow_Deactivated;
        Closed += MainWindow_Closed;

        SetupSystemTray();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = PresentationSource.FromVisual(this) as HwndSource;
        source?.AddHook(WndProc);

        if (NativeMethods.WM_SHOW_INTERNET_METER != 0)
        {
            NativeMethods.ChangeWindowMessageFilter(NativeMethods.WM_SHOW_INTERNET_METER, NativeMethods.MSGFLT_ADD);
        }

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            NativeMethods.RegisterHotKey(
                hwnd,
                NativeMethods.GHOST_HOTKEY_ID,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
                NativeMethods.VK_G);

            NativeMethods.RegisterHotKey(
                hwnd,
                NativeMethods.INSPECTOR_HOTKEY_ID,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
                NativeMethods.VK_P);

            NativeMethods.RegisterHotKey(
                hwnd,
                NativeMethods.REPORT_HOTKEY_ID,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
                NativeMethods.VK_R);

            NativeMethods.RegisterHotKey(
                hwnd,
                NativeMethods.DOCK_HOTKEY_ID,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
                NativeMethods.VK_D);

            NativeMethods.RegisterHotKey(
                hwnd,
                NativeMethods.ANALYTICS_HOTKEY_ID,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
                NativeMethods.VK_A);

            if (_viewModel.IsGhostMode)
            {
                ApplyGhostMode(true, showNotification: false);
            }

            if (_viewModel.EnableAcrylicBlur)
            {
                ApplyAcrylicBackdrop(true);
            }
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            if (hotkeyId == NativeMethods.GHOST_HOTKEY_ID)
            {
                _viewModel.IsGhostMode = !_viewModel.IsGhostMode;
                handled = true;
                return IntPtr.Zero;
            }
            if (hotkeyId == NativeMethods.INSPECTOR_HOTKEY_ID)
            {
                _viewModel.IsProcessInspectorExpanded = !_viewModel.IsProcessInspectorExpanded;
                handled = true;
                return IntPtr.Zero;
            }
            if (hotkeyId == NativeMethods.REPORT_HOTKEY_ID)
            {
                OpenUsageReport();
                handled = true;
                return IntPtr.Zero;
            }
            if (hotkeyId == NativeMethods.DOCK_HOTKEY_ID)
            {
                _viewModel.ToggleCompactMode();
                handled = true;
                return IntPtr.Zero;
            }
            if (hotkeyId == NativeMethods.ANALYTICS_HOTKEY_ID)
            {
                OpenAnalyticsWindow();
                handled = true;
                return IntPtr.Zero;
            }
        }

        if (msg == NativeMethods.WM_SHOW_INTERNET_METER)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (Visibility != Visibility.Visible)
                {
                    Visibility = Visibility.Visible;
                }
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                Activate();
                Topmost = _viewModel.AlwaysOnTop;
            });
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var (left, top) = _viewModel.GetSavedWindowCoordinates();
        if (left.HasValue && top.HasValue && IsOnScreen(left.Value, top.Value))
        {
            Left = left.Value;
            Top = top.Value;
        }
        else
        {
            ResetToTopRight();
        }

        ApplyCompactMode(_viewModel.IsCompactMode, animate: false);

        PopulateAdaptersMenu();
        EnsureWithinWorkingArea();
        ReassertTopmost();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        EnsureWithinWorkingArea();
        ReassertTopmost();
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        ReassertTopmost();
    }

    private void MainWindow_LocationChanged(object? sender, EventArgs e)
    {
        if (IsLoaded && WindowState == WindowState.Normal)
        {
            if (_viewModel.IsCompactMode)
            {
                _viewModel.SaveCompactCoordinates(Left, Top);
            }
            else
            {
                _viewModel.SaveWindowCoordinates(Left, Top);
            }
        }
    }

    private bool IsOnScreen(double left, double top)
    {
        return left >= SystemParameters.VirtualScreenLeft &&
               left <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100 &&
               top >= SystemParameters.VirtualScreenTop &&
               top <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60;
    }

    private void ResetToTopRight()
    {
        var workArea = GetCurrentWorkArea();
        Left = workArea.Right - Width - 24;
        Top = workArea.Top + 24;
        _viewModel.SaveWindowCoordinates(Left, Top);
    }

    private void WidgetCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _viewModel.ToggleCompactMode();
            e.Handled = true;
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
            ApplyEdgeSnapping();
            EnsureWithinWorkingArea();
            ReassertTopmost();
            _viewModel.SaveWindowCoordinates(Left, Top);
        }
    }

    private Rect GetCurrentWorkArea()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            try
            {
                var screen = Forms.Screen.FromHandle(hwnd);
                var wa = screen.WorkingArea;
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    double dpiX = source.CompositionTarget.TransformToDevice.M11;
                    double dpiY = source.CompositionTarget.TransformToDevice.M22;
                    return new Rect(wa.Left / dpiX, wa.Top / dpiY, wa.Width / dpiX, wa.Height / dpiY);
                }
                return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
            }
            catch
            {
                // Fallback to system primary work area
            }
        }
        return SystemParameters.WorkArea;
    }

    private void EnsureWithinWorkingArea()
    {
        if (!IsLoaded || WindowState != WindowState.Normal) return;

        var workArea = GetCurrentWorkArea();
        double w = ActualWidth > 0 ? ActualWidth : Width;
        double h = ActualHeight > 0 ? ActualHeight : Height;

        if (w <= 0 || h <= 0) return;

        double currentLeft = Left;
        double currentTop = Top;

        // Never allow widget to sink below the bottom taskbar boundary
        if (currentTop + h > workArea.Bottom)
        {
            currentTop = workArea.Bottom - h;
        }

        // Never allow widget to clip above top screen edge
        if (currentTop < workArea.Top)
        {
            currentTop = workArea.Top;
        }

        // Never allow widget to extend past right screen edge
        if (currentLeft + w > workArea.Right)
        {
            currentLeft = workArea.Right - w;
        }

        // Never allow widget to go past left screen edge
        if (currentLeft < workArea.Left)
        {
            currentLeft = workArea.Left;
        }

        if (Math.Abs(Left - currentLeft) > 0.5 || Math.Abs(Top - currentTop) > 0.5)
        {
            Left = currentLeft;
            Top = currentTop;
            _viewModel.SaveWindowCoordinates(Left, Top);
        }
    }

    private void ApplyEdgeSnapping()
    {
        var workArea = GetCurrentWorkArea();
        const double snapDistance = 24.0;
        double w = ActualWidth > 0 ? ActualWidth : Width;
        double h = ActualHeight > 0 ? ActualHeight : Height;

        double newLeft = Left;
        double newTop = Top;

        // Snap Left edge
        if (Left < workArea.Left + 8 || Math.Abs(Left - (workArea.Left + 8)) <= snapDistance)
            newLeft = workArea.Left + 8;
        // Snap Right edge
        else if ((Left + w) > workArea.Right - 8 || Math.Abs((Left + w) - (workArea.Right - 8)) <= snapDistance)
            newLeft = workArea.Right - w - 8;

        // Snap Top edge
        if (Top < workArea.Top + 8 || Math.Abs(Top - (workArea.Top + 8)) <= snapDistance)
            newTop = workArea.Top + 8;
        // Snap Bottom edge (Sits cleanly on top of the Taskbar boundary)
        else if ((Top + h) > workArea.Bottom || Math.Abs((Top + h) - workArea.Bottom) <= snapDistance)
            newTop = workArea.Bottom - h;

        Left = newLeft;
        Top = newTop;
    }

    private void ReassertTopmost()
    {
        if (!_viewModel.AlwaysOnTop) return;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void AdapterButton_Click(object sender, RoutedEventArgs e)
    {
        PopulateAdaptersMenu();
        if (WidgetCard.ContextMenu != null)
        {
            WidgetCard.ContextMenu.IsOpen = true;
            Dispatcher.InvokeAsync(() => AdaptersMenu.IsSubmenuOpen = true, System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void PingBadge_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (PingBadgeBorder.ToolTip is System.Windows.Controls.ToolTip tt)
        {
            tt.PlacementTarget = PingBadgeBorder;
            tt.IsOpen = !tt.IsOpen;
        }
    }

    private void CompactPingBadge_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (CompactPingBadgeBorder.ToolTip is System.Windows.Controls.ToolTip tt)
        {
            tt.PlacementTarget = CompactPingBadgeBorder;
            tt.IsOpen = !tt.IsOpen;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Hidden;
        _notifyIcon?.ShowBalloonTip(1500, "InternetMeter", "Widget minimized to system tray. Double-click tray icon to restore.", Forms.ToolTipIcon.Info);
    }

    private void SetupSystemTray()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "InternetMeter — Windows Traffic Monitor",
            Visible = true,
            Icon = GenerateTrayIcon()
        };

        _notifyIcon.DoubleClick += (s, e) =>
        {
            if (Visibility == Visibility.Visible)
            {
                Visibility = Visibility.Hidden;
            }
            else
            {
                Visibility = Visibility.Visible;
                Activate();
            }
        };

        var menuRenderer = new ModernDarkMenuRenderer();
        var menuFont = new Font("Segoe UI", 9.25f, FontStyle.Regular);

        var trayMenu = new Forms.ContextMenuStrip
        {
            Renderer = menuRenderer,
            ShowImageMargin = true,
            ShowCheckMargin = false,
            Font = menuFont,
            BackColor = Color.FromArgb(22, 24, 34),
            ForeColor = Color.FromArgb(248, 250, 252)
        };

        var toggleShowItem = new Forms.ToolStripMenuItem("Show / Hide Widget", null, (s, e) =>
        {
            Visibility = Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            if (Visibility == Visibility.Visible) Activate();
        })
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        trayMenu.Items.Add(toggleShowItem);

        trayMenu.Items.Add(new Forms.ToolStripSeparator());

        var alwaysOnTopItem = new Forms.ToolStripMenuItem("Always on Top")
        {
            Checked = _viewModel.AlwaysOnTop,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        alwaysOnTopItem.Click += (s, e) => _viewModel.AlwaysOnTop = alwaysOnTopItem.Checked;
        trayMenu.Items.Add(alwaysOnTopItem);

        var unitsSubmenu = new Forms.ToolStripMenuItem("Speed Unit")
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        ApplyDarkMenuTheme(unitsSubmenu, menuRenderer, menuFont);

        (string Label, SpeedUnit Unit)[] unitOptions = [
            ("MBps (MB/s)", SpeedUnit.MBps),
            ("Mbps (Megabits)", SpeedUnit.Mbps),
            ("KBps (KB/s)", SpeedUnit.KBps),
            ("Kbps (Kilobits)", SpeedUnit.Kbps)
        ];
        foreach (var (lbl, u) in unitOptions)
        {
            var unitItem = new Forms.ToolStripMenuItem(lbl)
            {
                Checked = _viewModel.CurrentUnit == u,
                ForeColor = Color.FromArgb(248, 250, 252),
                Tag = u
            };
            unitItem.Click += (s, e) => _viewModel.SetUnit(u);
            unitsSubmenu.DropDownItems.Add(unitItem);
        }
        trayMenu.Items.Add(unitsSubmenu);

        var adaptersSubmenu = new Forms.ToolStripMenuItem("Network Adapter")
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        ApplyDarkMenuTheme(adaptersSubmenu, menuRenderer, menuFont);
        trayMenu.Items.Add(adaptersSubmenu);

        var themeSubmenu = new Forms.ToolStripMenuItem("Theme")
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        ApplyDarkMenuTheme(themeSubmenu, menuRenderer, menuFont);
        (string ThemeName, ThemePalette Palette)[] themeList = [
            ("Emerald & Sky (Default)", ThemePalette.EmeraldSky),
            ("Cyberpunk Neon", ThemePalette.CyberpunkNeon),
            ("Monochrome Stealth", ThemePalette.MonochromeStealth),
            ("Solar Amber", ThemePalette.SolarAmber)
        ];
        foreach (var (tName, pal) in themeList)
        {
            var palItem = new Forms.ToolStripMenuItem(tName)
            {
                Checked = _viewModel.CurrentTheme == pal,
                ForeColor = Color.FromArgb(248, 250, 252),
                Tag = pal
            };
            palItem.Click += (s, e) => _viewModel.SetTheme(pal);
            themeSubmenu.DropDownItems.Add(palItem);
        }
        trayMenu.Items.Add(themeSubmenu);

        var scaleSubmenu = new Forms.ToolStripMenuItem("Scale")
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        ApplyDarkMenuTheme(scaleSubmenu, menuRenderer, menuFont);

        (string ScaleName, double ScVal)[] scaleList = [
            ("85% (Compact)", 0.85),
            ("100% (Standard)", 1.0),
            ("125% (Large)", 1.25)
        ];
        foreach (var (scName, scVal) in scaleList)
        {
            var scItem = new Forms.ToolStripMenuItem(scName)
            {
                Checked = Math.Abs(_viewModel.UiScale - scVal) < 0.05,
                ForeColor = Color.FromArgb(248, 250, 252),
                Tag = scVal
            };
            scItem.Click += (s, e) => _viewModel.SetScale(scVal);
            scaleSubmenu.DropDownItems.Add(scItem);
        }
        trayMenu.Items.Add(scaleSubmenu);

        var acrylicItem = new Forms.ToolStripMenuItem("Windows 11 Frosted Glass")
        {
            Checked = _viewModel.EnableAcrylicBlur,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        acrylicItem.Click += (s, e) => _viewModel.EnableAcrylicBlur = acrylicItem.Checked;
        trayMenu.Items.Add(acrylicItem);

        var opacitySubmenu = new Forms.ToolStripMenuItem("Opacity")
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        ApplyDarkMenuTheme(opacitySubmenu, menuRenderer, menuFont);

        (string Label, double Val)[] opacityLevels = [("100%", 1.0), ("80%", 0.8), ("60%", 0.6), ("40%", 0.4)];
        foreach (var (lbl, val) in opacityLevels)
        {
            var opItem = new Forms.ToolStripMenuItem(lbl)
            {
                Checked = Math.Abs(_viewModel.WindowOpacity - val) < 0.05,
                ForeColor = Color.FromArgb(248, 250, 252)
            };
            double targetVal = val;
            opItem.Click += (s, e) => ApplyOpacity(targetVal);
            opacitySubmenu.DropDownItems.Add(opItem);
        }
        trayMenu.Items.Add(opacitySubmenu);

        var pingLatencyItem = new Forms.ToolStripMenuItem("Show Ping Latency")
        {
            Checked = _viewModel.ShowPingLatency,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        pingLatencyItem.Click += (s, e) => _viewModel.ShowPingLatency = pingLatencyItem.Checked;
        trayMenu.Items.Add(pingLatencyItem);

        var autoStartItem = new Forms.ToolStripMenuItem("Start with Windows")
        {
            Checked = _viewModel.StartWithWindows,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        autoStartItem.Click += (s, e) => _viewModel.StartWithWindows = autoStartItem.Checked;
        trayMenu.Items.Add(autoStartItem);

        var ghostModeItem = new Forms.ToolStripMenuItem("Click-Through (Ghost Mode)\tCtrl+Shift+G")
        {
            Checked = _viewModel.IsGhostMode,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        ghostModeItem.Click += (s, e) => _viewModel.IsGhostMode = ghostModeItem.Checked;
        trayMenu.Items.Add(ghostModeItem);

        var inspectorItem = new Forms.ToolStripMenuItem("Process Inspector (Top Apps)\tCtrl+Shift+P")
        {
            Checked = _viewModel.IsProcessInspectorExpanded,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        inspectorItem.Click += (s, e) => _viewModel.IsProcessInspectorExpanded = inspectorItem.Checked;
        trayMenu.Items.Add(inspectorItem);

        var reportItem = new Forms.ToolStripMenuItem("Traffic Usage Report...\tCtrl+Shift+R", null, (s, e) => OpenUsageReport())
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        trayMenu.Items.Add(reportItem);

        var analyticsItem = new Forms.ToolStripMenuItem("Detailed Bandwidth Analytics...\tCtrl+Shift+A", null, (s, e) => OpenAnalyticsWindow())
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        trayMenu.Items.Add(analyticsItem);

        var dataCapSubmenu = new Forms.ToolStripMenuItem("Monthly Data Cap")
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        ApplyDarkMenuTheme(dataCapSubmenu, menuRenderer, menuFont);

        var enableCapItem = new Forms.ToolStripMenuItem("Enable Data Cap")
        {
            Checked = _viewModel.IsDataCapEnabled,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        enableCapItem.Click += (s, e) => _viewModel.IsDataCapEnabled = enableCapItem.Checked;
        dataCapSubmenu.DropDownItems.Add(enableCapItem);

        var configCapItem = new Forms.ToolStripMenuItem("Configure Quota & Cycle...", null, (s, e) => OpenDataCapConfig())
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        dataCapSubmenu.DropDownItems.Add(configCapItem);
        dataCapSubmenu.DropDownItems.Add(new Forms.ToolStripSeparator());

        (string CapLabel, double CapGB)[] capPresets = [("50 GB", 50), ("100 GB", 100), ("250 GB", 250), ("500 GB", 500), ("1 TB", 1024)];
        foreach (var (lbl, gb) in capPresets)
        {
            var pItem = new Forms.ToolStripMenuItem(lbl, null, (s, e) => _viewModel.SetDataCapPreset(gb))
            {
                ForeColor = Color.FromArgb(248, 250, 252),
                Tag = gb
            };
            dataCapSubmenu.DropDownItems.Add(pItem);
        }
        trayMenu.Items.Add(dataCapSubmenu);

        var compactItem = new Forms.ToolStripMenuItem("Compact Ribbon Mode\tCtrl+Shift+D")
        {
            Checked = _viewModel.IsCompactMode,
            CheckOnClick = true,
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        compactItem.Click += (s, e) => _viewModel.IsCompactMode = compactItem.Checked;
        trayMenu.Items.Add(compactItem);

        var resetItem = new Forms.ToolStripMenuItem("Reset Position", null, (s, e) => ResetToTopRight())
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        trayMenu.Items.Add(resetItem);

        trayMenu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("Exit", null, (s, e) => System.Windows.Application.Current.Shutdown())
        {
            ForeColor = Color.FromArgb(248, 250, 252)
        };
        trayMenu.Items.Add(exitItem);

        trayMenu.Opening += (s, e) =>
        {
            alwaysOnTopItem.Checked = _viewModel.AlwaysOnTop;
            pingLatencyItem.Checked = _viewModel.ShowPingLatency;
            autoStartItem.Checked = _viewModel.StartWithWindows;
            ghostModeItem.Checked = _viewModel.IsGhostMode;
            inspectorItem.Checked = _viewModel.IsProcessInspectorExpanded;
            compactItem.Checked = _viewModel.IsCompactMode;
            acrylicItem.Checked = _viewModel.EnableAcrylicBlur;
            enableCapItem.Checked = _viewModel.IsDataCapEnabled;
            foreach (Forms.ToolStripMenuItem item in themeSubmenu.DropDownItems)
            {
                if (item.Tag is ThemePalette pal) item.Checked = _viewModel.CurrentTheme == pal;
            }
            foreach (Forms.ToolStripMenuItem item in scaleSubmenu.DropDownItems)
            {
                if (item.Tag is double sc) item.Checked = Math.Abs(_viewModel.UiScale - sc) < 0.05;
            }
            foreach (Forms.ToolStripMenuItem item in unitsSubmenu.DropDownItems)
            {
                if (item.Tag is SpeedUnit u) item.Checked = _viewModel.CurrentUnit == u;
            }
            adaptersSubmenu.DropDownItems.Clear();
            _viewModel.ReloadAdapters();
            foreach (var adapter in _viewModel.Adapters)
            {
                var adItem = new Forms.ToolStripMenuItem(adapter.Name)
                {
                    Checked = adapter.Id == _viewModel.SelectedAdapterId,
                    ForeColor = Color.FromArgb(248, 250, 252),
                    Tag = adapter.Id
                };
                adItem.Click += (senderObj, ev) =>
                {
                    if (senderObj is Forms.ToolStripMenuItem clicked && clicked.Tag is string id)
                    {
                        _viewModel.SetAdapter(id);
                    }
                };
                adaptersSubmenu.DropDownItems.Add(adItem);
            }
            foreach (Forms.ToolStripItem item in dataCapSubmenu.DropDownItems)
            {
                if (item is Forms.ToolStripMenuItem mi && mi.Tag is double gb)
                {
                    mi.Checked = _viewModel.IsDataCapEnabled && Math.Abs(_viewModel.DataCapGigabytes - gb) < 1.0;
                }
            }
            foreach (Forms.ToolStripMenuItem item in opacitySubmenu.DropDownItems)
            {
                if (item.Text == "100%") item.Checked = Math.Abs(_viewModel.WindowOpacity - 1.0) < 0.05;
                else if (item.Text == "80%") item.Checked = Math.Abs(_viewModel.WindowOpacity - 0.8) < 0.05;
                else if (item.Text == "60%") item.Checked = Math.Abs(_viewModel.WindowOpacity - 0.6) < 0.05;
                else if (item.Text == "40%") item.Checked = Math.Abs(_viewModel.WindowOpacity - 0.4) < 0.05;
            }
        };

        _notifyIcon.ContextMenuStrip = trayMenu;
    }

    private void OnGhostModeChanged(bool isGhost)
    {
        Dispatcher.InvokeAsync(() => ApplyGhostMode(isGhost, showNotification: true));
    }

    private void OnUiScaleChanged(double scale)
    {
        Dispatcher.InvokeAsync(() =>
        {
            ApplyCompactMode(_viewModel.IsCompactMode, animate: true);
            EnsureWithinWorkingArea();
            ReassertTopmost();
        });
    }

    private void OnAcrylicBlurChanged(bool enable)
    {
        Dispatcher.InvokeAsync(() => ApplyAcrylicBackdrop(enable));
    }

    private void ApplyAcrylicBackdrop(bool enable)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        NativeMethods.ApplySystemBackdrop(
            hwnd,
            enable ? NativeMethods.DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW : NativeMethods.DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE);
    }

    private void ApplyGhostMode(bool enabled, bool showNotification)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        NativeMethods.SetClickThrough(hwnd, enabled);

        if (enabled && showNotification)
        {
            _notifyIcon?.ShowBalloonTip(
                2000,
                "InternetMeter Ghost Mode Active",
                "Clicks pass cleanly through widget to underlying windows.\nPress Ctrl+Shift+G or click tray icon to exit.",
                Forms.ToolTipIcon.Info);
        }
    }

    private static System.Drawing.Icon GenerateTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Draw dark rounded backing
        using var backBrush = new SolidBrush(Color.FromArgb(18, 19, 26));
        g.FillEllipse(backBrush, 0, 0, 15, 15);

        // Draw emerald down arrow
        using var greenPen = new Pen(Color.FromArgb(16, 185, 129), 2);
        g.DrawLine(greenPen, 4, 3, 4, 11);
        g.DrawLine(greenPen, 2, 8, 4, 11);
        g.DrawLine(greenPen, 6, 8, 4, 11);

        // Draw sky up arrow
        using var bluePen = new Pen(Color.FromArgb(56, 189, 248), 2);
        g.DrawLine(bluePen, 11, 12, 11, 4);
        g.DrawLine(bluePen, 9, 7, 11, 4);
        g.DrawLine(bluePen, 13, 7, 11, 4);

        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        PopulateAdaptersMenu();
    }

    private void AdaptersMenu_Click(object sender, RoutedEventArgs e)
    {
        AdaptersMenu.IsSubmenuOpen = !AdaptersMenu.IsSubmenuOpen;
    }

    private void PopulateAdaptersMenu()
    {
        _viewModel.ReloadAdapters();
        AdaptersMenu.Items.Clear();

        foreach (var adapter in _viewModel.Adapters)
        {
            var item = new MenuItem
            {
                Header = adapter.Name,
                IsCheckable = true,
                IsChecked = adapter.Id == _viewModel.SelectedAdapterId,
                Tag = adapter.Id
            };
            item.Click += (s, ev) =>
            {
                if (s is MenuItem clicked && clicked.Tag is string id)
                {
                    _viewModel.SetAdapter(id);
                }
            };
            AdaptersMenu.Items.Add(item);
        }

        if (AdaptersMenu.Items.Count == 0)
        {
            AdaptersMenu.Items.Add(new MenuItem { Header = "All Sources (Default)", IsEnabled = false });
        }
    }

    private void MenuUnit_KBps_Click(object sender, RoutedEventArgs e) => _viewModel.SetUnit(SpeedUnit.KBps);
    private void MenuUnit_MBps_Click(object sender, RoutedEventArgs e) => _viewModel.SetUnit(SpeedUnit.MBps);
    private void MenuUnit_Kbps_Click(object sender, RoutedEventArgs e) => _viewModel.SetUnit(SpeedUnit.Kbps);
    private void MenuUnit_Mbps_Click(object sender, RoutedEventArgs e) => _viewModel.SetUnit(SpeedUnit.Mbps);

    private void MenuTheme_EmeraldSky_Click(object sender, RoutedEventArgs e) => _viewModel.SetTheme(ThemePalette.EmeraldSky);
    private void MenuTheme_CyberpunkNeon_Click(object sender, RoutedEventArgs e) => _viewModel.SetTheme(ThemePalette.CyberpunkNeon);
    private void MenuTheme_MonochromeStealth_Click(object sender, RoutedEventArgs e) => _viewModel.SetTheme(ThemePalette.MonochromeStealth);
    private void MenuTheme_SolarAmber_Click(object sender, RoutedEventArgs e) => _viewModel.SetTheme(ThemePalette.SolarAmber);

    private void MenuScale_85_Click(object sender, RoutedEventArgs e) => _viewModel.SetScale(0.85);
    private void MenuScale_100_Click(object sender, RoutedEventArgs e) => _viewModel.SetScale(1.0);
    private void MenuScale_125_Click(object sender, RoutedEventArgs e) => _viewModel.SetScale(1.25);

    private void MenuOpacity_100_Click(object sender, RoutedEventArgs e) => ApplyOpacity(1.0);
    private void MenuOpacity_80_Click(object sender, RoutedEventArgs e) => ApplyOpacity(0.80);
    private void MenuOpacity_60_Click(object sender, RoutedEventArgs e) => ApplyOpacity(0.60);
    private void MenuOpacity_40_Click(object sender, RoutedEventArgs e) => ApplyOpacity(0.40);

    private void ApplyOpacity(double opacity)
    {
        _viewModel.SetOpacity(opacity);
        SetCurrentValue(OpacityProperty, _viewModel.CurrentOpacity);
    }

    public void OpenUsageReport()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_reportWindow == null || !_reportWindow.IsLoaded)
            {
                _reportWindow = new Views.UsageReportWindow(_viewModel.Journal, _viewModel.CurrentUnit);
                _reportWindow.Owner = this;
                _reportWindow.Show();
            }
            else
            {
                if (_reportWindow.WindowState == WindowState.Minimized)
                {
                    _reportWindow.WindowState = WindowState.Normal;
                }
                _reportWindow.Activate();
            }
        });
    }

    public void OpenAnalyticsWindow()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_analyticsWindow == null || !_analyticsWindow.IsLoaded)
            {
                var vm = new BandwidthAnalyticsViewModel(_viewModel.RollingHistory, _viewModel.CurrentUnit, _viewModel.CurrentTheme);
                _analyticsWindow = new Views.BandwidthAnalyticsWindow(vm, _viewModel.EnableAcrylicBlur);
                _analyticsWindow.Owner = this;
                _analyticsWindow.Show();
            }
            else
            {
                if (_analyticsWindow.WindowState == WindowState.Minimized)
                {
                    _analyticsWindow.WindowState = WindowState.Normal;
                }
                _analyticsWindow.Activate();
            }
        });
    }

    public void OpenDataCapConfig()
    {
        Dispatcher.InvokeAsync(() =>
        {
            var win = new Views.DataCapConfigWindow(_viewModel.SettingsService, _viewModel.DataCapService)
            {
                Owner = this
            };
            win.ShowDialog();
            _viewModel.SyncSettingsFromDisk();
            _viewModel.EvaluateDataCap();
        });
    }

    private void OnQuotaToastRequested(QuotaAlertNotification n)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var icon = n.Level switch
            {
                QuotaAlertLevel.Exceeded100 => Forms.ToolTipIcon.Error,
                QuotaAlertLevel.Critical95 => Forms.ToolTipIcon.Warning,
                _ => Forms.ToolTipIcon.Info
            };

            _notifyIcon?.ShowBalloonTip(4000, n.Title, n.Message, icon);
        });
    }

    private void DataCapPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (e.ClickCount == 2)
        {
            OpenDataCapConfig();
            return;
        }

        if (sender is FrameworkElement fe && fe.ToolTip is System.Windows.Controls.ToolTip tt)
        {
            tt.PlacementTarget = fe;
            tt.IsOpen = !tt.IsOpen;
        }
    }

    private void MenuDataCap_50_Click(object sender, RoutedEventArgs e) => _viewModel.SetDataCapPreset(50.0);
    private void MenuDataCap_100_Click(object sender, RoutedEventArgs e) => _viewModel.SetDataCapPreset(100.0);
    private void MenuDataCap_250_Click(object sender, RoutedEventArgs e) => _viewModel.SetDataCapPreset(250.0);
    private void MenuDataCap_500_Click(object sender, RoutedEventArgs e) => _viewModel.SetDataCapPreset(500.0);
    private void MenuDataCap_1000_Click(object sender, RoutedEventArgs e) => _viewModel.SetDataCapPreset(1024.0);

    private const double FullWidth = 274.0;
    private const double CompactWidth = 168.0;

    private void OnCompactModeToggled(bool isCompact)
    {
        Dispatcher.InvokeAsync(() => ApplyCompactMode(isCompact, animate: true));
    }

    private void ApplyCompactMode(bool isCompact, bool animate)
    {
        double scale = _viewModel?.UiScale > 0 ? _viewModel.UiScale : 1.0;
        double targetWidth = (isCompact ? CompactWidth : FullWidth) * scale;

        if (!isCompact)
        {
            var workArea = GetCurrentWorkArea();
            if (Top + (240 * scale) > workArea.Bottom)
            {
                Top = Math.Max(workArea.Top + 8, workArea.Bottom - (260 * scale));
            }
            if (Left + targetWidth > workArea.Right)
            {
                Left = Math.Max(workArea.Left + 8, workArea.Right - targetWidth - 8);
            }
        }

        if (animate)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            anim.Completed += (s, e) =>
            {
                EnsureWithinWorkingArea();
                ReassertTopmost();
            };
            BeginAnimation(WidthProperty, anim);
        }
        else
        {
            Width = targetWidth;
            EnsureWithinWorkingArea();
            ReassertTopmost();
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(hwnd, NativeMethods.GHOST_HOTKEY_ID);
            NativeMethods.UnregisterHotKey(hwnd, NativeMethods.INSPECTOR_HOTKEY_ID);
            NativeMethods.UnregisterHotKey(hwnd, NativeMethods.REPORT_HOTKEY_ID);
            NativeMethods.UnregisterHotKey(hwnd, NativeMethods.DOCK_HOTKEY_ID);
            NativeMethods.UnregisterHotKey(hwnd, NativeMethods.ANALYTICS_HOTKEY_ID);
        }

        if (_reportWindow != null && _reportWindow.IsLoaded)
        {
            _reportWindow.Close();
            _reportWindow = null;
        }

        if (_analyticsWindow != null && _analyticsWindow.IsLoaded)
        {
            _analyticsWindow.Close();
            _analyticsWindow = null;
        }

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    private static void ApplyDarkMenuTheme(Forms.ToolStripDropDownItem item, Forms.ToolStripRenderer renderer, Font font)
    {
        if (item.DropDown != null)
        {
            item.DropDown.Renderer = renderer;
            item.DropDown.Font = font;
            item.DropDown.BackColor = Color.FromArgb(22, 24, 34);
            item.DropDown.ForeColor = Color.FromArgb(248, 250, 252);
            item.DropDown.ShowImageMargin = true;
            item.DropDown.ShowCheckMargin = false;
        }
    }

    private class ModernDarkMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        public ModernDarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderToolStripBackground(Forms.ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(Color.FromArgb(22, 24, 34)); // #161822
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderImageMargin(Forms.ToolStripRenderEventArgs e)
        {
            // Paint margin in the same seamless background so there is no gray gutter bar
            using var brush = new SolidBrush(Color.FromArgb(22, 24, 34));
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(51, 65, 85), 1); // #334155
            var r = e.AffectedBounds;
            e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
        }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var item = e.Item;
            if (item.Selected && item.Enabled)
            {
                var rect = new Rectangle(3, 1, item.Width - 6, item.Height - 2);
                using var brush = new SolidBrush(Color.FromArgb(38, 43, 61)); // #262B3D
                using var path = CreateRoundedRectangle(rect, 4);
                g.FillPath(brush, path);
            }
        }

        protected override void OnRenderItemCheck(Forms.ToolStripItemImageRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = e.ImageRectangle;
            int size = 15;
            int x = rect.X + (rect.Width - size) / 2;
            int y = rect.Y + (rect.Height - size) / 2;
            var boxRect = new Rectangle(x, y, size, size);

            // Draw rounded backing box: #1E2436 matching WPF CheckMarkBox
            using (var boxBrush = new SolidBrush(Color.FromArgb(30, 36, 54)))
            using (var boxPath = CreateRoundedRectangle(boxRect, 3))
            {
                g.FillPath(boxBrush, boxPath);
            }

            // Draw clean emerald green checkmark ✓: #10B981 matching WPF
            using (var checkPen = new Pen(Color.FromArgb(16, 185, 129), 2f))
            {
                checkPen.StartCap = LineCap.Round;
                checkPen.EndCap = LineCap.Round;
                Point p1 = new(x + 3, y + 7);
                Point p2 = new(x + 6, y + 11);
                Point p3 = new(x + 12, y + 4);

                g.DrawLine(checkPen, p1, p2);
                g.DrawLine(checkPen, p2, p3);
            }
        }

        protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
        {
            var g = e.Graphics;
            var rect = e.Item.ContentRectangle;
            int y = rect.Top + rect.Height / 2;
            using var pen = new Pen(Color.FromArgb(42, 47, 69), 1); // #2A2F45
            g.DrawLine(pen, rect.Left + 8, y, rect.Right - 8, y);
        }

        protected override void OnRenderArrow(Forms.ToolStripArrowRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var color = e.Item.Selected ? Color.FromArgb(248, 250, 252) : Color.FromArgb(148, 163, 184); // #94A3B8
            using var brush = new SolidBrush(color);

            var r = e.ArrowRectangle;
            Point[] points = [
                new(r.Left + 3, r.Top + r.Height / 2 - 4),
                new(r.Left + 7, r.Top + r.Height / 2),
                new(r.Left + 3, r.Top + r.Height / 2 + 4)
            ];
            g.FillPolygon(brush, points);
        }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = !e.Item.Enabled
                ? Color.FromArgb(100, 116, 139) // #64748B
                : e.Item.Selected
                    ? Color.FromArgb(255, 255, 255)
                    : Color.FromArgb(248, 250, 252); // #F8FAFC
            base.OnRenderItemText(e);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private class DarkColorTable : Forms.ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(22, 24, 34);
        public override Color MenuBorder => Color.FromArgb(51, 65, 85);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(38, 43, 61);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(38, 43, 61);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(38, 43, 61);
        public override Color MenuStripGradientBegin => Color.FromArgb(22, 24, 34);
        public override Color MenuStripGradientEnd => Color.FromArgb(22, 24, 34);
        public override Color CheckBackground => Color.FromArgb(30, 36, 54);
        public override Color CheckSelectedBackground => Color.FromArgb(30, 36, 54);
        public override Color CheckPressedBackground => Color.FromArgb(30, 36, 54);
        public override Color ImageMarginGradientBegin => Color.FromArgb(22, 24, 34);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(22, 24, 34);
        public override Color ImageMarginGradientEnd => Color.FromArgb(22, 24, 34);
        public override Color SeparatorDark => Color.FromArgb(42, 47, 69);
        public override Color SeparatorLight => Color.FromArgb(42, 47, 69);
    }
}
