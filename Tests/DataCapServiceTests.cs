using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using InternetMeter.Models;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using Xunit;

namespace InternetMeter.Tests;

public class DataCapServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly TrafficJournalService _journal;
    private readonly FakeSettingsService _settingsService;
    private readonly DataCapService _service;

    private class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; set; } = new()
        {
            DataCapEnabled = true,
            DataCapGigabytes = 100.0,
            BillingCycleResetDay = 1,
            WarningThresholdPercent = 80.0,
            CriticalThresholdPercent = 95.0,
            EnableToastNotifications = true,
            LastNotifiedThreshold = "None",
            LastNotifiedCycleStart = ""
        };

        public AppSettings Load() => CurrentSettings;
        public void Save(AppSettings settings) => CurrentSettings = settings;
    }

    private class MockJournalService : ITrafficJournalService
    {
        public long DlToReturn { get; set; }
        public long UlToReturn { get; set; }
        public List<ProcessUsageRecord> RecordsToReturn { get; set; } = new();

        public void RecordTraffic(string processName, string? exePath, ImageSource? icon, long deltaDownloadBytes, long deltaUploadBytes, double currentDlSpeed, double currentUlSpeed, bool isActive) { }
        public void UpdateActiveProcessList(ISet<string> activeProcessNames) { }
        public IReadOnlyList<ProcessUsageRecord> GetRecords(TrafficTimeframe timeframe, SpeedUnit unit, string? searchFilter = null, string sortBy = "Total", bool descending = true) => RecordsToReturn;
        public (long TotalDownload, long TotalUpload, long TotalCombined, int ActiveCount, int ClosedCount) GetSummary(TrafficTimeframe timeframe) => (DlToReturn, UlToReturn, DlToReturn + UlToReturn, 1, 0);
        public void ResetSession() { }
        public string ExportToCsv(TrafficTimeframe timeframe) => "";
        public void SaveHistory() { }

        public (long TotalDownload, long TotalUpload, long TotalCombined) GetBillingCycleUsage(DateTime cycleStartUtc, DateTime cycleEndUtc)
        {
            return (DlToReturn, UlToReturn, DlToReturn + UlToReturn);
        }

        public IReadOnlyList<ProcessUsageRecord> GetBillingCycleRecords(DateTime cycleStartUtc, DateTime cycleEndUtc, SpeedUnit unit)
        {
            return RecordsToReturn;
        }
    }

    public DataCapServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"datacap_test_{Guid.NewGuid():N}.json");
        _journal = new TrafficJournalService(_tempFile);
        _settingsService = new FakeSettingsService();
        _service = new DataCapService(_settingsService, _journal);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            try { File.Delete(_tempFile); } catch { }
        }
    }

    [Fact]
    public void GetBillingCycleRange_CalculatesAccurately_ForFirstOfMonth()
    {
        // Reference: Oct 20, 2026, reset day = 1
        var (start, end) = _service.GetBillingCycleRange(1, new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc), end);
    }

    [Fact]
    public void GetBillingCycleRange_CalculatesAccurately_ForMidMonth()
    {
        // Reference: Oct 20, 2026 (Day >= 15) -> cycle started Oct 15, ends Nov 14
        var (start1, end1) = _service.GetBillingCycleRange(15, new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), start1);
        Assert.Equal(new DateTime(2026, 11, 14, 23, 59, 59, DateTimeKind.Utc), end1);

        // Reference: Oct 10, 2026 (Day < 15) -> cycle started Sept 15, ends Oct 14
        var (start2, end2) = _service.GetBillingCycleRange(15, new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), start2);
        Assert.Equal(new DateTime(2026, 10, 14, 23, 59, 59, DateTimeKind.Utc), end2);
    }

    [Fact]
    public void GetBillingCycleRange_ClampsAccurately_ForFebruaryAnd31st()
    {
        // Reset day 31 in Feb 2026 (non-leap year, 28 days)
        var (start, end) = _service.GetBillingCycleRange(31, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc));
        // Started Jan 31, ends Feb 27
        Assert.Equal(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 2, 27, 23, 59, 59, DateTimeKind.Utc), end);

        // Leap year 2024: Started Jan 31, ends Feb 28 (next cycle starts Feb 29)
        var (startLeap, endLeap) = _service.GetBillingCycleRange(31, new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc), startLeap);
        Assert.Equal(new DateTime(2024, 2, 28, 23, 59, 59, DateTimeKind.Utc), endLeap);
    }

    [Fact]
    public void EvaluateQuota_WhenDisabled_ReturnsDisabledState()
    {
        _settingsService.CurrentSettings.DataCapEnabled = false;

        var info = _service.EvaluateQuota();

        Assert.False(info.IsEnabled);
        Assert.Equal(QuotaAlertLevel.Normal, info.AlertLevel);
        Assert.Equal("Disabled", info.StatusText);
    }

    [Fact]
    public void EvaluateQuota_CalculatesUsageAndRemaining_Accurately()
    {
        var mockJournal = new MockJournalService
        {
            // 50 GB used out of 100 GB cap
            DlToReturn = 40L * 1024 * 1024 * 1024,
            UlToReturn = 10L * 1024 * 1024 * 1024,
            RecordsToReturn = new List<ProcessUsageRecord>
            {
                new() { ProcessName = "chrome.exe", TotalDownloadBytes = 40L * 1024 * 1024 * 1024, TotalUploadBytes = 10L * 1024 * 1024 * 1024 }
            }
        };

        var service = new DataCapService(_settingsService, mockJournal);
        var info = service.EvaluateQuota();

        Assert.True(info.IsEnabled);
        Assert.Equal(50.0, info.UsagePercent, 1);
        Assert.Equal(50.0 * 1024 * 1024 * 1024, info.RemainingBytes, 1);
        Assert.Equal(QuotaAlertLevel.Normal, info.AlertLevel);
        Assert.Equal("On Track", info.StatusText);
        Assert.Equal("chrome.exe", info.TopConsumerProcess);
    }

    [Fact]
    public void EvaluateQuota_AlertsTrigger_AtWarning80_Critical95_Exceeded100()
    {
        var mockJournal = new MockJournalService();
        var service = new DataCapService(_settingsService, mockJournal);

        QuotaAlertNotification? receivedNotification = null;
        service.QuotaAlertTriggered += n => receivedNotification = n;

        // 1. At 82% (Warning80)
        mockJournal.DlToReturn = 82L * 1024 * 1024 * 1024;
        mockJournal.UlToReturn = 0;
        var info82 = service.EvaluateQuota();

        Assert.Equal(QuotaAlertLevel.Warning80, info82.AlertLevel);
        Assert.NotNull(receivedNotification);
        Assert.Equal(QuotaAlertLevel.Warning80, receivedNotification!.Level);
        Assert.Equal("Warning80", _settingsService.CurrentSettings.LastNotifiedThreshold);

        // 2. At 96% (Critical95)
        receivedNotification = null;
        mockJournal.DlToReturn = 96L * 1024 * 1024 * 1024;
        var info96 = service.EvaluateQuota();

        Assert.Equal(QuotaAlertLevel.Critical95, info96.AlertLevel);
        Assert.NotNull(receivedNotification);
        Assert.Equal(QuotaAlertLevel.Critical95, receivedNotification!.Level);
        Assert.Equal("Critical95", _settingsService.CurrentSettings.LastNotifiedThreshold);

        // 3. At 102% (Exceeded100)
        receivedNotification = null;
        mockJournal.DlToReturn = 102L * 1024 * 1024 * 1024;
        var info102 = service.EvaluateQuota();

        Assert.Equal(QuotaAlertLevel.Exceeded100, info102.AlertLevel);
        Assert.NotNull(receivedNotification);
        Assert.Equal(QuotaAlertLevel.Exceeded100, receivedNotification!.Level);
        Assert.Equal("Exceeded100", _settingsService.CurrentSettings.LastNotifiedThreshold);
    }

    [Fact]
    public void EvaluateQuota_AlertsDeduplicate_WithinSameBillingCycle()
    {
        var mockJournal = new MockJournalService
        {
            DlToReturn = 85L * 1024 * 1024 * 1024 // 85% Warning
        };
        var service = new DataCapService(_settingsService, mockJournal);

        int triggerCount = 0;
        service.QuotaAlertTriggered += _ => triggerCount++;

        // First evaluation: fires notification
        service.EvaluateQuota();
        Assert.Equal(1, triggerCount);

        // Second evaluation at 86%: should NOT fire duplicate Warning notification
        mockJournal.DlToReturn = 86L * 1024 * 1024 * 1024;
        service.EvaluateQuota();
        Assert.Equal(1, triggerCount);

        // Third evaluation at 87%: still 1
        mockJournal.DlToReturn = 87L * 1024 * 1024 * 1024;
        service.EvaluateQuota();
        Assert.Equal(1, triggerCount);
    }

    [Fact]
    public void FormatVolume_FormatsAccurately()
    {
        Assert.Equal("500 B", DataCapService.FormatVolume(500));
        Assert.Equal("512 KB", DataCapService.FormatVolume(512 * 1024));
        Assert.Equal("25.5 MB", DataCapService.FormatVolume(25.5 * 1024 * 1024));
        Assert.Equal("4.20 GB", DataCapService.FormatVolume(4.2 * 1024 * 1024 * 1024));
        Assert.Equal("1.50 TB", DataCapService.FormatVolume(1.5 * 1024.0 * 1024 * 1024 * 1024));
    }

    [Fact]
    public void DataCapConfigViewModel_AppliesPresets_AndSavesSettings()
    {
        var vm = new DataCapConfigViewModel(_settingsService, _service);

        // Apply preset
        vm.SetPresetCommand.Execute(250.0);
        Assert.Equal(250.0, vm.DataCapGigabytes);
        Assert.True(vm.IsEnabled);

        vm.BillingCycleResetDay = 15;
        vm.WarningThresholdPercent = 85;
        vm.CriticalThresholdPercent = 97;

        bool closeRequested = false;
        vm.RequestClose += () => closeRequested = true;

        vm.SaveCommand.Execute(null);

        Assert.True(closeRequested);
        Assert.Equal(250.0, _settingsService.CurrentSettings.DataCapGigabytes);
        Assert.Equal(15, _settingsService.CurrentSettings.BillingCycleResetDay);
        Assert.Equal(85.0, _settingsService.CurrentSettings.WarningThresholdPercent);
        Assert.Equal(97.0, _settingsService.CurrentSettings.CriticalThresholdPercent);
    }

    private class FakeNetworkService : INetworkService
    {
        public IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters() => new List<NetworkAdapterInfo>();
        public (double downloadSpeedBytes, double uploadSpeedBytes, bool isConnected) SampleTraffic(string adapterId) => (1024 * 1024, 512 * 1024, true);
        public (long totalDownloadBytes, long totalUploadBytes) GetSessionTotals() => (0, 0);
        public void ResetSessionTotals() { }
        public (long deltaDownloadBytes, long deltaUploadBytes) GetLastSampleDeltas() => (1024 * 1024, 512 * 1024);
    }

    private class FakeAutoStartService : IAutoStartService
    {
        public bool IsAutoStartEnabled() => false;
        public void SetAutoStart(bool enable, bool startMinimized = true) { }
        public void SyncWithCurrentPath(bool startMinimized = true) { }
    }

    private class FakePingService : IPingService
    {
        public bool IsRunning => false;
        public NetworkHealthTelemetry? CurrentHealth => null;
#pragma warning disable CS0067
        public event Action<PingTelemetry>? LatencyUpdated;
        public event Action<NetworkHealthTelemetry>? HealthUpdated;
#pragma warning restore CS0067
        public void Start(int intervalSeconds = 2, string targetHost = "1.1.1.1") { }
        public void Stop() { }
        public void UpdateThroughput(double downloadSpeedBytes, double uploadSpeedBytes) { }
        public Task<PingTelemetry> ProbeOnceAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PingTelemetry.CreateOffline());
        public void Dispose() { }
    }

    [Fact]
    public void MainViewModel_DataCap_UpdatesReactiveProperties_WhenQuotaChanges()
    {
        var mockJournal = new MockJournalService
        {
            DlToReturn = 82L * 1024 * 1024 * 1024,
            UlToReturn = 0
        };

        var service = new DataCapService(_settingsService, mockJournal);
        var vm = new MainViewModel(
            new FakeNetworkService(),
            _settingsService,
            new FakePingService(),
            new FakeAutoStartService(),
            dataCapService: service);

        // Act
        vm.EvaluateDataCap();

        // Assert
        Assert.True(vm.IsDataCapEnabled);
        Assert.Contains("82.0", vm.DataCapUsageText);
        Assert.Equal(82.0, vm.DataCapProgressPercent, 1);
        Assert.Equal(QuotaAlertLevel.Warning80, vm.DataCapAlertLevel);
        Assert.True(vm.DataCapHasAlertWarning);
        Assert.Equal("#F59E0B", vm.DataCapProgressColorHex);
        Assert.NotNull(vm.DataCapProgressBrush);
        Assert.Contains("Monthly Data Cap", vm.DataCapToolTipText);
    }

    [Fact]
    public void MainViewModel_SetDataCapPreset_PersistsSettings_AndSurvivesWindowMove()
    {
        _settingsService.CurrentSettings.DataCapEnabled = false;
        _settingsService.CurrentSettings.DataCapGigabytes = 100.0;

        var mockJournal = new MockJournalService { DlToReturn = 0, UlToReturn = 0 };
        var service = new DataCapService(_settingsService, mockJournal);
        var vm = new MainViewModel(
            new FakeNetworkService(),
            _settingsService,
            new FakePingService(),
            new FakeAutoStartService(),
            dataCapService: service);

        // Initially disabled
        Assert.False(vm.IsDataCapEnabled);

        // Select 50 GB Preset
        vm.SetDataCapPreset(50.0);

        // Assert enabled and 50 GB
        Assert.True(vm.IsDataCapEnabled);
        Assert.Equal(50.0, vm.DataCapGigabytes);
        Assert.True(_settingsService.CurrentSettings.DataCapEnabled);
        Assert.Equal(50.0, _settingsService.CurrentSettings.DataCapGigabytes);

        // Simulate user clicking on widget / dragging widget (SaveWindowCoordinates)
        vm.SaveWindowCoordinates(100, 200);

        // Assert settings were NOT wiped or reverted to false
        Assert.True(_settingsService.CurrentSettings.DataCapEnabled);
        Assert.Equal(50.0, _settingsService.CurrentSettings.DataCapGigabytes);
        Assert.True(vm.IsDataCapEnabled);
    }
}
