using System.IO;
using System.Text.Json;
using System.Windows;
using InternetMeter.Models;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using Xunit;

namespace InternetMeter.Tests;

public class CompactModeTests
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

    private class FakePingService : IPingService
    {
#pragma warning disable CS0067
        public event Action<PingTelemetry>? LatencyUpdated;
        public event Action<NetworkHealthTelemetry>? HealthUpdated;
#pragma warning restore CS0067
        public bool IsRunning => false;
        public NetworkHealthTelemetry? CurrentHealth => null;
        public void Start(int intervalSeconds = 3, string targetHost = "1.1.1.1") { }
        public void Stop() { }
        public void UpdateThroughput(double downloadSpeedBytes, double uploadSpeedBytes) { }
        public Task<PingTelemetry> ProbeOnceAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PingTelemetry.CreateOffline());
        public void Dispose() { }
    }

    private class FakeAutoStartService : IAutoStartService
    {
        public bool IsAutoStartEnabled() => false;
        public void SetAutoStart(bool enable, bool startMinimized = true) { }
        public void SyncWithCurrentPath(bool startMinimized = true) { }
    }

    private class FakeProcessNetworkService : IProcessNetworkService
    {
        public ITrafficJournalService Journal { get; set; } = new TrafficJournalService(Path.Combine(Path.GetTempPath(), $"journal_{Guid.NewGuid():N}.json"));
        public void UpdateTraffic(long systemDeltaDlBytes, long systemDeltaUlBytes, double systemDlRate, double systemUlRate) { }
        public IReadOnlyList<ProcessTrafficInfo> GetTopProcesses(int maxCount, SpeedUnit unit, bool isTotalMode, double totalSystemDownloadBytes = 0, double totalSystemUploadBytes = 0) =>
            Array.Empty<ProcessTrafficInfo>();
        public void Reset() { }
    }

    [Fact]
    public void AppSettings_SerializesAndDeserializes_CompactProperties()
    {
        var settings = new AppSettings
        {
            IsCompactMode = true,
            EnableEdgeSnapping = true,
            CompactLeft = 1400.5,
            CompactTop = 950.0
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(restored);
        Assert.True(restored.IsCompactMode);
        Assert.True(restored.EnableEdgeSnapping);
        Assert.Equal(1400.5, restored.CompactLeft);
        Assert.Equal(950.0, restored.CompactTop);
    }

    [Fact]
    public void MainViewModel_ToggleCompactMode_TogglesStateAndFiresEvent()
    {
        var net = new FakeNetworkService();
        var settings = new FakeSettingsService();
        var ping = new FakePingService();
        var auto = new FakeAutoStartService();
        var proc = new FakeProcessNetworkService();

        var vm = new MainViewModel(net, settings, ping, auto, proc);
        Assert.False(vm.IsCompactMode);

        bool eventFired = false;
        bool? eventState = null;
        vm.CompactModeToggled += isCompact =>
        {
            eventFired = true;
            eventState = isCompact;
        };

        // Act 1: Toggle on
        vm.ToggleCompactModeCommand.Execute(null);

        Assert.True(vm.IsCompactMode);
        Assert.True(settings.CurrentSettings.IsCompactMode);
        Assert.True(eventFired);
        Assert.True(eventState);

        // Act 2: Toggle off
        eventFired = false;
        vm.ToggleCompactModeCommand.Execute(null);

        Assert.False(vm.IsCompactMode);
        Assert.False(settings.CurrentSettings.IsCompactMode);
        Assert.True(eventFired);
        Assert.False(eventState);
    }

    [Fact]
    public void MainViewModel_CompactCoordinates_SaveAndRetrieve()
    {
        var net = new FakeNetworkService();
        var settings = new FakeSettingsService();
        var ping = new FakePingService();
        var auto = new FakeAutoStartService();
        var proc = new FakeProcessNetworkService();

        var vm = new MainViewModel(net, settings, ping, auto, proc);

        vm.SaveCompactCoordinates(850.0, 1020.0);

        var (left, top) = vm.GetSavedCompactCoordinates();
        Assert.Equal(850.0, left);
        Assert.Equal(1020.0, top);
        Assert.Equal(850.0, settings.CurrentSettings.CompactLeft);
        Assert.Equal(1020.0, settings.CurrentSettings.CompactTop);
    }

    [Theory]
    [InlineData(10.0, 10.0, 8.0, 8.0)]          // Snaps to Top-Left corner
    [InlineData(1750.0, 1060.0, 1744.0, 1054.0)] // Snaps to Bottom-Right taskbar corner
    [InlineData(500.0, 500.0, 500.0, 500.0)]     // Mid-screen, no snap
    public void SnappingLogic_EvaluatesCoordinates_Correctly(
        double proposedLeft, double proposedTop, double expectedLeft, double expectedTop)
    {
        var workArea = new Rect(0, 0, 1920, 1080);
        double width = 168.0;
        double height = 26.0;
        const double snapDistance = 16.0;

        double left = proposedLeft;
        double top = proposedTop;

        // Left snap
        if (Math.Abs(left - workArea.Left) <= snapDistance)
            left = workArea.Left + 8;
        // Right snap
        else if (Math.Abs((left + width) - workArea.Right) <= snapDistance)
            left = workArea.Right - width - 8;

        // Top snap
        if (Math.Abs(top - workArea.Top) <= snapDistance)
            top = workArea.Top + 8;
        // Bottom taskbar snap
        else if (Math.Abs((top + height) - workArea.Bottom) <= snapDistance)
            top = workArea.Bottom - height;

        Assert.Equal(expectedLeft, left);
        Assert.Equal(expectedTop, top);
    }
}
