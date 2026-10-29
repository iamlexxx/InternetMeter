using InternetMeter.Models;

namespace InternetMeter.Services;

public interface IRollingHistoryService
{
    int Capacity { get; }
    int Count { get; }
    void RecordSample(DateTime timestamp, double downloadSpeedBytes, double uploadSpeedBytes, string? topProcessName, double topProcessSpeedBytes);
    IReadOnlyList<BandwidthSample> GetSamples(TimeSpan duration);
    IReadOnlyList<BandwidthSample> GetAllSamples();
    void Clear();
    event Action<BandwidthSample>? SampleRecorded;
}

