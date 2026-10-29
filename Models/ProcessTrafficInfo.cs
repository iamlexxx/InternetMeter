using System.Windows.Media;

namespace InternetMeter.Models;

public class ProcessTrafficInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public double DownloadSpeedBytes { get; set; }
    public double UploadSpeedBytes { get; set; }
    public long TotalBytes { get; set; }

    public string FormattedDownloadSpeed { get; set; } = "0 B/s";
    public string FormattedUploadSpeed { get; set; } = "0 B/s";
    public string FormattedTotalData { get; set; } = "0 B";

    public double PercentageOfTotal { get; set; }
    public bool IsActive { get; set; } = true;

    public string? ExecutablePath { get; set; }
    public ImageSource? Icon { get; set; }

    public string DisplayValueMain { get; set; } = string.Empty;
    public string DisplayValueSecondary { get; set; } = string.Empty;
}
