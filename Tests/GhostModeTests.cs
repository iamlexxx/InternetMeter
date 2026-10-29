using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using System.Text.Json;
using Xunit;

namespace InternetMeter.Tests;

public class GhostModeTests
{
    private class FakeNetworkService : INetworkService
    {
        public IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters() => new List<NetworkAdapterInfo>();
        public (double downloadSpeedBytes, double uploadSpeedBytes, bool isConnected) SampleTraffic(string adapterId) => (0, 0, true);
        public (long totalDownloadBytes, long totalUploadBytes) GetSessionTotals() => (0, 0);
        public void ResetSessionTotals() { }
        public (long deltaDownloadBytes, long deltaUploadBytes) GetLastSampleDeltas() => (0, 0);
    }

    private class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; set; } = new();
        public AppSettings Load() => CurrentSettings;
        public void Save(AppSettings settings) => CurrentSettings = settings;
    }

    private class FakePingService : IPingService
    {
        public bool IsRunning => false;
        public NetworkHealthTelemetry? CurrentHealth => null;
#pragma warning disable CS0067
        public event Action<PingTelemetry>? LatencyUpdated;
        public event Action<NetworkHealthTelemetry>? HealthUpdated;
#pragma warning restore CS0067
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

    [Fact]
    public void ExtendedStyleBitmask_AddsAndRemoves_WS_EX_TRANSPARENT()
    {
        const int initialStyle = 0x00080000; // e.g. WS_EX_LAYERED
        const int transparent = NativeMethods.WS_EX_TRANSPARENT; // 0x00000020

        // Enable ghost mode
        int ghostStyle = initialStyle | transparent;
        Assert.True((ghostStyle & transparent) == transparent);
        Assert.True((ghostStyle & initialStyle) == initialStyle);

        // Disable ghost mode
        int normalStyle = ghostStyle & ~transparent;
        Assert.False((normalStyle & transparent) == transparent);
        Assert.Equal(initialStyle, normalStyle);
    }

    [Fact]
    public void AppSettings_SerializesAndDeserializes_GhostModeProperties()
    {
        var settings = new AppSettings
        {
            IsGhostMode = true,
            GhostOpacity = 0.55,
            GhostHotkeyText = "Ctrl+Shift+G"
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(restored);
        Assert.True(restored.IsGhostMode);
        Assert.Equal(0.55, restored.GhostOpacity);
        Assert.Equal("Ctrl+Shift+G", restored.GhostHotkeyText);
    }

    [Fact]
    public void MainViewModel_ToggleGhostMode_FiresEventAndPersists()
    {
        var fakeNet = new FakeNetworkService();
        var fakeSettings = new FakeSettingsService();
        var fakePing = new FakePingService();
        var fakeAuto = new FakeAutoStartService();

        var vm = new MainViewModel(fakeNet, fakeSettings, fakePing, fakeAuto);

        bool eventFired = false;
        bool lastEventValue = false;
        vm.GhostModeChanged += val =>
        {
            eventFired = true;
            lastEventValue = val;
        };

        Assert.False(vm.IsGhostMode);

        // Toggle on
        vm.IsGhostMode = true;
        Assert.True(eventFired);
        Assert.True(lastEventValue);
        Assert.True(fakeSettings.CurrentSettings.IsGhostMode);

        // Toggle via command
        eventFired = false;
        vm.ToggleGhostModeCommand.Execute(null);
        Assert.True(eventFired);
        Assert.False(lastEventValue);
        Assert.False(vm.IsGhostMode);
        Assert.False(fakeSettings.CurrentSettings.IsGhostMode);
    }

    [Fact]
    public void MainViewModel_CurrentOpacity_ReflectsWindowOpacityAndGhostMode()
    {
        var fakeNet = new FakeNetworkService();
        var fakeSettings = new FakeSettingsService();
        var fakePing = new FakePingService();
        var fakeAuto = new FakeAutoStartService();

        var vm = new MainViewModel(fakeNet, fakeSettings, fakePing, fakeAuto);

        vm.SetOpacity(0.60);
        Assert.Equal(0.60, vm.WindowOpacity);
        Assert.Equal(0.60, vm.CurrentOpacity);

        // Turn on ghost mode
        vm.IsGhostMode = true;
        Assert.Equal(vm.GhostOpacity, vm.CurrentOpacity);

        // Change opacity while in ghost mode: disengages ghost mode and applies opacity immediately
        vm.SetOpacity(0.80);
        Assert.False(vm.IsGhostMode);
        Assert.Equal(0.80, vm.WindowOpacity);
        Assert.Equal(0.80, vm.CurrentOpacity);
    }
}
