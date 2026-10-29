namespace InternetMeter.Models;

public class AppSettings
{
    public bool AlwaysOnTop { get; set; } = true;
    public string SelectedAdapterId { get; set; } = "ALL";
    public SpeedUnit Unit { get; set; } = SpeedUnit.Mbps;
    public double Opacity { get; set; } = 0.95;
    public double? WindowLeft { get; set; } = null;
    public double? WindowTop { get; set; } = null;
    public bool ShowSparkline { get; set; } = true;
    public bool ShowFooter { get; set; } = true;
    public bool ShowPingLatency { get; set; } = true;
    public string PingTargetHost { get; set; } = "1.1.1.1";
    public int PingIntervalSeconds { get; set; } = 3;
    public int RefreshIntervalMs { get; set; } = 1000;
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimizedOnBoot { get; set; } = true;
    public bool IsGhostMode { get; set; } = false;
    public double GhostOpacity { get; set; } = 0.65;
    public string GhostHotkeyText { get; set; } = "Ctrl+Shift+G";
    public bool ShowProcessInspector { get; set; } = true;
    public bool IsProcessInspectorExpanded { get; set; } = false;
    public bool IsProcessInspectorTotalMode { get; set; } = false;
    public int MaxProcessInspectorCount { get; set; } = 4;
    public bool IsCompactMode { get; set; } = false;
    public bool EnableEdgeSnapping { get; set; } = true;
    public double? CompactLeft { get; set; } = null;
    public double? CompactTop { get; set; } = null;
    public ThemePalette Theme { get; set; } = ThemePalette.EmeraldSky;
    public double UiScale { get; set; } = 1.0;
    public bool EnableAcrylicBlur { get; set; } = false;

    // Monthly Data Cap & Hotspot Quota Settings
    public bool DataCapEnabled { get; set; } = false;
    public double DataCapGigabytes { get; set; } = 100.0;
    public int BillingCycleResetDay { get; set; } = 1;
    public double WarningThresholdPercent { get; set; } = 80.0;
    public double CriticalThresholdPercent { get; set; } = 95.0;
    public bool EnableToastNotifications { get; set; } = true;
    public string LastNotifiedThreshold { get; set; } = "None"; // "None", "Warning80", "Critical95", "Exceeded100"
    public string LastNotifiedCycleStart { get; set; } = "";     // ISO 8601 date string e.g. "2026-10-01"
}
