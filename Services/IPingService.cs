using InternetMeter.Models;

namespace InternetMeter.Services;

public interface IPingService : IDisposable
{
    event Action<PingTelemetry>? LatencyUpdated;
    event Action<NetworkHealthTelemetry>? HealthUpdated;
    bool IsRunning { get; }
    NetworkHealthTelemetry? CurrentHealth { get; }
    void Start(int intervalSeconds = 2, string targetHost = "1.1.1.1");
    void Stop();
    void UpdateThroughput(double downloadSpeedBytes, double uploadSpeedBytes);
    Task<PingTelemetry> ProbeOnceAsync(CancellationToken cancellationToken = default);
}
