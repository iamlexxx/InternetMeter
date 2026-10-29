using System.Diagnostics;
using System.Net.NetworkInformation;
using InternetMeter.Models;

namespace InternetMeter.Services;

public class PingService : IPingService
{
    private static readonly byte[] Buffer32 = new byte[32];
    private readonly PingOptions _options = new(64, true);

    private CancellationTokenSource? _cts;
    private Task? _probeTask;
    private string _targetHost = "1.1.1.1";
    private int _intervalSeconds = 2;
    private bool _disposed;

    private readonly Queue<long> _rttHistory = new();
    private readonly Queue<bool> _lossHistory = new();
    private readonly Queue<long> _baselineRttHistory = new();
    private const int LossWindowCapacity = 20;
    private const int RttHistoryCapacity = 50;
    private const int BaselineHistoryCapacity = 10;

    private long? _lastRtt;
    private double _smoothedJitter;
    private double _currentDlBytes;
    private double _currentUlBytes;
    private readonly object _stateLock = new();

    public event Action<PingTelemetry>? LatencyUpdated;
    public event Action<NetworkHealthTelemetry>? HealthUpdated;

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;
    public NetworkHealthTelemetry? CurrentHealth { get; private set; }

    public void Start(int intervalSeconds = 2, string targetHost = "1.1.1.1")
    {
        if (IsRunning)
        {
            if (_intervalSeconds == intervalSeconds && string.Equals(_targetHost, targetHost, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            Stop();
        }

        _intervalSeconds = Math.Max(1, intervalSeconds);
        _targetHost = string.IsNullOrWhiteSpace(targetHost) ? "1.1.1.1" : targetHost.Trim();
        _cts = new CancellationTokenSource();

        _probeTask = Task.Run(() => ProbeLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        if (_cts != null)
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            catch
            {
                // Silently handle race condition
            }
            finally
            {
                _cts = null;
            }
        }
    }

    public void UpdateThroughput(double downloadSpeedBytes, double uploadSpeedBytes)
    {
        lock (_stateLock)
        {
            _currentDlBytes = Math.Max(0, downloadSpeedBytes);
            _currentUlBytes = Math.Max(0, uploadSpeedBytes);
        }
    }

    public async Task<PingTelemetry> ProbeOnceAsync(CancellationToken cancellationToken = default)
    {
        var target = ResolveTargetHost();

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(target, 1200, Buffer32, _options)
                                  .WaitAsync(cancellationToken);

            ProcessProbeResult(reply.RoundtripTime, reply.Status, target);
            return PingTelemetry.Evaluate(reply.RoundtripTime, reply.Status);
        }
        catch (OperationCanceledException)
        {
            ProcessProbeResult(-1, IPStatus.Unknown, target);
            return PingTelemetry.CreateOffline();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ping error to {target}: {ex.Message}");
            ProcessProbeResult(-1, IPStatus.DestinationUnreachable, target);
            return PingTelemetry.Evaluate(-1, IPStatus.DestinationUnreachable);
        }
    }

    public void ProcessProbeResult(long rttMs, IPStatus status, string target)
    {
        NetworkHealthTelemetry health;
        lock (_stateLock)
        {
            bool isSuccess = status == IPStatus.Success && rttMs >= 0;

            // 1. Loss sliding window
            _lossHistory.Enqueue(isSuccess);
            while (_lossHistory.Count > LossWindowCapacity)
            {
                _lossHistory.Dequeue();
            }

            // 2. Success telemetry & RFC 3550 Jitter
            if (isSuccess)
            {
                _rttHistory.Enqueue(rttMs);
                while (_rttHistory.Count > RttHistoryCapacity)
                {
                    _rttHistory.Dequeue();
                }

                if (_lastRtt.HasValue)
                {
                    double diff = Math.Abs(rttMs - _lastRtt.Value);
                    _smoothedJitter = _smoothedJitter == 0
                        ? diff
                        : _smoothedJitter + ((diff - _smoothedJitter) / 16.0);
                }
                _lastRtt = rttMs;

                // Calibrate resting baseline latency during idle traffic (<= 1.0 MB/s total)
                if ((_currentDlBytes + _currentUlBytes) <= 1024 * 1024)
                {
                    _baselineRttHistory.Enqueue(rttMs);
                    while (_baselineRttHistory.Count > BaselineHistoryCapacity)
                    {
                        _baselineRttHistory.Dequeue();
                    }
                }
            }

            // 3. Bufferbloat Early Warning
            double baseline = _baselineRttHistory.Count > 0 ? _baselineRttHistory.Average() : 0;
            bool isBufferbloat = false;
            long bufferbloatDelta = 0;

            if (isSuccess && baseline > 0 && (_currentDlBytes + _currentUlBytes) >= 2.5 * 1024 * 1024)
            {
                if (rttMs >= Math.Max(65, baseline * 3.0) || (rttMs - baseline >= 50))
                {
                    isBufferbloat = true;
                    bufferbloatDelta = rttMs - (long)baseline;
                }
            }

            health = NetworkHealthTelemetry.Evaluate(
                rttMs,
                status,
                _rttHistory.ToList(),
                _lossHistory.ToList(),
                _smoothedJitter,
                isBufferbloat,
                bufferbloatDelta,
                target);

            CurrentHealth = health;
        }

        HealthUpdated?.Invoke(health);
    }

    private async Task ProbeLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await ProbeOnceAsync(token);
                if (!token.IsCancellationRequested)
                {
                    LatencyUpdated?.Invoke(result);
                }

                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ping probe loop: {ex.Message}");
                if (!token.IsCancellationRequested)
                {
                    ProcessProbeResult(-1, IPStatus.Unknown, ResolveTargetHost());
                    LatencyUpdated?.Invoke(PingTelemetry.CreateOffline());
                    await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), token);
                }
            }
        }
    }

    private string ResolveTargetHost()
    {
        if (!string.Equals(_targetHost, "GATEWAY", StringComparison.OrdinalIgnoreCase))
        {
            return _targetHost;
        }

        try
        {
            var gateway = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .SelectMany(nic => nic.GetIPProperties().GatewayAddresses)
                .Select(g => g.Address.ToString())
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(gateway))
            {
                return gateway;
            }
        }
        catch
        {
            // Fall back to public target
        }

        return "1.1.1.1";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}
