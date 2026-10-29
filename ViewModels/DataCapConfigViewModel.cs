using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.Services;

namespace InternetMeter.ViewModels;

public class DataCapConfigViewModel : INotifyPropertyChanged
{
    private readonly ISettingsService _settingsService;
    private readonly IDataCapService _dataCapService;

    private bool _isEnabled;
    private double _dataCapGigabytes;
    private int _billingCycleResetDay;
    private double _warningThresholdPercent;
    private double _criticalThresholdPercent;
    private bool _enableToastNotifications;
    private string _billingCyclePreviewText = "";
    private string _statusMessage = "";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestClose;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
            {
                UpdatePreview();
            }
        }
    }

    public double DataCapGigabytes
    {
        get => _dataCapGigabytes;
        set
        {
            if (SetField(ref _dataCapGigabytes, Math.Max(0.1, value)))
            {
                UpdatePreview();
            }
        }
    }

    public int BillingCycleResetDay
    {
        get => _billingCycleResetDay;
        set
        {
            if (SetField(ref _billingCycleResetDay, Math.Clamp(value, 1, 31)))
            {
                UpdatePreview();
            }
        }
    }

    public double WarningThresholdPercent
    {
        get => _warningThresholdPercent;
        set
        {
            double clamped = Math.Clamp(value, 10.0, 95.0);
            if (SetField(ref _warningThresholdPercent, clamped))
            {
                if (_criticalThresholdPercent <= clamped)
                {
                    CriticalThresholdPercent = Math.Min(99.0, clamped + 5.0);
                }
            }
        }
    }

    public double CriticalThresholdPercent
    {
        get => _criticalThresholdPercent;
        set
        {
            double clamped = Math.Clamp(value, _warningThresholdPercent + 1.0, 99.9);
            SetField(ref _criticalThresholdPercent, clamped);
        }
    }

    public bool EnableToastNotifications
    {
        get => _enableToastNotifications;
        set => SetField(ref _enableToastNotifications, value);
    }

    public string BillingCyclePreviewText
    {
        get => _billingCyclePreviewText;
        private set => SetField(ref _billingCyclePreviewText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public ICommand SetPresetCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetAlertsCommand { get; }

    public DataCapConfigViewModel(ISettingsService settingsService, IDataCapService dataCapService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _dataCapService = dataCapService ?? throw new ArgumentNullException(nameof(dataCapService));

        var s = _settingsService.Load();
        _isEnabled = s.DataCapEnabled;
        _dataCapGigabytes = s.DataCapGigabytes > 0 ? s.DataCapGigabytes : 100.0;
        _billingCycleResetDay = Math.Clamp(s.BillingCycleResetDay, 1, 31);
        _warningThresholdPercent = s.WarningThresholdPercent > 0 ? s.WarningThresholdPercent : 80.0;
        _criticalThresholdPercent = s.CriticalThresholdPercent > 0 ? s.CriticalThresholdPercent : 95.0;
        _enableToastNotifications = s.EnableToastNotifications;

        SetPresetCommand = new RelayCommand<object>(preset =>
        {
            if (preset != null && double.TryParse(preset.ToString(), out double val))
            {
                DataCapGigabytes = val;
                IsEnabled = true;
            }
        });
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
        ResetAlertsCommand = new RelayCommand(ResetAlerts);

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var (start, end) = _dataCapService.GetBillingCycleRange(_billingCycleResetDay, DateTime.UtcNow);
        int daysRemaining = Math.Max(0, (end.Date - DateTime.UtcNow.Date).Days + 1);
        BillingCyclePreviewText = $"Active Cycle: {start:MMM dd, yyyy} – {end:MMM dd, yyyy} ({daysRemaining} days left)";
    }

    private void Save()
    {
        _dataCapService.UpdateSettings(
            _isEnabled,
            _dataCapGigabytes,
            _billingCycleResetDay,
            _warningThresholdPercent,
            _criticalThresholdPercent,
            _enableToastNotifications);

        RequestClose?.Invoke();
    }

    private void ResetAlerts()
    {
        _dataCapService.ResetAlertHistory();
        StatusMessage = "Alert state reset for the current cycle.";
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
