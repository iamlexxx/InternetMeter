namespace InternetMeter.Models;

public enum QuotaAlertLevel
{
    Normal,         // < 80%
    Warning80,      // 80% - 94.9%
    Critical95,     // 95% - 99.9%
    Exceeded100     // >= 100%
}

public record DataCapQuotaInfo
{
    public bool IsEnabled { get; init; }
    public double CapBytes { get; init; }
    public double UsedBytes { get; init; }
    public double RemainingBytes => Math.Max(0, CapBytes - UsedBytes);
    public double UsagePercent => CapBytes > 0 ? (UsedBytes / CapBytes) * 100.0 : 0.0;
    public int DaysRemainingInCycle { get; init; }
    public DateTime CycleStartUtc { get; init; }
    public DateTime CycleEndUtc { get; init; }
    public QuotaAlertLevel AlertLevel { get; init; }
    public string TopConsumerProcess { get; init; } = "";
    public double TopConsumerBytes { get; init; }
    public string FormattedUsed { get; init; } = "0 GB";
    public string FormattedCap { get; init; } = "100 GB";
    public string FormattedRemaining { get; init; } = "0 GB";
    public string ProgressColorHex { get; init; } = "#10B981";
    public string StatusText { get; init; } = "On Track";
}

public record QuotaAlertNotification(
    QuotaAlertLevel Level,
    string Title,
    string Message,
    double CurrentUsagePercent,
    string FormattedUsed,
    string FormattedCap
);
