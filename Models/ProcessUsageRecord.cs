using System.Windows.Media;

namespace InternetMeter.Models;

public enum TrafficTimeframe
{
    Session,
    Today,
    Month
}

public class ProcessUsageRecord
{
    public string ProcessName { get; set; } = string.Empty;
    public long TotalDownloadBytes { get; set; }
    public long TotalUploadBytes { get; set; }
    public long CombinedBytes => TotalDownloadBytes + TotalUploadBytes;

    public double CurrentDownloadSpeed { get; set; }
    public double CurrentUploadSpeed { get; set; }

    public string FormattedTotalDownload { get; set; } = "0 B";
    public string FormattedTotalUpload { get; set; } = "0 B";
    public string FormattedTotal { get; set; } = "0 B";

    public string FormattedCurrentDownload { get; set; } = "0 B/s";
    public string FormattedCurrentUpload { get; set; } = "0 B/s";

    public double PercentageOfTotal { get; set; }
    public bool IsActive { get; set; } = true;

    public string? ExecutablePath { get; set; }
    public ImageSource? Icon { get; set; }

    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;
}

