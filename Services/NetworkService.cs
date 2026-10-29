using System.Diagnostics;
using System.Net.NetworkInformation;
using InternetMeter.Models;

namespace InternetMeter.Services;

public class NetworkService : INetworkService
{
    private class AdapterSnapshot
    {
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
        public long TimestampStopwatchTicks { get; set; }
    }

    private readonly Dictionary<string, AdapterSnapshot> _snapshots = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private long _sessionTotalDownloadBytes;
    private long _sessionTotalUploadBytes;
    private long _lastDeltaRx;
    private long _lastDeltaTx;

    public IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters()
    {
        var list = new List<NetworkAdapterInfo>
        {
            new()
            {
                Id = "ALL",
                Name = "All Sources",
                Description = "Aggregate of all active non-loopback network adapters",
                IsAllSources = true
            }
        };

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            foreach (var nic in interfaces)
            {
                list.Add(new NetworkAdapterInfo
                {
                    Id = nic.Id,
                    Name = $"{nic.Name} ({nic.NetworkInterfaceType})",
                    Description = nic.Description,
                    IsAllSources = false
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to enumerate network interfaces: {ex.Message}");
        }

        return list;
    }

    public (double downloadSpeedBytes, double uploadSpeedBytes, bool isConnected) SampleTraffic(string adapterId)
    {
        var currentTicks = _stopwatch.ElapsedTicks;
        var interfaces = GetFilteredInterfaces();
        bool isConnected = interfaces.Any(i => i.OperationalStatus == OperationalStatus.Up);

        if (!isConnected)
        {
            return (0, 0, false);
        }

        if (string.Equals(adapterId, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            double totalDlRate = 0;
            double totalUlRate = 0;
            long totalDeltaRx = 0;
            long totalDeltaTx = 0;

            foreach (var nic in interfaces.Where(i => i.OperationalStatus == OperationalStatus.Up))
            {
                var (dlRate, ulRate, deltaRx, deltaTx) = CalculateInterfaceSpeed(nic, currentTicks);
                totalDlRate += dlRate;
                totalUlRate += ulRate;
                totalDeltaRx += deltaRx;
                totalDeltaTx += deltaTx;
            }

            _lastDeltaRx = totalDeltaRx;
            _lastDeltaTx = totalDeltaTx;
            return (totalDlRate, totalUlRate, true);
        }
        else
        {
            var targetNic = interfaces.FirstOrDefault(i => i.Id == adapterId);
            if (targetNic == null || targetNic.OperationalStatus != OperationalStatus.Up)
            {
                _lastDeltaRx = 0;
                _lastDeltaTx = 0;
                return (0, 0, targetNic?.OperationalStatus == OperationalStatus.Up);
            }

            var (dlRate, ulRate, deltaRx, deltaTx) = CalculateInterfaceSpeed(targetNic, currentTicks);
            _lastDeltaRx = deltaRx;
            _lastDeltaTx = deltaTx;
            return (dlRate, ulRate, true);
        }
    }

    private (double dlRate, double ulRate, long deltaRx, long deltaTx) CalculateInterfaceSpeed(NetworkInterface nic, long currentTicks)
    {
        try
        {
            var stats = nic.GetIPStatistics();
            var currentRx = stats.BytesReceived;
            var currentTx = stats.BytesSent;

            if (!_snapshots.TryGetValue(nic.Id, out var previous))
            {
                _snapshots[nic.Id] = new AdapterSnapshot
                {
                    BytesReceived = currentRx,
                    BytesSent = currentTx,
                    TimestampStopwatchTicks = currentTicks
                };
                return (0, 0, 0, 0);
            }

            var elapsedSeconds = (double)(currentTicks - previous.TimestampStopwatchTicks) / Stopwatch.Frequency;
            if (elapsedSeconds <= 0.05)
            {
                return (0, 0, 0, 0);
            }

            var deltaRx = currentRx - previous.BytesReceived;
            var deltaTx = currentTx - previous.BytesSent;

            // Counter wrapped or adapter reset
            if (deltaRx < 0) deltaRx = 0;
            if (deltaTx < 0) deltaTx = 0;

            previous.BytesReceived = currentRx;
            previous.BytesSent = currentTx;
            previous.TimestampStopwatchTicks = currentTicks;

            Interlocked.Add(ref _sessionTotalDownloadBytes, deltaRx);
            Interlocked.Add(ref _sessionTotalUploadBytes, deltaTx);

            var dlRate = deltaRx / elapsedSeconds;
            var ulRate = deltaTx / elapsedSeconds;

            return (dlRate, ulRate, deltaRx, deltaTx);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    private static NetworkInterface[] GetFilteredInterfaces()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToArray();
        }
        catch
        {
            return Array.Empty<NetworkInterface>();
        }
    }

    public (long totalDownloadBytes, long totalUploadBytes) GetSessionTotals()
    {
        return (Interlocked.Read(ref _sessionTotalDownloadBytes),
                Interlocked.Read(ref _sessionTotalUploadBytes));
    }

    public (long deltaDownloadBytes, long deltaUploadBytes) GetLastSampleDeltas()
    {
        return (Interlocked.Read(ref _lastDeltaRx),
                Interlocked.Read(ref _lastDeltaTx));
    }

    public void ResetSessionTotals()
    {
        Interlocked.Exchange(ref _sessionTotalDownloadBytes, 0);
        Interlocked.Exchange(ref _sessionTotalUploadBytes, 0);
    }
}

