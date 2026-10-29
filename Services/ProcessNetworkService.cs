using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using InternetMeter.Common;
using InternetMeter.Models;

namespace InternetMeter.Services;

public class ProcessNetworkService : IProcessNetworkService
{
    private record ProcessSnapshot(ulong ReadBytes, ulong WriteBytes, long TimestampTicks);
    private record CachedMetadata(string Name, ImageSource? Icon, string? ExePath, long CachedAtTicks);
    private record ProcessCandidate(int Pid, string Name, ImageSource? Icon, string? ExePath, double DlSpeed, double UlSpeed);

    private readonly Dictionary<int, ProcessSnapshot> _snapshots = new();
    private readonly Dictionary<int, CachedMetadata> _metadataCache = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly List<ProcessCandidate> _latestCandidates = new();

    public ITrafficJournalService Journal { get; }

    public ProcessNetworkService(ITrafficJournalService? journal = null)
    {
        Journal = journal ?? new TrafficJournalService();
    }

    public void UpdateTraffic(
        long systemDeltaDlBytes,
        long systemDeltaUlBytes,
        double systemDlRate,
        double systemUlRate)
    {
        long nowTicks = _stopwatch.ElapsedTicks;
        var activePids = NativeMethods.GetActiveNetworkPids();
        var rawDeltas = new List<(int Pid, ulong DeltaRead, ulong DeltaWrite, double Dt, ulong ReadBytes, ulong WriteBytes)>();
        var activeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (int pid in activePids)
        {
            if (pid <= 0) continue;

            if (NativeMethods.TryGetProcessIoBytes(pid, out ulong readBytes, out ulong writeBytes))
            {
                if (_snapshots.TryGetValue(pid, out var prev))
                {
                    double dt = (nowTicks - prev.TimestampTicks) / (double)Stopwatch.Frequency;
                    if (dt >= 0.25)
                    {
                        ulong deltaRead = readBytes >= prev.ReadBytes ? readBytes - prev.ReadBytes : 0;
                        ulong deltaWrite = writeBytes >= prev.WriteBytes ? writeBytes - prev.WriteBytes : 0;

                        rawDeltas.Add((pid, deltaRead, deltaWrite, dt, readBytes, writeBytes));
                    }
                }
                else
                {
                    _snapshots[pid] = new ProcessSnapshot(readBytes, writeBytes, nowTicks);
                    var (name, _, _) = ResolveMetadata(pid, nowTicks);
                    activeNames.Add(name);
                }
            }
        }

        // Aggregate raw process deltas to calibrate against NIC physical traffic
        ulong totalRawRead = 0;
        ulong totalRawWrite = 0;
        foreach (var item in rawDeltas)
        {
            totalRawRead += item.DeltaRead;
            totalRawWrite += item.DeltaWrite;
        }

        _latestCandidates.Clear();

        foreach (var item in rawDeltas)
        {
            _snapshots[item.Pid] = new ProcessSnapshot(item.ReadBytes, item.WriteBytes, nowTicks);

            var (name, icon, exePath) = ResolveMetadata(item.Pid, nowTicks);
            activeNames.Add(name);

            // Calibrate Download: local disk reads (file copies, cached assets) filtered out
            ulong calibratedDeltaRead;
            double calibratedDlSpeed;
            if (systemDeltaDlBytes <= 0)
            {
                calibratedDeltaRead = 0;
                calibratedDlSpeed = 0;
            }
            else if (totalRawRead <= (ulong)systemDeltaDlBytes)
            {
                calibratedDeltaRead = item.DeltaRead;
                calibratedDlSpeed = item.Dt > 0 ? calibratedDeltaRead / item.Dt : 0;
            }
            else
            {
                double share = totalRawRead > 0 ? (double)item.DeltaRead / totalRawRead : 0;
                calibratedDeltaRead = (ulong)Math.Round((double)systemDeltaDlBytes * share);
                calibratedDlSpeed = item.Dt > 0 ? calibratedDeltaRead / item.Dt : 0;
            }

            // Calibrate Upload: local disk writes (browser disk cache, sqlite, leveldb) filtered out
            ulong calibratedDeltaWrite;
            double calibratedUlSpeed;
            if (systemDeltaUlBytes <= 0)
            {
                calibratedDeltaWrite = 0;
                calibratedUlSpeed = 0;
            }
            else if (totalRawWrite <= (ulong)systemDeltaUlBytes)
            {
                calibratedDeltaWrite = item.DeltaWrite;
                calibratedUlSpeed = item.Dt > 0 ? calibratedDeltaWrite / item.Dt : 0;
            }
            else
            {
                double share = totalRawWrite > 0 ? (double)item.DeltaWrite / totalRawWrite : 0;
                calibratedDeltaWrite = (ulong)Math.Round((double)systemDeltaUlBytes * share);
                calibratedUlSpeed = item.Dt > 0 ? calibratedDeltaWrite / item.Dt : 0;
            }

            // Record true calibrated network traffic to persistent & session journal
            Journal.RecordTraffic(name, exePath, icon, (long)calibratedDeltaRead, (long)calibratedDeltaWrite, calibratedDlSpeed, calibratedUlSpeed, true);

            if (calibratedDlSpeed > 100 || calibratedUlSpeed > 100)
            {
                _latestCandidates.Add(new ProcessCandidate(item.Pid, name, icon, exePath, calibratedDlSpeed, calibratedUlSpeed));
            }
        }

        // Inform journal of currently active processes
        Journal.UpdateActiveProcessList(activeNames);

        // Cleanup stale PID snapshots
        if (_snapshots.Count > 250)
        {
            var staleKeys = _snapshots.Keys.Where(k => !activePids.Contains(k)).ToList();
            foreach (var k in staleKeys)
            {
                _snapshots.Remove(k);
                _metadataCache.Remove(k);
            }
        }
    }

