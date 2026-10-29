using System.IO;
using System.Text.Json;
using System.Windows.Media;
using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using Xunit;

namespace InternetMeter.Tests;

public class ThemeCustomizerTests
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
        public ITrafficJournalService Journal { get; set; } = new TrafficJournalService(Path.Combine(Path.GetTempPath(), $"journal_{Guid.NewGuid():N}.json"));
        public void UpdateTraffic(long systemDeltaDlBytes, long systemDeltaUlBytes, double systemDlRate, double systemUlRate) { }
        public IReadOnlyList<ProcessTrafficInfo> GetTopProcesses(int maxCount, SpeedUnit unit, bool isTotalMode, double totalSystemDownloadBytes = 0, double totalSystemUploadBytes = 0) =>
            Array.Empty<ProcessTrafficInfo>();
        public void Reset() { }
    }

    [Fact]
    public void ThemeTokens_EmeraldSky_ReturnsCorrectPaletteColorsAndFrozenBrushes()
    {
        var tokens = ThemeManager.GetTokens(ThemePalette.EmeraldSky);

        Assert.Equal("Emerald & Sky", tokens.Name);
        Assert.Equal("#10B981", tokens.DownloadColorHex);
        Assert.Equal("#38BDF8", tokens.UploadColorHex);
        Assert.True(tokens.DownloadBrush.IsFrozen);
        Assert.True(tokens.UploadBrush.IsFrozen);
        Assert.True(tokens.CardBackgroundBrush.IsFrozen);
        Assert.True(tokens.CardBorderBrush.IsFrozen);
        Assert.True(tokens.DownloadAreaBrush.IsFrozen);
        Assert.True(tokens.UploadAreaBrush.IsFrozen);
    }

    [Fact]
    public void ThemeTokens_CyberpunkNeon_ReturnsCorrectPaletteColors()
    {
        var tokens = ThemeManager.GetTokens(ThemePalette.CyberpunkNeon);

        Assert.Equal("Cyberpunk Neon", tokens.Name);
        Assert.Equal("#F43F5E", tokens.DownloadColorHex);
        Assert.Equal("#06B6D4", tokens.UploadColorHex);
        Assert.True(tokens.DownloadBrush.IsFrozen);
        Assert.True(tokens.UploadBrush.IsFrozen);
    }

    [Fact]
    public void ThemeTokens_MonochromeStealth_ReturnsCorrectPaletteColors()
    {
        var tokens = ThemeManager.GetTokens(ThemePalette.MonochromeStealth);

        Assert.Equal("Monochrome Stealth", tokens.Name);
        Assert.Equal("#F8FAFC", tokens.DownloadColorHex);
        Assert.Equal("#94A3B8", tokens.UploadColorHex);
        Assert.Equal("#64748B", tokens.TextSecondaryHex);
        Assert.True(tokens.DownloadBrush.IsFrozen);
        Assert.True(tokens.UploadBrush.IsFrozen);
    }

    [Fact]
    public void ThemeTokens_SolarAmber_ReturnsCorrectPaletteColors()
    {
        var tokens = ThemeManager.GetTokens(ThemePalette.SolarAmber);

        Assert.Equal("Solar Amber", tokens.Name);
        Assert.Equal("#F59E0B", tokens.DownloadColorHex);
        Assert.Equal("#EAB308", tokens.UploadColorHex);
        Assert.True(tokens.DownloadBrush.IsFrozen);
        Assert.True(tokens.UploadBrush.IsFrozen);
    }

    [Fact]
    public void AppSettings_SerializesAndDeserializes_ThemeAndScalingProperties()
    {
        var settings = new AppSettings
        {
            Theme = ThemePalette.CyberpunkNeon,
            UiScale = 1.25,
            EnableAcrylicBlur = true
        };

        var json = JsonSerializer.Serialize(settings);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(ThemePalette.CyberpunkNeon, deserialized.Theme);
        Assert.Equal(1.25, deserialized.UiScale);
        Assert.True(deserialized.EnableAcrylicBlur);
    }

    [Fact]
    public void MainViewModel_SetTheme_UpdatesThemeAndNotifiesProperties()
    {
        var fakeSettings = new FakeSettingsService
        {
            CurrentSettings = new AppSettings { Theme = ThemePalette.EmeraldSky }
        };

        var vm = new MainViewModel(
            new FakeNetworkService(),
            fakeSettings,
            new FakePingService(),
            new FakeAutoStartService(),
            new FakeProcessNetworkService());

        var notifiedProps = new List<string>();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null) notifiedProps.Add(e.PropertyName);
        };

        vm.SetTheme(ThemePalette.SolarAmber);

        Assert.Equal(ThemePalette.SolarAmber, vm.CurrentTheme);
        Assert.Equal(ThemePalette.SolarAmber, fakeSettings.CurrentSettings.Theme);
        Assert.Contains(nameof(vm.CurrentTheme), notifiedProps);
        Assert.Contains(nameof(vm.DownloadBrush), notifiedProps);
        Assert.Contains(nameof(vm.UploadBrush), notifiedProps);
        Assert.Contains(nameof(vm.CardBackgroundBrush), notifiedProps);
    }

    [Fact]
    public void MainViewModel_SetScale_ClampsBetweenBoundsAndTriggersEvent()
    {
        var fakeSettings = new FakeSettingsService
        {
            CurrentSettings = new AppSettings { UiScale = 1.0 }
        };

        var vm = new MainViewModel(
            new FakeNetworkService(),
            fakeSettings,
            new FakePingService(),
            new FakeAutoStartService(),
            new FakeProcessNetworkService());

        double? receivedScale = null;
        vm.UiScaleChanged += sc => receivedScale = sc;

        // Test normal scale
        vm.SetScale(1.25);
        Assert.Equal(1.25, vm.UiScale);
        Assert.Equal(1.25, receivedScale);
        Assert.Equal(1.25, fakeSettings.CurrentSettings.UiScale);

        // Test lower bound clamping (0.70)
        vm.SetScale(0.30);
        Assert.Equal(0.70, vm.UiScale);
        Assert.Equal(0.70, fakeSettings.CurrentSettings.UiScale);

        // Test upper bound clamping (1.50)
        vm.SetScale(2.50);
        Assert.Equal(1.50, vm.UiScale);
        Assert.Equal(1.50, fakeSettings.CurrentSettings.UiScale);
    }

    [Fact]
    public void MainViewModel_ToggleAcrylicBlur_PersistsAndTriggersEvent()
    {
        var fakeSettings = new FakeSettingsService
        {
            CurrentSettings = new AppSettings { EnableAcrylicBlur = false }
        };

        var vm = new MainViewModel(
            new FakeNetworkService(),
            fakeSettings,
            new FakePingService(),
            new FakeAutoStartService(),
            new FakeProcessNetworkService());

        bool? receivedAcrylic = null;
        vm.AcrylicBlurChanged += b => receivedAcrylic = b;

        vm.EnableAcrylicBlur = true;
        Assert.True(vm.EnableAcrylicBlur);
        Assert.True(receivedAcrylic);
        Assert.True(fakeSettings.CurrentSettings.EnableAcrylicBlur);

        vm.EnableAcrylicBlur = false;
        Assert.False(vm.EnableAcrylicBlur);
        Assert.False(receivedAcrylic);
        Assert.False(fakeSettings.CurrentSettings.EnableAcrylicBlur);
    }
}
