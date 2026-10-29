using System.IO;
using System.Text.Json;
using System.Windows.Media;
using InternetMeter.Models;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using Xunit;

namespace InternetMeter.Tests;

public class ProcessNetworkServiceTests
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

    private class FakeProcessNetworkService : IProcessNetworkService
    {
        public List<ProcessTrafficInfo> MockResults { get; set; } = new();
        public ITrafficJournalService Journal { get; set; } = new TrafficJournalService(Path.Combine(Path.GetTempPath(), $"journal_{Guid.NewGuid():N}.json"));
        public int UpdateTrafficCallCount { get; private set; }
        public long LastSystemDeltaDl { get; private set; }
        public long LastSystemDeltaUl { get; private set; }

        public void UpdateTraffic(
            long systemDeltaDlBytes,
            long systemDeltaUlBytes,
            double systemDlRate,
            double systemUlRate)
        {
            UpdateTrafficCallCount++;
            LastSystemDeltaDl = systemDeltaDlBytes;
            LastSystemDeltaUl = systemDeltaUlBytes;
        }

        public IReadOnlyList<ProcessTrafficInfo> GetTopProcesses(
            int maxCount,
            SpeedUnit unit,
            bool isTotalMode,
            double totalSystemDownloadBytes = 0,
            double totalSystemUploadBytes = 0)
        {
            return MockResults.Take(maxCount).ToList();
        }

        public void Reset()
        {
            MockResults.Clear();
        }
    }

    [Fact]
    public void AppSettings_SerializesAndDeserializes_ProcessInspectorProperties()
    {
        var settings = new AppSettings
        {
            ShowProcessInspector = true,
            IsProcessInspectorExpanded = true,
            MaxProcessInspectorCount = 5
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(restored);
        Assert.True(restored.ShowProcessInspector);
        Assert.True(restored.IsProcessInspectorExpanded);
        Assert.Equal(5, restored.MaxProcessInspectorCount);
    }

    [Fact]
    public void MainViewModel_ToggleProcessInspector_UpdatesStateAndGlyph()
    {
        var fakeNet = new FakeNetworkService();
        var fakeSettings = new FakeSettingsService();
        var fakePing = new FakePingService();
        var fakeAuto = new FakeAutoStartService();
        var fakeProc = new FakeProcessNetworkService();

        var vm = new MainViewModel(fakeNet, fakeSettings, fakePing, fakeAuto, fakeProc);

        Assert.False(vm.IsProcessInspectorExpanded);
        Assert.Equal("▾ Apps", vm.ProcessInspectorToggleGlyph);

        // Toggle via command
        vm.ToggleProcessInspectorCommand.Execute(null);

        Assert.True(vm.IsProcessInspectorExpanded);
        Assert.Equal("▴ Apps", vm.ProcessInspectorToggleGlyph);
        Assert.True(fakeSettings.CurrentSettings.IsProcessInspectorExpanded);

        // Toggle off
        vm.ToggleProcessInspectorCommand.Execute(null);

        Assert.False(vm.IsProcessInspectorExpanded);
        Assert.Equal("▾ Apps", vm.ProcessInspectorToggleGlyph);
        Assert.False(fakeSettings.CurrentSettings.IsProcessInspectorExpanded);
    }

    [Fact]
    public void MainViewModel_SetOpacity_ImmediatelyUpdatesCurrentOpacity_AndClearsGhostMode()
    {
        var fakeNet = new FakeNetworkService();
        var fakeSettings = new FakeSettingsService();
        var fakePing = new FakePingService();
        var fakeAuto = new FakeAutoStartService();
        var fakeProc = new FakeProcessNetworkService();

        var vm = new MainViewModel(fakeNet, fakeSettings, fakePing, fakeAuto, fakeProc);
        vm.IsGhostMode = true;
        Assert.Equal(vm.GhostOpacity, vm.CurrentOpacity);

        // Act: User selects 60% opacity
        bool notifiedCurrent = false;
        bool notifiedWindow = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.CurrentOpacity)) notifiedCurrent = true;
            if (e.PropertyName == nameof(vm.WindowOpacity)) notifiedWindow = true;
        };

        vm.SetOpacity(0.60);

        // Assert
        Assert.False(vm.IsGhostMode);
        Assert.Equal(0.60, vm.WindowOpacity);
        Assert.Equal(0.60, vm.CurrentOpacity);
        Assert.True(notifiedCurrent);
        Assert.True(notifiedWindow);
        Assert.Equal(0.60, fakeSettings.CurrentSettings.Opacity);
    }

    [Fact]
    public void MainViewModel_InspectorModes_ToggleBetweenLiveAndTotal()
    {
        var fakeNet = new FakeNetworkService();
        var fakeSettings = new FakeSettingsService();
        var fakePing = new FakePingService();
        var fakeAuto = new FakeAutoStartService();
        var fakeProc = new FakeProcessNetworkService();

        var vm = new MainViewModel(fakeNet, fakeSettings, fakePing, fakeAuto, fakeProc);

        Assert.False(vm.IsProcessInspectorTotalMode);

        vm.SetInspectorTotalModeCommand.Execute(null);
        Assert.True(vm.IsProcessInspectorTotalMode);
        Assert.True(fakeSettings.CurrentSettings.IsProcessInspectorTotalMode);

        vm.SetInspectorLiveModeCommand.Execute(null);
        Assert.False(vm.IsProcessInspectorTotalMode);
        Assert.False(fakeSettings.CurrentSettings.IsProcessInspectorTotalMode);
    }

    [Fact]
    public void MainViewModel_OpenUsageReportCommand_FiresRequestEvent()
    {
        var fakeNet = new FakeNetworkService();
        var fakeSettings = new FakeSettingsService();
        var fakePing = new FakePingService();
        var fakeAuto = new FakeAutoStartService();
        var fakeProc = new FakeProcessNetworkService();

        var vm = new MainViewModel(fakeNet, fakeSettings, fakePing, fakeAuto, fakeProc);
        bool fired = false;
        vm.RequestOpenUsageReport += () => fired = true;

        vm.OpenUsageReportCommand.Execute(null);

        Assert.True(fired);
    }

    [Fact]
    public void ProcessTrafficInfo_PropertiesAndFormatting()
    {
        var item = new ProcessTrafficInfo
        {
            ProcessId = 1234,
            ProcessName = "chrome.exe",
            DownloadSpeedBytes = 1024 * 1024 * 2.5,
            UploadSpeedBytes = 1024 * 512,
            FormattedDownloadSpeed = UnitFormatter.FormatSpeed(1024 * 1024 * 2.5, SpeedUnit.MBps),
            FormattedUploadSpeed = UnitFormatter.FormatSpeed(1024 * 512, SpeedUnit.MBps),
            PercentageOfTotal = 75.5
        };

        Assert.Equal(1234, item.ProcessId);
        Assert.Equal("chrome.exe", item.ProcessName);
        Assert.Contains("2.50", item.FormattedDownloadSpeed);
        Assert.Contains("0.50", item.FormattedUploadSpeed);
        Assert.Equal(75.5, item.PercentageOfTotal);
    }

    [Fact]
    public void FakeProcessNetworkService_LimitsMaxCount()
    {
        var service = new FakeProcessNetworkService
        {
            MockResults = new List<ProcessTrafficInfo>
            {
                new() { ProcessId = 1, ProcessName = "app1.exe" },
                new() { ProcessId = 2, ProcessName = "app2.exe" },
                new() { ProcessId = 3, ProcessName = "app3.exe" },
                new() { ProcessId = 4, ProcessName = "app4.exe" },
                new() { ProcessId = 5, ProcessName = "app5.exe" }
            }
        };

        var top3 = service.GetTopProcesses(3, SpeedUnit.Mbps, false);
        Assert.Equal(3, top3.Count);
        Assert.Equal("app1.exe", top3[0].ProcessName);
        Assert.Equal("app3.exe", top3[2].ProcessName);
    }

    [Fact]
    public void MainViewModel_ContinuouslyUpdatesProcessTraffic_EvenWhenDrawerCollapsed()
    {
        var fakeNet = new FakeNetworkService();
        var fakeSettings = new FakeSettingsService();
        var fakePing = new FakePingService();
        var fakeAuto = new FakeAutoStartService();
        var fakeProc = new FakeProcessNetworkService();

        var vm = new MainViewModel(fakeNet, fakeSettings, fakePing, fakeAuto, fakeProc);
        
        // Assert drawer is collapsed initially
        Assert.False(vm.IsProcessInspectorExpanded);

        // Act: trigger private OnTimerTick via reflection
        var method = typeof(MainViewModel).GetMethod("OnTimerTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(vm, null);

        // Assert: UpdateTraffic was invoked with accurate NIC deltas
        Assert.True(fakeProc.UpdateTrafficCallCount > 0);
        Assert.Equal(1024 * 1024, fakeProc.LastSystemDeltaDl);
        Assert.Equal(512 * 1024, fakeProc.LastSystemDeltaUl);
    }
}

