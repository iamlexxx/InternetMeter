using System.Net.NetworkInformation;

namespace InternetMeter.Models;

public enum NetworkGrade
{
    APlus,
    A,
    B,
    C,
    D,
    F
}

public record NetworkHealthTelemetry(
    long CurrentRttMs,
    long MinRttMs,
    long MaxRttMs,
    long AvgRttMs,
    double JitterMs,
    string JitterLabel,
    string JitterColorHex,
    double PacketLossPercent,
    int LossCount,
    int TotalProbesInWindow,
    string PacketLossColorHex,
    bool IsBufferbloat,
    long BufferbloatDeltaMs,
    NetworkGrade Grade,
    string GradeLabel,
    string GradeDescription,
    string GradeColorHex,
    string DisplayBadgeText,
    string DisplayBadgeColorHex,
    string TargetHost,
    IPStatus Status)
{
    public static NetworkHealthTelemetry CreateOffline(string targetHost = "1.1.1.1") =>
        new(
            CurrentRttMs: -1,
            MinRttMs: 0,
            MaxRttMs: 0,
            AvgRttMs: 0,
            JitterMs: 0,
            JitterLabel: "Offline",
            JitterColorHex: "#64748B",
            PacketLossPercent: 100.0,
            LossCount: 20,
            TotalProbesInWindow: 20,
            PacketLossColorHex: "#64748B",
            IsBufferbloat: false,
            BufferbloatDeltaMs: 0,
            Grade: NetworkGrade.F,
            GradeLabel: "F",
            GradeDescription: "Connection Offline",
            GradeColorHex: "#64748B",
            DisplayBadgeText: "--",
            DisplayBadgeColorHex: "#64748B",
            TargetHost: targetHost,
            Status: IPStatus.Unknown);

    public static NetworkHealthTelemetry Evaluate(
        long currentRtt,
        IPStatus status,
        IReadOnlyCollection<long> rttHistory,
        IReadOnlyCollection<bool> lossHistory,
        double smoothedJitter,
        bool isBufferbloat,
        long bufferbloatDelta,
        string targetHost)
    {
        // 1. Packet Loss calculation over sliding window (default 20 probes)
        int totalProbes = lossHistory.Count > 0 ? lossHistory.Count : 1;
        int failedProbes = lossHistory.Count(success => !success);
        double lossPercent = ((double)failedProbes / totalProbes) * 100.0;

        string lossColor = lossPercent switch
        {
            <= 0.0 => "#10B981", // Emerald
            <= 3.0 => "#F59E0B", // Amber
            _ => "#F43F5E"        // Rose
        };

        // 2. RTT statistics
        var validRtts = rttHistory.Where(r => r > 0).ToList();
        long minRtt = validRtts.Count > 0 ? validRtts.Min() : (currentRtt > 0 ? currentRtt : 0);
        long maxRtt = validRtts.Count > 0 ? validRtts.Max() : (currentRtt > 0 ? currentRtt : 0);
        long avgRtt = validRtts.Count > 0 ? (long)Math.Round(validRtts.Average()) : (currentRtt > 0 ? currentRtt : 0);

        // 3. Jitter classification
        string jitterLabel;
        string jitterColor;
        if (smoothedJitter <= 2.5)
        {
            jitterLabel = "Smooth (Optimal)";
            jitterColor = "#10B981";
        }
        else if (smoothedJitter <= 8.0)
        {
            jitterLabel = "Moderate (Acceptable)";
            jitterColor = "#F59E0B";
        }
        else
        {
            jitterLabel = "Spiking (Noticeable)";
            jitterColor = "#F43F5E";
        }

        // 4. Overall Quality Grade
        NetworkGrade grade;
        string gradeLabel;
        string gradeDesc;
        string gradeColor;

        if (status != IPStatus.Success && lossPercent >= 80.0)
        {
            grade = NetworkGrade.F;
            gradeLabel = "F";
            gradeDesc = "Severe Outage / No Link";
            gradeColor = "#EF4444";
        }
        else if (currentRtt < 35 && smoothedJitter <= 2.5 && lossPercent <= 0.0 && !isBufferbloat)
        {
            grade = NetworkGrade.APlus;
            gradeLabel = "A+";
            gradeDesc = "Esports & Competitive Ready";
            gradeColor = "#10B981";
        }
        else if (currentRtt < 60 && smoothedJitter <= 5.0 && lossPercent <= 1.0 && !isBufferbloat)
        {
            grade = NetworkGrade.A;
            gradeLabel = "A";
            gradeDesc = "Gaming & 4K Streaming Ready";
            gradeColor = "#38BDF8";
        }
        else if (currentRtt < 95 && smoothedJitter <= 10.0 && lossPercent <= 2.5)
        {
            grade = NetworkGrade.B;
            gradeLabel = "B";
            gradeDesc = "Good for VoIP & Web Browsing";
            gradeColor = "#818CF8";
        }
        else if (currentRtt < 150 && smoothedJitter <= 20.0 && lossPercent <= 5.0)
        {
            grade = NetworkGrade.C;
            gradeLabel = "C";
            gradeDesc = "Noticeable Latency / Audio Lag";
            gradeColor = "#F59E0B";
        }
        else
        {
            grade = NetworkGrade.D;
            gradeLabel = "D";
            gradeDesc = "High Packet Loss / Frequent Stutter";
            gradeColor = "#F43F5E";
        }

        // 5. Badge text & color
        string badgeText;
        string badgeColor;
        if (status == IPStatus.Success)
        {
            if (isBufferbloat)
            {
                badgeText = $"⚠ {currentRtt} ms";
                badgeColor = "#F59E0B"; // Amber warning
            }
            else
            {
                badgeText = $"{currentRtt} ms";
                badgeColor = currentRtt switch
                {
                    < 40 => "#10B981",
                    <= 120 => "#F59E0B",
                    _ => "#F43F5E"
                };
            }
        }
        else
        {
            badgeText = status == IPStatus.TimedOut ? "Loss" : "Drop";
            badgeColor = "#F43F5E";
        }

        return new NetworkHealthTelemetry(
            CurrentRttMs: currentRtt,
            MinRttMs: minRtt,
            MaxRttMs: maxRtt,
            AvgRttMs: avgRtt,
            JitterMs: smoothedJitter,
            JitterLabel: jitterLabel,
            JitterColorHex: jitterColor,
            PacketLossPercent: lossPercent,
            LossCount: failedProbes,
            TotalProbesInWindow: totalProbes,
            PacketLossColorHex: lossColor,
            IsBufferbloat: isBufferbloat,
            BufferbloatDeltaMs: bufferbloatDelta,
            Grade: grade,
            GradeLabel: gradeLabel,
            GradeDescription: gradeDesc,
            GradeColorHex: gradeColor,
            DisplayBadgeText: badgeText,
            DisplayBadgeColorHex: badgeColor,
            TargetHost: targetHost,
            Status: status);
    }
}

