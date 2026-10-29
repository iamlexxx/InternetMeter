using System.Net.NetworkInformation;
using InternetMeter.Models;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using Xunit;

namespace InternetMeter.Tests;

public class NetworkHealthTests
{
    private class FakeNetworkService : INetworkService
    {
        public IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters() => new List<NetworkAdapterInfo>();
        public (double downloadSpeedBytes, double uploadSpeedBytes, bool isConnected) SampleTraffic(string adapterId) => (1024 * 1024, 512 * 1024, true);
        public (long totalDownloadBytes, long totalUploadBytes) GetSessionTotals() => (0, 0);
        public void ResetSessionTotals() { }
        public (long deltaDownloadBytes, long deltaUploadBytes) GetLastSampleDeltas() => (1024 * 1024, 512 * 1024);
    }

    private class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; set; } = new();
        public AppSettings Load() => CurrentSettings;
        public void Save(AppSettings settings) => CurrentSettings = settings;
    }

    private class FakeAutoStartService : IAutoStartService
    {
        public bool IsAutoStartEnabled() => false;
        public void SetAutoStart(bool enable, bool startMinimized = true) { }
        public void SyncWithCurrentPath(bool startMinimized = true) { }
    }

    [Fact]
    public void Jitter_CalculatesRfc3550Smoothing_Accurately()
    {
        var pingService = new PingService();

        // 1st probe: 20 ms -> initial state
        pingService.ProcessProbeResult(20, IPStatus.Success, "1.1.1.1");
        Assert.NotNull(pingService.CurrentHealth);
        Assert.Equal(0, pingService.CurrentHealth.JitterMs);

        // 2nd probe: 24 ms -> diff = 4.0, smoothedJitter = 4.0
        pingService.ProcessProbeResult(24, IPStatus.Success, "1.1.1.1");
        Assert.NotNull(pingService.CurrentHealth);
        Assert.Equal(4.0, pingService.CurrentHealth.JitterMs, 3);

        // 3rd probe: 19 ms -> diff = |19 - 24| = 5.0. New Jitter = 4.0 + (5.0 - 4.0)/16 = 4.0625
        pingService.ProcessProbeResult(19, IPStatus.Success, "1.1.1.1");
        Assert.NotNull(pingService.CurrentHealth);
        Assert.Equal(4.0625, pingService.CurrentHealth.JitterMs, 4);
    }

    [Fact]
    public void PacketLoss_CalculatesWindowPercentage()
    {
        var pingService = new PingService();

        // Feed 19 successes and 1 timeout (1/20 = 5.0% loss)
        for (int i = 0; i < 19; i++)
        {
            pingService.ProcessProbeResult(20, IPStatus.Success, "1.1.1.1");
        }
        pingService.ProcessProbeResult(-1, IPStatus.TimedOut, "1.1.1.1");

        Assert.NotNull(pingService.CurrentHealth);
        Assert.Equal(5.0, pingService.CurrentHealth.PacketLossPercent);
        Assert.Equal(1, pingService.CurrentHealth.LossCount);
        Assert.Equal(20, pingService.CurrentHealth.TotalProbesInWindow);
        Assert.Equal("#F43F5E", pingService.CurrentHealth.PacketLossColorHex); // Rose for >= 5%
    }

    [Fact]
    public void Bufferbloat_DetectsSpikeUnderHighThroughput()
    {
        var pingService = new PingService();

        // 1. Calibrate baseline with idle traffic (500 KB/s total) at 16 ms
        pingService.UpdateThroughput(300 * 1024, 200 * 1024);
        for (int i = 0; i < 10; i++)
        {
            pingService.ProcessProbeResult(16, IPStatus.Success, "1.1.1.1");
        }

        Assert.NotNull(pingService.CurrentHealth);
        Assert.False(pingService.CurrentHealth.IsBufferbloat);

        // 2. Saturate throughput (5 MB/s total) and spike latency to 85 ms
        pingService.UpdateThroughput(3 * 1024 * 1024, 2 * 1024 * 1024);
        pingService.ProcessProbeResult(85, IPStatus.Success, "1.1.1.1");

        Assert.NotNull(pingService.CurrentHealth);
        Assert.True(pingService.CurrentHealth.IsBufferbloat);
        Assert.Equal(69, pingService.CurrentHealth.BufferbloatDeltaMs); // 85 - 16 = 69 ms penalty
        Assert.Contains("⚠", pingService.CurrentHealth.DisplayBadgeText);
    }

    [Fact]
    public void GradeScorer_AssignsAppropriateTier()
    {
        // 1. Esports Ready (A+)
        var healthAPlus = NetworkHealthTelemetry.Evaluate(
            currentRtt: 18,
            status: IPStatus.Success,
            rttHistory: new long[] { 16, 18, 20 },
            lossHistory: Enumerable.Repeat(true, 20).ToList(),
            smoothedJitter: 1.5,
            isBufferbloat: false,
            bufferbloatDelta: 0,
            targetHost: "1.1.1.1");

        Assert.Equal(NetworkGrade.APlus, healthAPlus.Grade);
        Assert.Equal("A+", healthAPlus.GradeLabel);
        Assert.Equal("#10B981", healthAPlus.GradeColorHex);

        // 2. Gaming Ready (A)
        var healthA = NetworkHealthTelemetry.Evaluate(
            currentRtt: 48,
            status: IPStatus.Success,
            rttHistory: new long[] { 45, 50, 48 },
            lossHistory: Enumerable.Repeat(true, 20).ToList(),
            smoothedJitter: 4.2,
            isBufferbloat: false,
            bufferbloatDelta: 0,
            targetHost: "1.1.1.1");

        Assert.Equal(NetworkGrade.A, healthA.Grade);
        Assert.Equal("A", healthA.GradeLabel);

        // 3. VoIP Ready (B)
        var healthB = NetworkHealthTelemetry.Evaluate(
            currentRtt: 75,
            status: IPStatus.Success,
            rttHistory: new long[] { 70, 80, 75 },
            lossHistory: Enumerable.Repeat(true, 20).ToList(),
            smoothedJitter: 8.5,
            isBufferbloat: false,
            bufferbloatDelta: 0,
            targetHost: "1.1.1.1");

        Assert.Equal(NetworkGrade.B, healthB.Grade);
        Assert.Equal("B", healthB.GradeLabel);

        // 4. Critical Loss (D)
        var lossWithDrops = Enumerable.Repeat(true, 18).Concat(new bool[] { false, false }).ToList();
        var healthD = NetworkHealthTelemetry.Evaluate(
            currentRtt: 160,
            status: IPStatus.Success,
            rttHistory: new long[] { 150, 170, 160 },
            lossHistory: lossWithDrops,
            smoothedJitter: 22.0,
            isBufferbloat: true,
            bufferbloatDelta: 100,
            targetHost: "1.1.1.1");

        Assert.Equal(NetworkGrade.D, healthD.Grade);
        Assert.Equal("D", healthD.GradeLabel);

        // 5. Total Outage (F)
        var healthF = NetworkHealthTelemetry.CreateOffline("1.1.1.1");
        Assert.Equal(NetworkGrade.F, healthF.Grade);
        Assert.Equal("F", healthF.GradeLabel);
    }

    [Fact]
    public void MainViewModel_NetworkHealth_UpdatesAllReactiveProperties()
    {
        var pingService = new PingService();
        var settingsService = new FakeSettingsService();
        var vm = new MainViewModel(
            new FakeNetworkService(),
            settingsService,
            pingService,
            new FakeAutoStartService());

        // Process a probe result through ping service
        pingService.ProcessProbeResult(22, IPStatus.Success, "1.1.1.1");

        Assert.NotNull(vm.CurrentHealth);
        Assert.Equal("22 ms", vm.LatencyText);
        Assert.Equal("A+", vm.NetworkQualityGradeText);
        Assert.Equal("1.1.1.1", vm.TargetHostText);
        Assert.False(vm.IsBufferbloatActive);
    }
}

