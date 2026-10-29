using System.Text.RegularExpressions;
using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using Xunit;

namespace InternetMeter.Tests;

public class RollingHistoryServiceTests
{
    [Fact]
    public void RollingHistoryService_StoresAndRetrieves_WithinCapacity()
    {
        var service = new RollingHistoryService(capacity: 100);
        var baseTime = new DateTime(2026, 10, 29, 12, 0, 0);

        for (int i = 0; i < 50; i++)
        {
            service.RecordSample(
                timestamp: baseTime.AddSeconds(i),
                downloadSpeedBytes: (i + 1) * 1024 * 1024,
                uploadSpeedBytes: (i + 1) * 512 * 1024,
                topProcessName: i % 2 == 0 ? "brave.exe" : "code.exe",
                topProcessSpeedBytes: (i + 1) * 1024 * 1024);
        }

        Assert.Equal(50, service.Count);
        var all = service.GetAllSamples();
        Assert.Equal(50, all.Count);
        Assert.Equal(baseTime, all[0].Timestamp);
        Assert.Equal(baseTime.AddSeconds(49), all[49].Timestamp);
        Assert.Equal(50 * 1024 * 1024, all[49].DownloadSpeedBytes);
    }

    [Fact]
    public void RollingHistoryService_OverwritesOldest_WhenExceedingCapacity()
    {
        var service = new RollingHistoryService(capacity: 5);
        var baseTime = new DateTime(2026, 10, 29, 12, 0, 0);

        // Record 8 samples in a 5-capacity buffer
        for (int i = 0; i < 8; i++)
        {
            service.RecordSample(
                timestamp: baseTime.AddSeconds(i),
                downloadSpeedBytes: i * 1000,
                uploadSpeedBytes: i * 500,
                topProcessName: "test.exe",
                topProcessSpeedBytes: i * 1000);
        }

        Assert.Equal(5, service.Count);
        var all = service.GetAllSamples();
        Assert.Equal(5, all.Count);

        // Samples 0, 1, 2 were overwritten; should contain 3, 4, 5, 6, 7
        Assert.Equal(baseTime.AddSeconds(3), all[0].Timestamp);
        Assert.Equal(3000, all[0].DownloadSpeedBytes);
        Assert.Equal(baseTime.AddSeconds(7), all[4].Timestamp);
        Assert.Equal(7000, all[4].DownloadSpeedBytes);
    }

    [Fact]
    public void RollingHistoryService_GetSamples_SlicesByDuration()
    {
        var service = new RollingHistoryService(capacity: 3600);
        var baseTime = new DateTime(2026, 10, 29, 12, 0, 0);

        for (int i = 0; i < 1800; i++) // 30 minutes of 1Hz samples
        {
            service.RecordSample(
                timestamp: baseTime.AddSeconds(i),
                downloadSpeedBytes: 1024 * 1024,
                uploadSpeedBytes: 512 * 1024,
                topProcessName: "app.exe",
                topProcessSpeedBytes: 1024 * 1024);
        }

        var slice15m = service.GetSamples(TimeSpan.FromMinutes(15));
        Assert.Equal(900, slice15m.Count);

        var slice30m = service.GetSamples(TimeSpan.FromMinutes(30));
        Assert.Equal(1800, slice30m.Count);

        var slice60m = service.GetSamples(TimeSpan.FromMinutes(60));
        Assert.Equal(1800, slice60m.Count); // only 1800 recorded so far
    }

    [Fact]
    public void RollingHistoryService_Clear_ResetsCountAndSamples()
    {
        var service = new RollingHistoryService(capacity: 10);
        service.RecordSample(DateTime.Now, 100, 100, "app", 100);
        service.RecordSample(DateTime.Now, 200, 200, "app", 200);
        Assert.Equal(2, service.Count);

        service.Clear();
        Assert.Equal(0, service.Count);
        Assert.Empty(service.GetAllSamples());
    }

    [Fact]
    public void BandwidthAnalyticsViewModel_CalculatesPeakAndAverageAccurately()
    {
        var history = new RollingHistoryService(capacity: 100);
        var baseTime = new DateTime(2026, 10, 29, 14, 0, 0);

        history.RecordSample(baseTime.AddSeconds(0), 10 * 1024 * 1024, 2 * 1024 * 1024, "browser.exe", 10 * 1024 * 1024);
        history.RecordSample(baseTime.AddSeconds(1), 50 * 1024 * 1024, 5 * 1024 * 1024, "game_download.exe", 50 * 1024 * 1024); // peak DL
        history.RecordSample(baseTime.AddSeconds(2), 20 * 1024 * 1024, 30 * 1024 * 1024, "backup.exe", 30 * 1024 * 1024); // peak UL

        var vm = new BandwidthAnalyticsViewModel(history, SpeedUnit.MBps, ThemePalette.CyberpunkNeon);

        Assert.Equal(3, vm.CurrentSamples.Count);
        Assert.Contains("50.00 MB/s", vm.PeakDownloadText);
        Assert.Contains("game_download.exe", vm.PeakDownloadSubtext);
        Assert.Contains("30.00 MB/s", vm.PeakUploadText);
        Assert.Contains("backup.exe", vm.PeakUploadSubtext);
        Assert.Equal("Cyberpunk Neon", vm.CurrentTokens.Name);
    }

    [Fact]
    public void BandwidthAnalyticsViewModel_GeneratesValidCsvContent()
    {
        var history = new RollingHistoryService(capacity: 10);
        var sampleTime = new DateTime(2026, 10, 29, 12, 30, 0, DateTimeKind.Utc);
        history.RecordSample(sampleTime, 1048576, 524288, "test_client.exe", 1048576);

        var vm = new BandwidthAnalyticsViewModel(history, SpeedUnit.MBps, ThemePalette.EmeraldSky);
        string csv = vm.GenerateCsvContent();

        Assert.Contains("Timestamp,IsoUtcTime,DownloadBytesPerSec,UploadBytesPerSec,DownloadFormatted,UploadFormatted,TopProcess,TopProcessBytesPerSec", csv);
        Assert.Contains("1048576", csv);
        Assert.Contains("524288", csv);
        Assert.Contains("test_client.exe", csv);
    }

    [Fact]
    public void CalculateNiceYMax_ProducesCleanCeilings()
    {
        // 500 KB/s (< 1MB/s floor) -> 1 MB/s
        Assert.Equal(1024 * 1024, BandwidthAnalyticsViewModel.CalculateNiceYMax(500 * 1024));

        // 8 MB/s -> 10 MB/s
        Assert.Equal(10.0 * 1024 * 1024, BandwidthAnalyticsViewModel.CalculateNiceYMax(8 * 1024 * 1024));

        // 18 MB/s -> 25 MB/s
        Assert.Equal(25.0 * 1024 * 1024, BandwidthAnalyticsViewModel.CalculateNiceYMax(18 * 1024 * 1024));

        // 84 MB/s -> 100 MB/s
        Assert.Equal(100.0 * 1024 * 1024, BandwidthAnalyticsViewModel.CalculateNiceYMax(84 * 1024 * 1024));

        // 350 MB/s -> 500 MB/s
        Assert.Equal(500.0 * 1024 * 1024, BandwidthAnalyticsViewModel.CalculateNiceYMax(350 * 1024 * 1024));
    }
}

