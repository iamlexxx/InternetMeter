using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using MediaColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;

namespace InternetMeter.Views;

public partial class BandwidthAnalyticsWindow : Window
{
    private readonly BandwidthAnalyticsViewModel _viewModel;
    private readonly bool _enableAcrylicBlur;
    private readonly DispatcherTimer _exportNoticeTimer;

    private static readonly SolidColorBrush ActiveTabBg = new(MediaColor.FromRgb(99, 102, 241));
    private static readonly SolidColorBrush ActiveTabText = new(MediaColor.FromRgb(255, 255, 255));
    private static readonly SolidColorBrush InactiveTabBg = new(MediaColor.FromArgb(0, 0, 0, 0));
    private static readonly SolidColorBrush InactiveTabText = new(MediaColor.FromRgb(148, 163, 184));

    public BandwidthAnalyticsWindow(BandwidthAnalyticsViewModel viewModel, bool enableAcrylicBlur = false)
    {
        _viewModel = viewModel;
        _enableAcrylicBlur = enableAcrylicBlur;
        DataContext = _viewModel;

        InitializeComponent();

        _exportNoticeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _exportNoticeTimer.Tick += (s, e) =>
        {
            ExportNoticeText.Visibility = Visibility.Collapsed;
            _exportNoticeTimer.Stop();
        };

        _viewModel.RequestRedraw += OnRequestRedraw;
        _viewModel.CsvExportCompleted += OnCsvExportCompleted;

        Loaded += BandwidthAnalyticsWindow_Loaded;
        SizeChanged += BandwidthAnalyticsWindow_SizeChanged;
        Closed += BandwidthAnalyticsWindow_Closed;
    }

