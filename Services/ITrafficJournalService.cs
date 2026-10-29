using System.Windows.Media;
using InternetMeter.Models;

namespace InternetMeter.Services;

public interface ITrafficJournalService
{
    void RecordTraffic(
        string processName,
        string? exePath,
        ImageSource? icon,
        long deltaDownloadBytes,
        long deltaUploadBytes,
        double currentDlSpeed,
        double currentUlSpeed,
        bool isActive);

    void UpdateActiveProcessList(ISet<string> activeProcessNames);

    IReadOnlyList<ProcessUsageRecord> GetRecords(
        TrafficTimeframe timeframe,
        SpeedUnit unit,
        string? searchFilter = null,
        string sortBy = "Total",
        bool descending = true);

    (long TotalDownload, long TotalUpload, long TotalCombined, int ActiveCount, int ClosedCount) GetSummary(
        TrafficTimeframe timeframe);

    (long TotalDownload, long TotalUpload, long TotalCombined) GetBillingCycleUsage(DateTime cycleStartUtc, DateTime cycleEndUtc);

    IReadOnlyList<ProcessUsageRecord> GetBillingCycleRecords(DateTime cycleStartUtc, DateTime cycleEndUtc, SpeedUnit unit);

    void ResetSession();

    string ExportToCsv(TrafficTimeframe timeframe);

    void SaveHistory();
}

