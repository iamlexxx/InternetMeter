using System.IO;
using InternetMeter.Models;
using InternetMeter.Services;
using Xunit;

namespace InternetMeter.Tests;

public class TrafficJournalServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly TrafficJournalService _journal;

    public TrafficJournalServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"traffic_test_{Guid.NewGuid():N}.json");
        _journal = new TrafficJournalService(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            try { File.Delete(_tempFile); } catch { }
        }
    }

    [Fact]
    public void RecordTraffic_AccumulatesDeltas_ForSameProcess()
    {
        // Act
        _journal.RecordTraffic("chrome.exe", @"C:\Chrome\chrome.exe", null, 1000, 200, 500, 100, true);
        _journal.RecordTraffic("chrome.exe", @"C:\Chrome\chrome.exe", null, 3000, 800, 1500, 400, true);

        // Assert
        var records = _journal.GetRecords(TrafficTimeframe.Session, SpeedUnit.Mbps);
        Assert.Single(records);

        var r = records[0];
        Assert.Equal("chrome.exe", r.ProcessName);
        Assert.Equal(4000, r.TotalDownloadBytes);
        Assert.Equal(1000, r.TotalUploadBytes);
        Assert.Equal(5000, r.CombinedBytes);
        Assert.True(r.IsActive);
    }

    [Fact]
    public void RecordTraffic_PreservesData_WhenProcessTerminates()
    {
        // Act: git.exe downloads 5 MB and then terminates
        _journal.RecordTraffic("git.exe", @"C:\Git\git.exe", null, 5_000_000, 20_000, 1_000_000, 5_000, true);
        
        // Simulating PID vanished: active list now only contains chrome
        _journal.UpdateActiveProcessList(new HashSet<string> { "chrome.exe" });

        // Assert: git data must STILL be preserved in session and marked as closed/inactive
        var records = _journal.GetRecords(TrafficTimeframe.Session, SpeedUnit.MBps);
        var gitRecord = records.FirstOrDefault(r => r.ProcessName.Equals("git.exe", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(gitRecord);
        Assert.Equal(5_000_000, gitRecord.TotalDownloadBytes);
        Assert.Equal(20_000, gitRecord.TotalUploadBytes);
        Assert.False(gitRecord.IsActive);
        Assert.Equal(0, gitRecord.CurrentDownloadSpeed);
    }

    [Fact]
    public void GetRecords_ComputesPercentages_Accurately()
    {
        // Act: 75MB for process A, 25MB for process B => 75% and 25%
        _journal.RecordTraffic("streamer.exe", null, null, 75_000_000, 0, 0, 0, true);
        _journal.RecordTraffic("downloader.exe", null, null, 25_000_000, 0, 0, 0, true);

        var records = _journal.GetRecords(TrafficTimeframe.Session, SpeedUnit.MBps, sortBy: "Total");

        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal("streamer.exe", records[0].ProcessName);
        Assert.Equal(75.0, Math.Round(records[0].PercentageOfTotal, 1));
        Assert.Equal("downloader.exe", records[1].ProcessName);
        Assert.Equal(25.0, Math.Round(records[1].PercentageOfTotal, 1));
    }

    [Fact]
    public void GetRecords_AppliesSearchFilter()
    {
        _journal.RecordTraffic("discord.exe", null, null, 100, 100, 0, 0, true);
        _journal.RecordTraffic("spotify.exe", null, null, 200, 200, 0, 0, true);
        _journal.RecordTraffic("steam.exe", null, null, 300, 300, 0, 0, true);

        var filtered = _journal.GetRecords(TrafficTimeframe.Session, SpeedUnit.Mbps, searchFilter: "spot");

        Assert.Single(filtered);
        Assert.Equal("spotify.exe", filtered[0].ProcessName);
    }

    [Fact]
    public void ExportToCsv_ProducesValidCsvFormat()
    {
        _journal.RecordTraffic("curl.exe", @"C:\tools\curl.exe", null, 1024, 512, 0, 0, false);

        string csv = _journal.ExportToCsv(TrafficTimeframe.Session);

        Assert.NotNull(csv);
        Assert.Contains("Application,Status,Download_Bytes,Upload_Bytes,Total_Bytes", csv);
        Assert.Contains("curl.exe", csv);
        Assert.Contains("1024", csv);
        Assert.Contains("512", csv);
        Assert.Contains("Closed", csv);
    }

    [Fact]
    public void ResetSession_ClearsActiveSession_Only()
    {
        _journal.RecordTraffic("browser.exe", null, null, 5000, 1000, 0, 0, true);
        Assert.NotEmpty(_journal.GetRecords(TrafficTimeframe.Session, SpeedUnit.Mbps));

        _journal.ResetSession();

        Assert.Empty(_journal.GetRecords(TrafficTimeframe.Session, SpeedUnit.Mbps));
    }

    [Fact]
    public void GetSummary_CalculatesAccurateTotals()
    {
        _journal.RecordTraffic("app1.exe", null, null, 4000, 1000, 0, 0, true);
        _journal.RecordTraffic("app2.exe", null, null, 6000, 2000, 0, 0, false);

        var summary = _journal.GetSummary(TrafficTimeframe.Session);

        Assert.Equal(10000, summary.TotalDownload);
        Assert.Equal(3000, summary.TotalUpload);
        Assert.Equal(13000, summary.TotalCombined);
        Assert.Equal(1, summary.ActiveCount);
        Assert.Equal(1, summary.ClosedCount);
    }
}
