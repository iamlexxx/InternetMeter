using InternetMeter.Models;

namespace InternetMeter.Services;

public interface INetworkService
{
    IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters();
    (double downloadSpeedBytes, double uploadSpeedBytes, bool isConnected) SampleTraffic(string adapterId);
    (long totalDownloadBytes, long totalUploadBytes) GetSessionTotals();
    void ResetSessionTotals();
    (long deltaDownloadBytes, long deltaUploadBytes) GetLastSampleDeltas();
}

