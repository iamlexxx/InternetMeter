using InternetMeter.Models;

namespace InternetMeter.Services;

public static class UnitFormatter
{
    public static string GetUnitLabel(SpeedUnit unit) => unit switch
    {
        SpeedUnit.KBps => "KB/s",
        SpeedUnit.MBps => "MB/s",
        SpeedUnit.Kbps => "Kbps",
        SpeedUnit.Mbps => "Mbps",
        _ => "Mbps"
    };

    public static string FormatSpeed(double bytesPerSecond, SpeedUnit unit)
    {
        if (bytesPerSecond < 0) bytesPerSecond = 0;

        return unit switch
        {
            SpeedUnit.KBps => (bytesPerSecond / 1024.0).ToString("F1"),
            SpeedUnit.MBps => (bytesPerSecond / (1024.0 * 1024.0)).ToString("F2"),
            SpeedUnit.Kbps => ((bytesPerSecond * 8.0) / 1000.0).ToString("F0"),
            SpeedUnit.Mbps => ((bytesPerSecond * 8.0) / 1_000_000.0).ToString("F2"),
            _ => ((bytesPerSecond * 8.0) / 1_000_000.0).ToString("F2")
        };
    }

    public static string FormatDataSize(long bytes)
    {
        if (bytes < 0) bytes = 0;

        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{(bytes / 1024.0):F1} KB",
            < 1024 * 1024 * 1024 => $"{(bytes / (1024.0 * 1024.0)):F1} MB",
            _ => $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB"
        };
    }
}

