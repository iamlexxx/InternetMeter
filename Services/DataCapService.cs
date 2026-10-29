using System;
using System.Linq;
using InternetMeter.Models;

namespace InternetMeter.Services;

public class DataCapService : IDataCapService
{
    private readonly ISettingsService _settingsService;
    private readonly ITrafficJournalService _trafficJournalService;
    private readonly object _lock = new();

    public DataCapQuotaInfo CurrentQuota { get; private set; }

    public event Action<DataCapQuotaInfo>? QuotaEvaluated;
    public event Action<QuotaAlertNotification>? QuotaAlertTriggered;

    public DataCapService(ISettingsService settingsService, ITrafficJournalService trafficJournalService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _trafficJournalService = trafficJournalService ?? throw new ArgumentNullException(nameof(trafficJournalService));

        CurrentQuota = new DataCapQuotaInfo
        {
            IsEnabled = false,
            CapBytes = 0,
            UsedBytes = 0,
            AlertLevel = QuotaAlertLevel.Normal
        };
    }

    public (DateTime StartDate, DateTime EndDate) GetBillingCycleRange(int resetDay, DateTime referenceDate)
    {
        int clampedDay = Math.Clamp(resetDay, 1, 31);
        DateTime refDate = referenceDate.Date;

        DateTime cycleStart;
        int daysInCurrentMonth = DateTime.DaysInMonth(refDate.Year, refDate.Month);
        int actualDayThisMonth = Math.Min(clampedDay, daysInCurrentMonth);

        if (refDate.Day >= actualDayThisMonth)
        {
            cycleStart = new DateTime(refDate.Year, refDate.Month, actualDayThisMonth, 0, 0, 0, DateTimeKind.Utc);
        }
        else
        {
            var prevMonth = refDate.AddMonths(-1);
            int daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
            int actualDayPrevMonth = Math.Min(clampedDay, daysInPrevMonth);
            cycleStart = new DateTime(prevMonth.Year, prevMonth.Month, actualDayPrevMonth, 0, 0, 0, DateTimeKind.Utc);
        }

        // Calculate next month reset start, end is 1 second before that start
        var nextCycleMonth = cycleStart.AddMonths(1);
        int daysInNextMonth = DateTime.DaysInMonth(nextCycleMonth.Year, nextCycleMonth.Month);
        int actualDayNextMonth = Math.Min(clampedDay, daysInNextMonth);
        var nextCycleStart = new DateTime(nextCycleMonth.Year, nextCycleMonth.Month, actualDayNextMonth, 0, 0, 0, DateTimeKind.Utc);
        var cycleEnd = nextCycleStart.AddSeconds(-1);

        return (cycleStart, cycleEnd);
    }

    public DataCapQuotaInfo EvaluateQuota()
    {
        lock (_lock)
        {
            var settings = _settingsService.Load();
            if (!settings.DataCapEnabled || settings.DataCapGigabytes <= 0)
            {
                CurrentQuota = new DataCapQuotaInfo
                {
                    IsEnabled = false,
                    CapBytes = 0,
                    UsedBytes = 0,
                    AlertLevel = QuotaAlertLevel.Normal,
                    StatusText = "Disabled"
                };
                QuotaEvaluated?.Invoke(CurrentQuota);
                return CurrentQuota;
            }

            DateTime now = DateTime.UtcNow;
            var (cycleStart, cycleEnd) = GetBillingCycleRange(settings.BillingCycleResetDay, now);
            int daysRemaining = Math.Max(0, (cycleEnd.Date - now.Date).Days + 1);

            double capBytes = settings.DataCapGigabytes * 1024.0 * 1024.0 * 1024.0;
            var (dlBytes, ulBytes, totalCombined) = _trafficJournalService.GetBillingCycleUsage(cycleStart, cycleEnd);

            double usedBytes = totalCombined;
            double percent = capBytes > 0 ? (usedBytes / capBytes) * 100.0 : 0.0;

            // Determine alert level
            QuotaAlertLevel level = QuotaAlertLevel.Normal;
            if (percent >= 100.0)
            {
                level = QuotaAlertLevel.Exceeded100;
            }
            else if (percent >= settings.CriticalThresholdPercent)
            {
                level = QuotaAlertLevel.Critical95;
            }
            else if (percent >= settings.WarningThresholdPercent)
            {
                level = QuotaAlertLevel.Warning80;
            }

            // Top process consumer
            string topProcess = "";
            double topProcessBytes = 0;
            try
            {
                var records = _trafficJournalService.GetBillingCycleRecords(cycleStart, cycleEnd, SpeedUnit.MBps);
                var top = records.FirstOrDefault();
                if (top != null)
                {
                    topProcess = top.ProcessName;
                    topProcessBytes = top.CombinedBytes;
                }
            }
            catch
            {
                // Non-critical metadata query
            }

            string colorHex = level switch
            {
                QuotaAlertLevel.Exceeded100 => "#EF4444", // Crimson
                QuotaAlertLevel.Critical95 => "#F43F5E",  // Hot Magenta / Coral
                QuotaAlertLevel.Warning80 => "#F59E0B",   // Vibrant Amber
                _ => "#10B981"                            // Emerald
            };

            string statusText = level switch
            {
                QuotaAlertLevel.Exceeded100 => "Cap Exceeded!",
                QuotaAlertLevel.Critical95 => $"Critical ({percent:F1}%)",
                QuotaAlertLevel.Warning80 => $"Warning ({percent:F1}%)",
                _ => "On Track"
            };

            var info = new DataCapQuotaInfo
            {
                IsEnabled = true,
                CapBytes = capBytes,
                UsedBytes = usedBytes,
                DaysRemainingInCycle = daysRemaining,
                CycleStartUtc = cycleStart,
                CycleEndUtc = cycleEnd,
                AlertLevel = level,
                TopConsumerProcess = topProcess,
                TopConsumerBytes = topProcessBytes,
                FormattedUsed = FormatVolume(usedBytes),
                FormattedCap = FormatVolume(capBytes),
                FormattedRemaining = FormatVolume(Math.Max(0, capBytes - usedBytes)),
                ProgressColorHex = colorHex,
                StatusText = statusText
            };

            CurrentQuota = info;

            // Handle Single-Fire Toast Alert Logic
            CheckAndDispatchAlert(settings, info, cycleStart);

            QuotaEvaluated?.Invoke(info);
            return info;
        }
    }

