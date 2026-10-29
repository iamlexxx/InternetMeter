using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using InternetMeter.Models;

namespace InternetMeter.Services;

public class TrafficJournalService : ITrafficJournalService
{
    private class ProcessDailyUsage
    {
        public long Dl { get; set; }
        public long Ul { get; set; }
    }

    private class HistoryStorageModel
    {
        public int Version { get; set; } = 1;
        public Dictionary<string, Dictionary<string, ProcessDailyUsage>> Days { get; set; } = new();
    }

    private readonly object _lock = new();
    private readonly Dictionary<string, ProcessUsageRecord> _sessionRecords = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string? ExePath, ImageSource? Icon)> _metadataCache = new(StringComparer.OrdinalIgnoreCase);
    private HistoryStorageModel _history = new();

    private readonly string _historyFilePath;
    private DateTime _lastSaveTime = DateTime.UtcNow;

    public TrafficJournalService(string? customFilePath = null)
    {
        if (!string.IsNullOrEmpty(customFilePath))
        {
            _historyFilePath = customFilePath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "InternetMeter");
            Directory.CreateDirectory(dir);
            _historyFilePath = Path.Combine(dir, "usage_history.json");
        }

        LoadHistory();
    }

    public void RecordTraffic(
        string processName,
        string? exePath,
        ImageSource? icon,
        long deltaDownloadBytes,
        long deltaUploadBytes,
        double currentDlSpeed,
        double currentUlSpeed,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;

        string normalizedName = processName.Trim();
        if (!normalizedName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName += ".exe";
        }

        lock (_lock)
        {
            // Cache metadata (path & icon)
            if (!string.IsNullOrEmpty(exePath) || icon != null)
            {
                _metadataCache[normalizedName] = (exePath, icon);
            }
            else if (_metadataCache.TryGetValue(normalizedName, out var cachedMeta))
            {
                exePath = cachedMeta.ExePath;
                icon = cachedMeta.Icon;
            }

            // Update Session Record
            if (!_sessionRecords.TryGetValue(normalizedName, out var record))
            {
                record = new ProcessUsageRecord
                {
                    ProcessName = normalizedName,
                    ExecutablePath = exePath,
                    Icon = icon,
                    FirstSeenUtc = DateTime.UtcNow
                };
                _sessionRecords[normalizedName] = record;
            }

            record.TotalDownloadBytes += deltaDownloadBytes;
            record.TotalUploadBytes += deltaUploadBytes;
            record.CurrentDownloadSpeed = currentDlSpeed;
            record.CurrentUploadSpeed = currentUlSpeed;
            record.IsActive = isActive;
            record.LastActiveUtc = DateTime.UtcNow;
            if (icon != null) record.Icon = icon;
            if (!string.IsNullOrEmpty(exePath)) record.ExecutablePath = exePath;

            // Update Persistent Daily History
            if (deltaDownloadBytes > 0 || deltaUploadBytes > 0)
            {
                string todayKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
                if (!_history.Days.TryGetValue(todayKey, out var dayDict))
                {
                    dayDict = new Dictionary<string, ProcessDailyUsage>(StringComparer.OrdinalIgnoreCase);
                    _history.Days[todayKey] = dayDict;
                }

                if (!dayDict.TryGetValue(normalizedName, out var dayUsage))
                {
                    dayUsage = new ProcessDailyUsage();
                    dayDict[normalizedName] = dayUsage;
                }

                dayUsage.Dl += deltaDownloadBytes;
                dayUsage.Ul += deltaUploadBytes;

                // Throttled periodic save
                if ((DateTime.UtcNow - _lastSaveTime).TotalSeconds > 15)
                {
                    SaveHistoryInternal();
                }
            }
        }
    }

    public void UpdateActiveProcessList(ISet<string> activeProcessNames)
    {
        lock (_lock)
        {
            foreach (var kvp in _sessionRecords)
            {
                kvp.Value.IsActive = activeProcessNames.Contains(kvp.Key);
                if (!kvp.Value.IsActive)
                {
                    kvp.Value.CurrentDownloadSpeed = 0;
                    kvp.Value.CurrentUploadSpeed = 0;
                }
            }
        }
    }

    public IReadOnlyList<ProcessUsageRecord> GetRecords(
        TrafficTimeframe timeframe,
        SpeedUnit unit,
        string? searchFilter = null,
        string sortBy = "Total",
        bool descending = true)
    {
        lock (_lock)
        {
            List<ProcessUsageRecord> list;

            if (timeframe == TrafficTimeframe.Session)
            {
                list = _sessionRecords.Values.Select(CloneRecord).ToList();
            }
            else
            {
                list = AggregateHistoricalRecords(timeframe);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                string query = searchFilter.Trim();
                list = list.Where(r => r.ProcessName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Compute percentage of total
            long grandTotal = list.Sum(r => r.CombinedBytes);
            foreach (var r in list)
            {
                r.PercentageOfTotal = grandTotal > 0 ? (r.CombinedBytes / (double)grandTotal) * 100.0 : 0.0;
                r.FormattedTotalDownload = UnitFormatter.FormatDataSize(r.TotalDownloadBytes);
                r.FormattedTotalUpload = UnitFormatter.FormatDataSize(r.TotalUploadBytes);
                r.FormattedTotal = UnitFormatter.FormatDataSize(r.CombinedBytes);
                r.FormattedCurrentDownload = UnitFormatter.FormatSpeed(r.CurrentDownloadSpeed, unit);
                r.FormattedCurrentUpload = UnitFormatter.FormatSpeed(r.CurrentUploadSpeed, unit);
            }

            // Sort
            IEnumerable<ProcessUsageRecord> sorted = sortBy.ToLowerInvariant() switch
            {
                "download" => descending ? list.OrderByDescending(r => r.TotalDownloadBytes) : list.OrderBy(r => r.TotalDownloadBytes),
                "upload" => descending ? list.OrderByDescending(r => r.TotalUploadBytes) : list.OrderBy(r => r.TotalUploadBytes),
                "name" => descending ? list.OrderByDescending(r => r.ProcessName) : list.OrderBy(r => r.ProcessName),
                _ => descending ? list.OrderByDescending(r => r.CombinedBytes) : list.OrderBy(r => r.CombinedBytes)
            };

            return sorted.ToList();
        }
    }

    public (long TotalDownload, long TotalUpload, long TotalCombined, int ActiveCount, int ClosedCount) GetSummary(
        TrafficTimeframe timeframe)
    {
        var records = GetRecords(timeframe, SpeedUnit.MBps);
        long dl = records.Sum(r => r.TotalDownloadBytes);
        long ul = records.Sum(r => r.TotalUploadBytes);
        int active = records.Count(r => r.IsActive);
        int closed = records.Count(r => !r.IsActive);
        return (dl, ul, dl + ul, active, closed);
    }

    public (long TotalDownload, long TotalUpload, long TotalCombined) GetBillingCycleUsage(DateTime cycleStartUtc, DateTime cycleEndUtc)
    {
        lock (_lock)
        {
            long dl = 0;
            long ul = 0;

            string startDateStr = cycleStartUtc.Date.ToString("yyyy-MM-dd");
            string endDateStr = cycleEndUtc.Date.ToString("yyyy-MM-dd");

            foreach (var (dayKey, dayDict) in _history.Days)
            {
                if (string.CompareOrdinal(dayKey, startDateStr) >= 0 && string.CompareOrdinal(dayKey, endDateStr) <= 0)
                {
                    foreach (var usage in dayDict.Values)
                    {
                        dl += usage.Dl;
                        ul += usage.Ul;
                    }
                }
            }

            return (dl, ul, dl + ul);
        }
    }

    public IReadOnlyList<ProcessUsageRecord> GetBillingCycleRecords(DateTime cycleStartUtc, DateTime cycleEndUtc, SpeedUnit unit)
    {
        lock (_lock)
        {
            var result = new Dictionary<string, ProcessUsageRecord>(StringComparer.OrdinalIgnoreCase);
            string startDateStr = cycleStartUtc.Date.ToString("yyyy-MM-dd");
            string endDateStr = cycleEndUtc.Date.ToString("yyyy-MM-dd");

            foreach (var (dayKey, dayDict) in _history.Days)
            {
                if (string.CompareOrdinal(dayKey, startDateStr) >= 0 && string.CompareOrdinal(dayKey, endDateStr) <= 0)
                {
                    foreach (var (name, usage) in dayDict)
                    {
                        if (!result.TryGetValue(name, out var rec))
                        {
                            _metadataCache.TryGetValue(name, out var meta);
                            rec = new ProcessUsageRecord
                            {
                                ProcessName = name,
                                ExecutablePath = meta.ExePath,
                                Icon = meta.Icon,
                                IsActive = _sessionRecords.TryGetValue(name, out var sess) && sess.IsActive
                            };
                            result[name] = rec;
                        }

                        rec.TotalDownloadBytes += usage.Dl;
                        rec.TotalUploadBytes += usage.Ul;
                    }
                }
            }

            long totalAll = result.Values.Sum(r => r.CombinedBytes);
            foreach (var r in result.Values)
            {
                r.PercentageOfTotal = totalAll > 0 ? (double)r.CombinedBytes / totalAll * 100.0 : 0.0;
                r.FormattedTotalDownload = UnitFormatter.FormatSpeed(r.TotalDownloadBytes, unit) + " " + UnitFormatter.GetUnitLabel(unit);
                r.FormattedTotalUpload = UnitFormatter.FormatSpeed(r.TotalUploadBytes, unit) + " " + UnitFormatter.GetUnitLabel(unit);
                r.FormattedTotal = UnitFormatter.FormatSpeed(r.CombinedBytes, unit) + " " + UnitFormatter.GetUnitLabel(unit);
            }

            return result.Values.OrderByDescending(r => r.CombinedBytes).ToList();
        }
    }

    public void ResetSession()
    {
        lock (_lock)
        {
            _sessionRecords.Clear();
        }
    }

    public string ExportToCsv(TrafficTimeframe timeframe)
    {
        var records = GetRecords(timeframe, SpeedUnit.MBps);
        var sb = new StringBuilder();
        sb.AppendLine("Application,Status,Download_Bytes,Upload_Bytes,Total_Bytes,Formatted_Download,Formatted_Upload,Formatted_Total,Share_Percent,First_Seen,Last_Active,Executable_Path");

        foreach (var r in records)
        {
            string status = r.IsActive ? "Active" : "Closed";
            string path = (r.ExecutablePath ?? string.Empty).Replace("\"", "\"\"");
            sb.AppendLine($"\"{r.ProcessName}\",\"{status}\",{r.TotalDownloadBytes},{r.TotalUploadBytes},{r.CombinedBytes},\"{r.FormattedTotalDownload}\",\"{r.FormattedTotalUpload}\",\"{r.FormattedTotal}\",{r.PercentageOfTotal:F2}%,{r.FirstSeenUtc:u},{r.LastActiveUtc:u},\"{path}\"");
        }

        return sb.ToString();
    }

    public void SaveHistory()
    {
        lock (_lock)
        {
            SaveHistoryInternal();
        }
    }

    private void SaveHistoryInternal()
    {
        try
        {
            string json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyFilePath, json);
            _lastSaveTime = DateTime.UtcNow;
        }
        catch
        {
            // Silently absorb transient file lock issues
        }
    }

    private void LoadHistory()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    string json = File.ReadAllText(_historyFilePath);
                    var data = JsonSerializer.Deserialize<HistoryStorageModel>(json);
                    if (data != null)
                    {
                        _history = data;
                        PruneOldHistory();
                    }
                }
            }
            catch
            {
                _history = new HistoryStorageModel();
            }
        }
    }

    private void PruneOldHistory()
    {
        // Keep up to 30 days
        var cutoff = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var keysToRemove = _history.Days.Keys.Where(k => string.CompareOrdinal(k, cutoff) < 0).ToList();
        foreach (var key in keysToRemove)
        {
            _history.Days.Remove(key);
        }
    }

    private List<ProcessUsageRecord> AggregateHistoricalRecords(TrafficTimeframe timeframe)
    {
        var result = new Dictionary<string, ProcessUsageRecord>(StringComparer.OrdinalIgnoreCase);
        string todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string monthPrefix = DateTime.UtcNow.ToString("yyyy-MM");

        foreach (var (dayKey, dayDict) in _history.Days)
        {
            bool include = timeframe switch
            {
                TrafficTimeframe.Today => dayKey == todayStr,
                TrafficTimeframe.Month => dayKey.StartsWith(monthPrefix, StringComparison.Ordinal),
                _ => true
            };

            if (!include) continue;

            foreach (var (name, usage) in dayDict)
            {
                if (!result.TryGetValue(name, out var rec))
                {
                    _metadataCache.TryGetValue(name, out var meta);
                    rec = new ProcessUsageRecord
                    {
                        ProcessName = name,
                        ExecutablePath = meta.ExePath,
                        Icon = meta.Icon,
                        IsActive = _sessionRecords.TryGetValue(name, out var sess) && sess.IsActive
                    };
                    result[name] = rec;
                }

                rec.TotalDownloadBytes += usage.Dl;
                rec.TotalUploadBytes += usage.Ul;
            }
        }

        return result.Values.ToList();
    }

    private static ProcessUsageRecord CloneRecord(ProcessUsageRecord src)
    {
        return new ProcessUsageRecord
        {
            ProcessName = src.ProcessName,
            TotalDownloadBytes = src.TotalDownloadBytes,
            TotalUploadBytes = src.TotalUploadBytes,
            CurrentDownloadSpeed = src.CurrentDownloadSpeed,
            CurrentUploadSpeed = src.CurrentUploadSpeed,
            IsActive = src.IsActive,
            ExecutablePath = src.ExecutablePath,
            Icon = src.Icon,
            FirstSeenUtc = src.FirstSeenUtc,
            LastActiveUtc = src.LastActiveUtc
        };
    }
}

