using InternetMeter.Models;

namespace InternetMeter.Services;

public interface IProcessNetworkService
{
    ITrafficJournalService Journal { get; }

    void UpdateTraffic(
        long systemDeltaDlBytes,
        long systemDeltaUlBytes,
        double systemDlRate,
        double systemUlRate);

    IReadOnlyList<ProcessTrafficInfo> GetTopProcesses(
        int maxCount,
        SpeedUnit unit,
        bool isTotalMode,
        double totalSystemDownloadBytes = 0,
        double totalSystemUploadBytes = 0);

    void Reset();
}
