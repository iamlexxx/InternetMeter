using System;
using InternetMeter.Models;

namespace InternetMeter.Services;

public interface IDataCapService
{
    DataCapQuotaInfo CurrentQuota { get; }
    event Action<DataCapQuotaInfo>? QuotaEvaluated;
    event Action<QuotaAlertNotification>? QuotaAlertTriggered;

    DataCapQuotaInfo EvaluateQuota();
    (DateTime StartDate, DateTime EndDate) GetBillingCycleRange(int resetDay, DateTime referenceDate);
    void UpdateSettings(bool enabled, double capGigabytes, int resetDay, double warningPercent, double criticalPercent, bool enableToasts);
    void ResetAlertHistory();
}