    private void CheckAndDispatchAlert(AppSettings settings, DataCapQuotaInfo info, DateTime cycleStart)
    {
        string cycleKey = cycleStart.ToString("yyyy-MM-dd");
        bool settingsChanged = false;

        // Reset threshold history if a new billing cycle commenced
        if (settings.LastNotifiedCycleStart != cycleKey)
        {
            settings.LastNotifiedCycleStart = cycleKey;
            settings.LastNotifiedThreshold = "None";
            settingsChanged = true;
        }

        if (settings.EnableToastNotifications && info.IsEnabled)
        {
            string lastNotified = settings.LastNotifiedThreshold;

            if (info.AlertLevel == QuotaAlertLevel.Warning80 && lastNotified == "None")
            {
                settings.LastNotifiedThreshold = "Warning80";
                settingsChanged = true;

                QuotaAlertTriggered?.Invoke(new QuotaAlertNotification(
                    QuotaAlertLevel.Warning80,
                    $"InternetMeter — {settings.WarningThresholdPercent:F0}% Data Cap Warning",
                    $"You have consumed {info.FormattedUsed} of your {info.FormattedCap} quota ({info.UsagePercent:F1}%). {info.FormattedRemaining} remaining with {info.DaysRemainingInCycle} days left.",
                    info.UsagePercent,
                    info.FormattedUsed,
                    info.FormattedCap
                ));
            }
            else if (info.AlertLevel == QuotaAlertLevel.Critical95 && (lastNotified == "None" || lastNotified == "Warning80"))
            {
                settings.LastNotifiedThreshold = "Critical95";
                settingsChanged = true;

                string topInfo = !string.IsNullOrEmpty(info.TopConsumerProcess)
                    ? $" Top consumer: {info.TopConsumerProcess} ({FormatVolume(info.TopConsumerBytes)})."
                    : "";

                QuotaAlertTriggered?.Invoke(new QuotaAlertNotification(
                    QuotaAlertLevel.Critical95,
                    $"InternetMeter — ⚠ Critical {settings.CriticalThresholdPercent:F0}% Quota Alert",
                    $"Critical capacity reached: {info.FormattedUsed} used of {info.FormattedCap} ({info.UsagePercent:F1}%).{topInfo}",
                    info.UsagePercent,
                    info.FormattedUsed,
                    info.FormattedCap
                ));
            }
            else if (info.AlertLevel == QuotaAlertLevel.Exceeded100 && lastNotified != "Exceeded100")
            {
                settings.LastNotifiedThreshold = "Exceeded100";
                settingsChanged = true;

                QuotaAlertTriggered?.Invoke(new QuotaAlertNotification(
                    QuotaAlertLevel.Exceeded100,
                    "InternetMeter — ⛔ Data Cap Exceeded",
                    $"Your monthly allowance of {info.FormattedCap} has been exceeded ({info.FormattedUsed} consumed). Background traffic may incur ISP overage fees.",
                    info.UsagePercent,
                    info.FormattedUsed,
                    info.FormattedCap
                ));
            }
        }

        if (settingsChanged)
        {
            _settingsService.Save(settings);
        }
    }

    public void UpdateSettings(bool enabled, double capGigabytes, int resetDay, double warningPercent, double criticalPercent, bool enableToasts)
    {
        lock (_lock)
        {
            var settings = _settingsService.Load();
            settings.DataCapEnabled = enabled;
            settings.DataCapGigabytes = Math.Max(0.1, capGigabytes);
            settings.BillingCycleResetDay = Math.Clamp(resetDay, 1, 31);
            settings.WarningThresholdPercent = Math.Clamp(warningPercent, 10.0, 95.0);
            settings.CriticalThresholdPercent = Math.Clamp(criticalPercent, settings.WarningThresholdPercent + 1.0, 99.9);
            settings.EnableToastNotifications = enableToasts;

            _settingsService.Save(settings);
            EvaluateQuota();
        }
    }

    public void ResetAlertHistory()
    {
        lock (_lock)
        {
            var settings = _settingsService.Load();
            settings.LastNotifiedThreshold = "None";
            _settingsService.Save(settings);
        }
    }

    public static string FormatVolume(double bytes)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;
        const double TB = GB * 1024.0;

        if (bytes >= TB)
            return $"{bytes / TB:F2} TB";
        if (bytes >= GB)
            return $"{bytes / GB:F2} GB";
        if (bytes >= MB)
            return $"{bytes / MB:F1} MB";
        if (bytes >= KB)
            return $"{bytes / KB:F0} KB";

        return $"{bytes:F0} B";
    }
}