    public IReadOnlyList<ProcessTrafficInfo> GetTopProcesses(
        int maxCount,
        SpeedUnit unit,
        bool isTotalMode,
        double totalSystemDownloadBytes = 0,
        double totalSystemUploadBytes = 0)
    {
        if (maxCount <= 0) maxCount = 4;

        if (isTotalMode)
        {
            var records = Journal.GetRecords(TrafficTimeframe.Session, unit, sortBy: "Total", descending: true);
            var totalList = new List<ProcessTrafficInfo>();

            foreach (var r in records.Take(maxCount))
            {
                totalList.Add(new ProcessTrafficInfo
                {
                    ProcessId = 0,
                    ProcessName = r.ProcessName,
                    DownloadSpeedBytes = r.CurrentDownloadSpeed,
                    UploadSpeedBytes = r.CurrentUploadSpeed,
                    TotalBytes = r.CombinedBytes,
                    FormattedDownloadSpeed = r.FormattedCurrentDownload,
                    FormattedUploadSpeed = r.FormattedCurrentUpload,
                    FormattedTotalData = r.FormattedTotal,
                    PercentageOfTotal = r.PercentageOfTotal,
                    IsActive = r.IsActive,
                    ExecutablePath = r.ExecutablePath,
                    Icon = r.Icon,
                    DisplayValueMain = $"{r.FormattedTotal} ({r.PercentageOfTotal:F0}%)",
                    DisplayValueSecondary = r.IsActive ? "active" : "[closed]"
                });
            }

            return totalList;
        }

        if (_latestCandidates.Count == 0 && !isTotalMode)
        {
            long estDl = (long)totalSystemDownloadBytes;
            long estUl = (long)totalSystemUploadBytes;
            UpdateTraffic(estDl, estUl, totalSystemDownloadBytes, totalSystemUploadBytes);
        }

        if (_latestCandidates.Count == 0)
        {
            return Array.Empty<ProcessTrafficInfo>();
        }

        // Group by process name so multi-PID apps (like Brave or Discord) show as one combined row in Live mode
        var grouped = _latestCandidates
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Name = g.Key,
                FirstPid = g.First().Pid,
                Icon = g.First().Icon,
                ExePath = g.First().ExePath,
                DlSpeed = g.Sum(x => x.DlSpeed),
                UlSpeed = g.Sum(x => x.UlSpeed),
                TotalSpeed = g.Sum(x => x.DlSpeed + x.UlSpeed)
            })
            .OrderByDescending(g => g.TotalSpeed)
            .Take(maxCount)
            .ToList();

        double baseTotal = totalSystemDownloadBytes + totalSystemUploadBytes;
        if (baseTotal <= 0)
        {
            baseTotal = grouped.Sum(t => t.TotalSpeed);
        }

        var results = new List<ProcessTrafficInfo>(grouped.Count);

        foreach (var item in grouped)
        {
            double pct = baseTotal > 0 ? Math.Min(100.0, (item.TotalSpeed / baseTotal) * 100.0) : 0;

            results.Add(new ProcessTrafficInfo
            {
                ProcessId = item.FirstPid,
                ProcessName = item.Name,
                DownloadSpeedBytes = item.DlSpeed,
                UploadSpeedBytes = item.UlSpeed,
                FormattedDownloadSpeed = UnitFormatter.FormatSpeed(item.DlSpeed, unit),
                FormattedUploadSpeed = UnitFormatter.FormatSpeed(item.UlSpeed, unit),
                PercentageOfTotal = pct,
                Icon = item.Icon,
                ExecutablePath = item.ExePath,
                IsActive = true,
                DisplayValueMain = $"↓ {UnitFormatter.FormatSpeed(item.DlSpeed, unit)}",
                DisplayValueSecondary = $"↑ {UnitFormatter.FormatSpeed(item.UlSpeed, unit)}"
            });
        }

        return results;
    }

    public void Reset()
    {
        _snapshots.Clear();
        _metadataCache.Clear();
        _latestCandidates.Clear();
    }

    private (string Name, ImageSource? Icon, string? ExePath) ResolveMetadata(int pid, long nowTicks)
    {
        if (_metadataCache.TryGetValue(pid, out var cached))
        {
            double cacheAgeSec = (nowTicks - cached.CachedAtTicks) / (double)Stopwatch.Frequency;
            if (cacheAgeSec < 30.0)
            {
                return (cached.Name, cached.Icon, cached.ExePath);
            }
        }

        string name = $"PID {pid}";
        ImageSource? icon = null;
        string? exePath = null;

        try
        {
            using var proc = Process.GetProcessById(pid);
            name = proc.ProcessName;
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name += ".exe";
            }

            try
            {
                exePath = proc.MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    using var sysIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (sysIcon != null)
                    {
                        var bmpSource = Imaging.CreateBitmapSourceFromHIcon(
                            sysIcon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bmpSource.Freeze();
                        icon = bmpSource;
                    }
                }
            }
            catch
            {
                // Protected process or 32/64 bit mismatch on MainModule
            }
        }
        catch
        {
            // Exited or access denied
        }

        _metadataCache[pid] = new CachedMetadata(name, icon, exePath, nowTicks);
        return (name, icon, exePath);
    }
}
