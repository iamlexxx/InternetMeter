using System.Net.NetworkInformation;

namespace InternetMeter.Models;

public enum LatencyTier
{
    Optimal,    // < 40 ms (Emerald)
    Moderate,   // 40 - 120 ms (Amber)
    Critical,   // > 120 ms or Timeout/Loss (Rose)
    Offline     // Disabled or network down (Slate)
}

public record PingTelemetry(
    long RttMs,
    IPStatus Status,
    LatencyTier Tier,
    string DisplayText,
    string ColorHex
)
{
    public static PingTelemetry Evaluate(long rttMs, IPStatus status)
    {
        if (status == IPStatus.Success)
        {
            if (rttMs < 40)
            {
                return new PingTelemetry(rttMs, status, LatencyTier.Optimal, $"{rttMs} ms", "#10B981");
            }
            if (rttMs <= 120)
            {
                return new PingTelemetry(rttMs, status, LatencyTier.Moderate, $"{rttMs} ms", "#F59E0B");
            }
            return new PingTelemetry(rttMs, status, LatencyTier.Critical, $"{rttMs} ms", "#F43F5E");
        }

        if (status == IPStatus.TimedOut)
        {
            return new PingTelemetry(-1, status, LatencyTier.Critical, "Loss", "#F43F5E");
        }

        if (status is IPStatus.DestinationHostUnreachable or IPStatus.DestinationNetworkUnreachable or IPStatus.DestinationUnreachable)
        {
            return new PingTelemetry(-1, status, LatencyTier.Critical, "Unreach", "#F43F5E");
        }

        return new PingTelemetry(-1, status, LatencyTier.Critical, "Drop", "#F43F5E");
    }

    public static PingTelemetry CreateOffline() =>
        new(-1, IPStatus.Unknown, LatencyTier.Offline, "--", "#64748B");
}