    private void BandwidthAnalyticsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_enableAcrylicBlur)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                NativeMethods.ApplySystemBackdrop(hwnd, NativeMethods.DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW);
            }
        }

        UpdateTabStyles();
        UpdateXAxisLabels();
        RedrawCanvas();
    }

    private void BandwidthAnalyticsWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RedrawCanvas();
    }

    private void BandwidthAnalyticsWindow_Closed(object? sender, EventArgs e)
    {
        _viewModel.RequestRedraw -= OnRequestRedraw;
        _viewModel.CsvExportCompleted -= OnCsvExportCompleted;
        _exportNoticeTimer.Stop();
    }

    private void OnRequestRedraw()
    {
        Dispatcher.InvokeAsync(RedrawCanvas);
    }

    private void OnCsvExportCompleted(string filePath)
    {
        Dispatcher.InvokeAsync(() =>
        {
            ExportNoticeText.Text = "Saved to Downloads!";
            ExportNoticeText.Visibility = Visibility.Visible;
            _exportNoticeTimer.Stop();
            _exportNoticeTimer.Start();

            try
            {
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                // Silently ignore explorer launch failure
            }
        });
    }

    private void RedrawCanvas()
    {
        double w = GraphCanvas.ActualWidth;
        double h = GraphCanvas.ActualHeight;

        if (w <= 0 || h <= 0) return;

        var samples = _viewModel.CurrentSamples;
        int count = samples.Count;

        if (count < 2)
        {
            DownloadPath.Data = null;
            DownloadAreaPath.Data = null;
            UploadPath.Data = null;
            UploadAreaPath.Data = null;
            return;
        }

        double maxY = _viewModel.MaxYBytes > 0 ? _viewModel.MaxYBytes : 1024 * 1024;

        var dlLineGeom = new StreamGeometry();
        var dlAreaGeom = new StreamGeometry();
        var ulLineGeom = new StreamGeometry();
        var ulAreaGeom = new StreamGeometry();

        using (var ctxDlLine = dlLineGeom.Open())
        using (var ctxDlArea = dlAreaGeom.Open())
        using (var ctxUlLine = ulLineGeom.Open())
        using (var ctxUlArea = ulAreaGeom.Open())
        {
            double firstX = 0;
            double firstDlY = h * (1.0 - Math.Clamp(samples[0].DownloadSpeedBytes / maxY, 0.0, 1.0));
            double firstUlY = h * (1.0 - Math.Clamp(samples[0].UploadSpeedBytes / maxY, 0.0, 1.0));

            // Start line curves
            ctxDlLine.BeginFigure(new WpfPoint(firstX, firstDlY), isFilled: false, isClosed: false);
            ctxUlLine.BeginFigure(new WpfPoint(firstX, firstUlY), isFilled: false, isClosed: false);

            // Start area polygon curves
            ctxDlArea.BeginFigure(new WpfPoint(firstX, h), isFilled: true, isClosed: true);
            ctxDlArea.LineTo(new WpfPoint(firstX, firstDlY), isStroked: true, isSmoothJoin: true);

            ctxUlArea.BeginFigure(new WpfPoint(firstX, h), isFilled: true, isClosed: true);
            ctxUlArea.LineTo(new WpfPoint(firstX, firstUlY), isStroked: true, isSmoothJoin: true);

            for (int i = 1; i < count; i++)
            {
                double x = w * ((double)i / (count - 1));
                double dlY = h * (1.0 - Math.Clamp(samples[i].DownloadSpeedBytes / maxY, 0.0, 1.0));
                double ulY = h * (1.0 - Math.Clamp(samples[i].UploadSpeedBytes / maxY, 0.0, 1.0));

                ctxDlLine.LineTo(new WpfPoint(x, dlY), isStroked: true, isSmoothJoin: true);
                ctxUlLine.LineTo(new WpfPoint(x, ulY), isStroked: true, isSmoothJoin: true);

                ctxDlArea.LineTo(new WpfPoint(x, dlY), isStroked: true, isSmoothJoin: true);
                ctxUlArea.LineTo(new WpfPoint(x, ulY), isStroked: true, isSmoothJoin: true);
            }

            // Close area polygons to bottom edge
            ctxDlArea.LineTo(new WpfPoint(w, h), isStroked: true, isSmoothJoin: true);
            ctxUlArea.LineTo(new WpfPoint(w, h), isStroked: true, isSmoothJoin: true);
        }

        dlLineGeom.Freeze();
        dlAreaGeom.Freeze();
        ulLineGeom.Freeze();
        ulAreaGeom.Freeze();

        DownloadPath.Data = dlLineGeom;
        DownloadAreaPath.Data = dlAreaGeom;
        UploadPath.Data = ulLineGeom;
        UploadAreaPath.Data = ulAreaGeom;
    }

    private void PlotCanvas_MouseMove(object sender, WpfMouseEventArgs e)
    {
        double w = GraphCanvas.ActualWidth;
        double h = GraphCanvas.ActualHeight;

        if (w <= 0 || h <= 0) return;

        WpfPoint mousePos = e.GetPosition(GraphCanvas);
        double mouseX = Math.Clamp(mousePos.X, 0, w);
        double normalizedX = mouseX / w;

        var sample = _viewModel.GetSampleAt(normalizedX);
        if (sample == null)
        {
            PlotCanvas_MouseLeave(sender, e);
            return;
        }

        double maxY = _viewModel.MaxYBytes > 0 ? _viewModel.MaxYBytes : 1024 * 1024;
        double dlY = h * (1.0 - Math.Clamp(sample.DownloadSpeedBytes / maxY, 0.0, 1.0));
        double ulY = h * (1.0 - Math.Clamp(sample.UploadSpeedBytes / maxY, 0.0, 1.0));

        // Position Crosshair
        CrosshairLine.X1 = mouseX;
        CrosshairLine.X2 = mouseX;
        CrosshairLine.Y1 = 0;
        CrosshairLine.Y2 = h;
        CrosshairLine.Visibility = Visibility.Visible;

        // Position Dots
        Canvas.SetLeft(DlDot, mouseX - 4);
        Canvas.SetTop(DlDot, dlY - 4);
        DlDot.Visibility = Visibility.Visible;

        Canvas.SetLeft(UlDot, mouseX - 4);
        Canvas.SetTop(UlDot, ulY - 4);
        UlDot.Visibility = Visibility.Visible;

        // Update Tooltip
        TooltipTimeText.Text = sample.Timestamp.ToString("HH:mm:ss");
        TooltipDlText.Text = $"↓ {UnitFormatter.FormatSpeed(sample.DownloadSpeedBytes, _viewModel.CurrentUnit)}";
        TooltipUlText.Text = $"↑ {UnitFormatter.FormatSpeed(sample.UploadSpeedBytes, _viewModel.CurrentUnit)}";

        if (!string.IsNullOrEmpty(sample.TopProcessName) && sample.TopProcessSpeedBytes > 0)
        {
            TooltipAppText.Text = $"{sample.TopProcessName} ({UnitFormatter.FormatSpeed(sample.TopProcessSpeedBytes, _viewModel.CurrentUnit)})";
            TooltipAppText.Visibility = Visibility.Visible;
        }
        else
        {
            TooltipAppText.Text = "System / Idle";
            TooltipAppText.Visibility = Visibility.Visible;
        }

        // Measure tooltip for clamping
        TooltipBorder.Measure(new WpfSize(double.PositiveInfinity, double.PositiveInfinity));
        double ttW = TooltipBorder.DesiredSize.Width > 0 ? TooltipBorder.DesiredSize.Width : 160;
        double ttH = TooltipBorder.DesiredSize.Height > 0 ? TooltipBorder.DesiredSize.Height : 70;

        double ttX = mouseX + 14;
        if (ttX + ttW > w - 8)
        {
            ttX = mouseX - ttW - 14;
        }
        if (ttX < 8) ttX = 8;

        double ttY = Math.Min(dlY, ulY) - ttH - 12;
        if (ttY < 8)
        {
            ttY = Math.Max(dlY, ulY) + 12;
        }
        if (ttY + ttH > h - 8)
        {
            ttY = h - ttH - 8;
        }

        Canvas.SetLeft(TooltipBorder, ttX);
        Canvas.SetTop(TooltipBorder, ttY);
        TooltipBorder.Visibility = Visibility.Visible;
    }

    private void PlotCanvas_MouseLeave(object sender, WpfMouseEventArgs e)
    {
        CrosshairLine.Visibility = Visibility.Collapsed;
        DlDot.Visibility = Visibility.Collapsed;
        UlDot.Visibility = Visibility.Collapsed;
        TooltipBorder.Visibility = Visibility.Collapsed;
    }

    private void UpdateTabStyles()
    {
        Tab15mBtn.Background = _viewModel.Is15mActive ? ActiveTabBg : InactiveTabBg;
        Tab15mBtn.Foreground = _viewModel.Is15mActive ? ActiveTabText : InactiveTabText;

        Tab30mBtn.Background = _viewModel.Is30mActive ? ActiveTabBg : InactiveTabBg;
        Tab30mBtn.Foreground = _viewModel.Is30mActive ? ActiveTabText : InactiveTabText;

        Tab60mBtn.Background = _viewModel.Is60mActive ? ActiveTabBg : InactiveTabBg;
        Tab60mBtn.Foreground = _viewModel.Is60mActive ? ActiveTabText : InactiveTabText;
    }

    private void UpdateXAxisLabels()
    {
        if (_viewModel.Is15mActive)
        {
            XAxisStartLabel.Text = "-15m";
            XAxisQuarterLabel.Text = "-11m";
            XAxisMidLabel.Text = "-7m";
            XAxisThreeQuarterLabel.Text = "-3m";
        }
        else if (_viewModel.Is30mActive)
        {
            XAxisStartLabel.Text = "-30m";
            XAxisQuarterLabel.Text = "-22m";
            XAxisMidLabel.Text = "-15m";
            XAxisThreeQuarterLabel.Text = "-7m";
        }
        else
        {
            XAxisStartLabel.Text = "-60m";
            XAxisQuarterLabel.Text = "-45m";
            XAxisMidLabel.Text = "-30m";
            XAxisThreeQuarterLabel.Text = "-15m";
        }
    }

    private void Tab15m_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetTimeframe(TimeSpan.FromMinutes(15));
        UpdateTabStyles();
        UpdateXAxisLabels();
        RedrawCanvas();
    }

    private void Tab30m_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetTimeframe(TimeSpan.FromMinutes(30));
        UpdateTabStyles();
        UpdateXAxisLabels();
        RedrawCanvas();
    }

    private void Tab60m_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetTimeframe(TimeSpan.FromMinutes(60));
        UpdateTabStyles();
        UpdateXAxisLabels();
        RedrawCanvas();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
