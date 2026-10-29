using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.Services;

namespace InternetMeter.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly INetworkService _networkService;
    private readonly ISettingsService _settingsService;
    private readonly IPingService _pingService;
    private readonly IAutoStartService _autoStartService;
    private readonly IProcessNetworkService _processNetworkService;
    private readonly IRollingHistoryService _rollingHistoryService;
    private readonly IDataCapService _dataCapService;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _timer;

    // Monthly Data Cap & Hotspot Quota State
    private DataCapQuotaInfo _currentQuota = new();
    private bool _isDataCapEnabled;
    private string _dataCapUsageText = "0 / 100 GB (0%)";
    private string _dataCapDaysLeftText = "-- days left";
    private double _dataCapProgressPercent;
    private System.Windows.Media.Brush _dataCapProgressBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
    private string _dataCapProgressColorHex = "#10B981";
    private QuotaAlertLevel _dataCapAlertLevel = QuotaAlertLevel.Normal;
    private bool _dataCapHasAlertWarning;
    private string _dataCapStatusText = "On Track";
    private string _dataCapToolTipText = "Monthly Data Cap";

    private readonly Queue<double> _dlHistory = new();
    private readonly Queue<double> _ulHistory = new();
    private const int HistoryCapacity = 24;
    private const double SparklineWidth = 70.0;
    private const double SparklineHeight = 18.0;

    private string _downloadSpeedText = "0.00";
    private string _uploadSpeedText = "0.00";
    private string _unitLabel = "Mbps";
    private SpeedUnit _currentUnit = SpeedUnit.Mbps;
    private bool _alwaysOnTop = true;
    private double _windowOpacity = 0.95;
    private bool _showSparkline = true;
    private bool _showFooter = true;
    private bool _showPingLatency = true;
    private bool _startWithWindows;
    private bool _startMinimizedOnBoot = true;
    private bool _isGhostMode;
    private double _ghostOpacity = 0.65;
    private bool _showProcessInspector = true;
    private bool _isProcessInspectorExpanded;
    private bool _isProcessInspectorTotalMode;
    private bool _isCompactMode;
    private int _maxProcessInspectorCount = 4;
    private bool _hasTopProcesses;
    private ThemePalette _currentTheme = ThemePalette.EmeraldSky;
    private double _uiScale = 1.0;
    private bool _enableAcrylicBlur;
    private string _latencyText = "--";
    private string _latencyColorHex = "#64748B";
    private NetworkHealthTelemetry? _currentHealth;
    private bool _isBufferbloatActive;
    private string _networkQualityGradeText = "A+";
    private string _networkQualityGradeColorHex = "#10B981";
    private string _latencyMinMaxText = "--";
    private string _jitterText = "--";
    private string _jitterRatingText = "--";
    private string _jitterColorHex = "#64748B";
    private string _packetLossText = "0.0%";
    private string _packetLossColorHex = "#10B981";
    private string _bufferbloatStatusText = "Clean Queue (Optimal)";
    private string _bufferbloatColorHex = "#10B981";
    private string _targetHostText = "1.1.1.1";
    private string _healthSummaryText = "Assessing connection health...";
    private bool _isConnected = true;
    private string _selectedAdapterId = "ALL";
    private string _selectedAdapterName = "All Sources";
    private string _sessionStatsText = "Session: ↓ 0 B  ↑ 0 B";

    private PointCollection _downloadPoints = new();
    private PointCollection _uploadPoints = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestResetPosition;
    public event Action? RequestOpenUsageReport;
    public event Action? RequestOpenAnalytics;
    public event Action? RequestOpenDataCapConfig;
    public event Action<QuotaAlertNotification>? QuotaToastRequested;
    public event Action<bool>? GhostModeChanged;
    public event Action<bool>? CompactModeToggled;
    public event Action<ThemePalette>? ThemeChanged;
    public event Action<double>? UiScaleChanged;
    public event Action<bool>? AcrylicBlurChanged;

    public ObservableCollection<NetworkAdapterInfo> Adapters { get; } = new();
    public ObservableCollection<ProcessTrafficInfo> TopProcesses { get; } = new();

    public MainViewModel(
        INetworkService networkService,
        ISettingsService settingsService,
        IPingService pingService,
        IAutoStartService autoStartService,
        IProcessNetworkService? processNetworkService = null,
        IRollingHistoryService? rollingHistoryService = null,
        IDataCapService? dataCapService = null)
    {
        _networkService = networkService;
        _settingsService = settingsService;
        _pingService = pingService;
        _autoStartService = autoStartService;
        _processNetworkService = processNetworkService ?? new ProcessNetworkService();
        _rollingHistoryService = rollingHistoryService ?? new RollingHistoryService();
        _settings = _settingsService.Load();
        _dataCapService = dataCapService ?? new DataCapService(_settingsService, _processNetworkService.Journal);
        _dataCapService.QuotaEvaluated += OnQuotaEvaluated;
        _dataCapService.QuotaAlertTriggered += OnQuotaAlertTriggered;

        // Initialize state from persisted settings
        _alwaysOnTop = _settings.AlwaysOnTop;
        _currentUnit = _settings.Unit;
        _unitLabel = UnitFormatter.GetUnitLabel(_currentUnit);
        _windowOpacity = _settings.Opacity;
        _showSparkline = _settings.ShowSparkline;
        _showFooter = _settings.ShowFooter;
        _showPingLatency = _settings.ShowPingLatency;
        _startWithWindows = _settings.StartWithWindows;
        _startMinimizedOnBoot = _settings.StartMinimizedOnBoot;
        _isGhostMode = _settings.IsGhostMode;
        _ghostOpacity = _settings.GhostOpacity > 0 ? _settings.GhostOpacity : 0.65;
        _showProcessInspector = _settings.ShowProcessInspector;
        _isProcessInspectorExpanded = _settings.IsProcessInspectorExpanded;
        _isProcessInspectorTotalMode = _settings.IsProcessInspectorTotalMode;
        _isCompactMode = _settings.IsCompactMode;
        _maxProcessInspectorCount = _settings.MaxProcessInspectorCount > 0 ? _settings.MaxProcessInspectorCount : 4;
        _currentTheme = _settings.Theme;
        _uiScale = _settings.UiScale > 0 ? _settings.UiScale : 1.0;
        _enableAcrylicBlur = _settings.EnableAcrylicBlur;
        _selectedAdapterId = _settings.SelectedAdapterId;

        // Sync registry with current executable path if enabled
        if (_startWithWindows)
        {
            _autoStartService.SyncWithCurrentPath(_startMinimizedOnBoot);
        }

        for (int i = 0; i < HistoryCapacity; i++)
        {
            _dlHistory.Enqueue(0);
            _ulHistory.Enqueue(0);
        }

        ReloadAdapters();

        // Setup commands
        CycleUnitCommand = new RelayCommand(CycleUnit);
        SetUnitCommand = new RelayCommand<SpeedUnit>(SetUnit);
        SetAdapterCommand = new RelayCommand<string>(SetAdapter);
        ToggleAlwaysOnTopCommand = new RelayCommand(ToggleAlwaysOnTop);
        SetOpacityCommand = new RelayCommand<double>(SetOpacity);
        ToggleSparklineCommand = new RelayCommand(() => ShowSparkline = !ShowSparkline);
        TogglePingCommand = new RelayCommand(() => ShowPingLatency = !ShowPingLatency);
        ToggleGhostModeCommand = new RelayCommand(() => IsGhostMode = !IsGhostMode);
        ToggleProcessInspectorCommand = new RelayCommand(ToggleProcessInspector);
        ToggleCompactModeCommand = new RelayCommand(ToggleCompactMode);
        SetThemeCommand = new RelayCommand<ThemePalette>(SetTheme);
        SetScaleCommand = new RelayCommand<double>(SetScale);
        ToggleAcrylicBlurCommand = new RelayCommand(() => EnableAcrylicBlur = !EnableAcrylicBlur);
        SetInspectorLiveModeCommand = new RelayCommand(() => IsProcessInspectorTotalMode = false);
        SetInspectorTotalModeCommand = new RelayCommand(() => IsProcessInspectorTotalMode = true);
        OpenUsageReportCommand = new RelayCommand(() => RequestOpenUsageReport?.Invoke());
        OpenAnalyticsCommand = new RelayCommand(() => RequestOpenAnalytics?.Invoke());
        OpenDataCapConfigCommand = new RelayCommand(() => RequestOpenDataCapConfig?.Invoke());
        ResetPositionCommand = new RelayCommand(() => RequestResetPosition?.Invoke());
        ExitCommand = new RelayCommand(() => System.Windows.Application.Current?.Shutdown());

        // Connect ping telemetry & network health listener
        _pingService.HealthUpdated += health =>
        {
            void Apply()
            {
                if (ShowPingLatency)
                {
                    CurrentHealth = health;
                    LatencyText = health.DisplayBadgeText;
                    LatencyColorHex = health.DisplayBadgeColorHex;
                    IsBufferbloatActive = health.IsBufferbloat;
                    NetworkQualityGradeText = health.GradeLabel;
                    NetworkQualityGradeColorHex = health.GradeColorHex;
                    LatencyMinMaxText = $"{health.MinRttMs} ms / {health.MaxRttMs} ms (Avg: {health.AvgRttMs} ms)";
                    JitterText = $"± {health.JitterMs:F1} ms";
                    JitterRatingText = health.JitterLabel;
                    JitterColorHex = health.JitterColorHex;
                    PacketLossText = $"{health.PacketLossPercent:F1}% ({health.LossCount}/{health.TotalProbesInWindow})";
                    PacketLossColorHex = health.PacketLossColorHex;
                    BufferbloatStatusText = health.IsBufferbloat
                        ? $"⚠ High Queue Saturation (+{health.BufferbloatDeltaMs} ms)"
                        : "Clean Queue (No delay penalty)";
                    BufferbloatColorHex = health.IsBufferbloat ? "#F59E0B" : "#10B981";
                    TargetHostText = health.TargetHost;
                    HealthSummaryText = health.GradeDescription;
                }
            }

            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.InvokeAsync(Apply);
            }
            else
            {
                Apply();
            }
        };

        _pingService.LatencyUpdated += telemetry =>
        {
            void Apply()
            {
                if (ShowPingLatency && CurrentHealth == null)
                {
                    LatencyText = telemetry.DisplayText;
                    LatencyColorHex = telemetry.ColorHex;
                }
            }

            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.InvokeAsync(Apply);
            }
            else
            {
                Apply();
            }
        };

        if (_showPingLatency)
        {
            _pingService.Start(_settings.PingIntervalSeconds, _settings.PingTargetHost);
        }

        _isDataCapEnabled = _settings.DataCapEnabled;
        if (_isDataCapEnabled)
        {
            _dataCapService.EvaluateQuota();
        }

        // Setup polling timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(500, _settings.RefreshIntervalMs))
        };
        _timer.Tick += (s, e) => OnTimerTick();
        _timer.Start();

        UpdateSparklinePoints();
    }

    public string DownloadSpeedText
    {
        get => _downloadSpeedText;
        set => SetField(ref _downloadSpeedText, value);
    }

    public string UploadSpeedText
    {
        get => _uploadSpeedText;
        set => SetField(ref _uploadSpeedText, value);
    }

    public string UnitLabel
    {
        get => _unitLabel;
        set => SetField(ref _unitLabel, value);
    }

    public SpeedUnit CurrentUnit
    {
        get => _currentUnit;
        set
        {
            if (SetField(ref _currentUnit, value))
            {
                UnitLabel = UnitFormatter.GetUnitLabel(value);
                _settings.Unit = value;
                _settingsService.Save(_settings);
            }
        }
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set
        {
            if (SetField(ref _alwaysOnTop, value))
            {
                _settings.AlwaysOnTop = value;
                _settingsService.Save(_settings);
            }
        }
    }

    public double WindowOpacity
    {
        get => _windowOpacity;
        set
        {
            if (SetField(ref _windowOpacity, value))
            {
                _settings.Opacity = value;
                _settingsService.Save(_settings);
                OnPropertyChanged(nameof(CurrentOpacity));
            }
        }
    }

    public double CurrentOpacity => IsGhostMode ? GhostOpacity : WindowOpacity;

    public bool ShowSparkline
    {
        get => _showSparkline;
        set
        {
            if (SetField(ref _showSparkline, value))
            {
                _settings.ShowSparkline = value;
                _settingsService.Save(_settings);
            }
        }
    }

    public ThemePalette CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (SetField(ref _currentTheme, value))
            {
                _settings.Theme = value;
                _settingsService.Save(_settings);
                NotifyThemeProperties();
                ThemeChanged?.Invoke(value);
            }
        }
    }

    public double UiScale
    {
        get => _uiScale;
        set
        {
            double clamped = Math.Clamp(value, 0.70, 1.50);
            if (SetField(ref _uiScale, clamped))
            {
                _settings.UiScale = clamped;
                _settingsService.Save(_settings);
                UiScaleChanged?.Invoke(clamped);
            }
        }
    }

    public bool EnableAcrylicBlur
    {
        get => _enableAcrylicBlur;
        set
        {
            if (SetField(ref _enableAcrylicBlur, value))
            {
                _settings.EnableAcrylicBlur = value;
                _settingsService.Save(_settings);
                AcrylicBlurChanged?.Invoke(value);
            }
        }
    }

    public ThemeTokens CurrentTokens => ThemeManager.GetTokens(CurrentTheme);

    public SolidColorBrush DownloadBrush => CurrentTokens.DownloadBrush;
    public SolidColorBrush UploadBrush => CurrentTokens.UploadBrush;
    public SolidColorBrush AccentGlowBrush => CurrentTokens.AccentGlowBrush;
    public SolidColorBrush CardBackgroundBrush => CurrentTokens.CardBackgroundBrush;
    public SolidColorBrush CardBorderBrush => CurrentTokens.CardBorderBrush;
    public SolidColorBrush TextSecondaryBrush => CurrentTokens.TextSecondaryBrush;
    public LinearGradientBrush DownloadAreaBrush => CurrentTokens.DownloadAreaBrush;
    public LinearGradientBrush UploadAreaBrush => CurrentTokens.UploadAreaBrush;
    public string DownloadColorHex => CurrentTokens.DownloadColorHex;
    public string UploadColorHex => CurrentTokens.UploadColorHex;
    public string AccentGlowHex => CurrentTokens.AccentGlowHex;
    public string CardBackgroundHex => CurrentTokens.CardBackgroundHex;
    public string CardBorderHex => CurrentTokens.CardBorderHex;

    private void NotifyThemeProperties()
    {
        OnPropertyChanged(nameof(CurrentTokens));
        OnPropertyChanged(nameof(DownloadBrush));
        OnPropertyChanged(nameof(UploadBrush));
        OnPropertyChanged(nameof(AccentGlowBrush));
        OnPropertyChanged(nameof(CardBackgroundBrush));
        OnPropertyChanged(nameof(CardBorderBrush));
        OnPropertyChanged(nameof(TextSecondaryBrush));
        OnPropertyChanged(nameof(DownloadAreaBrush));
        OnPropertyChanged(nameof(UploadAreaBrush));
        OnPropertyChanged(nameof(DownloadColorHex));
        OnPropertyChanged(nameof(UploadColorHex));
        OnPropertyChanged(nameof(AccentGlowHex));
        OnPropertyChanged(nameof(CardBackgroundHex));
        OnPropertyChanged(nameof(CardBorderHex));
    }

    public void SetTheme(ThemePalette palette)
    {
        CurrentTheme = palette;
    }

    public void SetScale(double scale)
    {
        UiScale = scale;
    }

    public bool ShowFooter
    {
        get => _showFooter;
        set
        {
            if (SetField(ref _showFooter, value))
            {
                _settings.ShowFooter = value;
                _settingsService.Save(_settings);
            }
        }
    }

    public bool ShowPingLatency
    {
        get => _showPingLatency;
        set
        {
            if (SetField(ref _showPingLatency, value))
            {
                _settings.ShowPingLatency = value;
                _settingsService.Save(_settings);

                if (value)
                {
                    _pingService.Start(_settings.PingIntervalSeconds, _settings.PingTargetHost);
                }
                else
                {
                    _pingService.Stop();
                    LatencyText = "--";
                    LatencyColorHex = "#64748B";
                }
            }
        }
    }

    public string LatencyText
    {
        get => _latencyText;
        set => SetField(ref _latencyText, value);
    }

    public string LatencyColorHex
    {
        get => _latencyColorHex;
        set => SetField(ref _latencyColorHex, value);
    }

    public NetworkHealthTelemetry? CurrentHealth
    {
        get => _currentHealth;
        private set => SetField(ref _currentHealth, value);
    }

    public bool IsBufferbloatActive
    {
        get => _isBufferbloatActive;
        private set => SetField(ref _isBufferbloatActive, value);
    }

    public string NetworkQualityGradeText
    {
        get => _networkQualityGradeText;
        private set => SetField(ref _networkQualityGradeText, value);
    }

    public string NetworkQualityGradeColorHex
    {
        get => _networkQualityGradeColorHex;
        private set => SetField(ref _networkQualityGradeColorHex, value);
    }

    public string LatencyMinMaxText
    {
        get => _latencyMinMaxText;
        private set => SetField(ref _latencyMinMaxText, value);
    }

    public string JitterText
    {
        get => _jitterText;
        private set => SetField(ref _jitterText, value);
    }

    public string JitterRatingText
    {
        get => _jitterRatingText;
        private set => SetField(ref _jitterRatingText, value);
    }

    public string JitterColorHex
    {
        get => _jitterColorHex;
        private set => SetField(ref _jitterColorHex, value);
    }

    public string PacketLossText
    {
        get => _packetLossText;
        private set => SetField(ref _packetLossText, value);
    }

    public string PacketLossColorHex
    {
        get => _packetLossColorHex;
        private set => SetField(ref _packetLossColorHex, value);
    }

    public string BufferbloatStatusText
    {
        get => _bufferbloatStatusText;
        private set => SetField(ref _bufferbloatStatusText, value);
    }

    public string BufferbloatColorHex
    {
        get => _bufferbloatColorHex;
        private set => SetField(ref _bufferbloatColorHex, value);
    }

    public string TargetHostText
    {
        get => _targetHostText;
        private set => SetField(ref _targetHostText, value);
    }

    public string HealthSummaryText
    {
        get => _healthSummaryText;
        private set => SetField(ref _healthSummaryText, value);
    }

    // Monthly Data Cap & Hotspot Quota
    public DataCapQuotaInfo CurrentQuota
    {
        get => _currentQuota;
        private set => SetField(ref _currentQuota, value);
    }

    public bool IsDataCapEnabled
    {
        get => _isDataCapEnabled;
        set
        {
            if (SetField(ref _isDataCapEnabled, value))
            {
                _settings.DataCapEnabled = value;
                _settingsService.Save(_settings);
                _dataCapService.UpdateSettings(
                    value,
                    _settings.DataCapGigabytes,
                    _settings.BillingCycleResetDay,
                    _settings.WarningThresholdPercent,
                    _settings.CriticalThresholdPercent,
                    _settings.EnableToastNotifications);
                OnPropertyChanged(nameof(DataCapGigabytes));
            }
        }
    }

    public double DataCapGigabytes => _settings.DataCapGigabytes;

    public void SetDataCapPreset(double gigabytes)
    {
        _settings.DataCapEnabled = true;
        _settings.DataCapGigabytes = Math.Max(0.1, gigabytes);
        _settingsService.Save(_settings);

        _dataCapService.UpdateSettings(
            true,
            _settings.DataCapGigabytes,
            _settings.BillingCycleResetDay,
            _settings.WarningThresholdPercent,
            _settings.CriticalThresholdPercent,
            _settings.EnableToastNotifications);

        _isDataCapEnabled = true;
        OnPropertyChanged(nameof(IsDataCapEnabled));
        OnPropertyChanged(nameof(DataCapGigabytes));
    }

    public void SyncSettingsFromDisk()
    {
        var fresh = _settingsService.Load();
        _settings.DataCapEnabled = fresh.DataCapEnabled;
        _settings.DataCapGigabytes = fresh.DataCapGigabytes;
        _settings.BillingCycleResetDay = fresh.BillingCycleResetDay;
        _settings.WarningThresholdPercent = fresh.WarningThresholdPercent;
        _settings.CriticalThresholdPercent = fresh.CriticalThresholdPercent;
        _settings.EnableToastNotifications = fresh.EnableToastNotifications;
        _settings.LastNotifiedThreshold = fresh.LastNotifiedThreshold;
        _settings.LastNotifiedCycleStart = fresh.LastNotifiedCycleStart;

        _isDataCapEnabled = fresh.DataCapEnabled;
        OnPropertyChanged(nameof(IsDataCapEnabled));
        OnPropertyChanged(nameof(DataCapGigabytes));
    }

    public string DataCapUsageText
    {
        get => _dataCapUsageText;
        private set => SetField(ref _dataCapUsageText, value);
    }

    public string DataCapDaysLeftText
    {
        get => _dataCapDaysLeftText;
        private set => SetField(ref _dataCapDaysLeftText, value);
    }

    public double DataCapProgressPercent
    {
        get => _dataCapProgressPercent;
        private set => SetField(ref _dataCapProgressPercent, value);
    }

    public System.Windows.Media.Brush DataCapProgressBrush
    {
        get => _dataCapProgressBrush;
        private set => SetField(ref _dataCapProgressBrush, value);
    }

    public string DataCapProgressColorHex
    {
        get => _dataCapProgressColorHex;
        private set => SetField(ref _dataCapProgressColorHex, value);
    }

    public QuotaAlertLevel DataCapAlertLevel
    {
        get => _dataCapAlertLevel;
        private set => SetField(ref _dataCapAlertLevel, value);
    }

    public bool DataCapHasAlertWarning
    {
        get => _dataCapHasAlertWarning;
        private set => SetField(ref _dataCapHasAlertWarning, value);
    }

    public string DataCapStatusText
    {
        get => _dataCapStatusText;
        private set => SetField(ref _dataCapStatusText, value);
    }

    public string DataCapToolTipText
    {
        get => _dataCapToolTipText;
        private set => SetField(ref _dataCapToolTipText, value);
    }

    public IDataCapService DataCapService => _dataCapService;
    public ISettingsService SettingsService => _settingsService;

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (SetField(ref _startWithWindows, value))
            {
                _settings.StartWithWindows = value;
                _settingsService.Save(_settings);
                _autoStartService.SetAutoStart(value, _startMinimizedOnBoot);
            }
        }
    }

    public bool StartMinimizedOnBoot
    {
        get => _startMinimizedOnBoot;
        set
        {
            if (SetField(ref _startMinimizedOnBoot, value))
            {
                _settings.StartMinimizedOnBoot = value;
                _settingsService.Save(_settings);
                if (_startWithWindows)
                {
                    _autoStartService.SetAutoStart(true, value);
                }
            }
        }
    }

    public bool IsGhostMode
    {
        get => _isGhostMode;
        set
        {
            if (SetField(ref _isGhostMode, value))
            {
                _settings.IsGhostMode = value;
                _settingsService.Save(_settings);
                OnPropertyChanged(nameof(CurrentOpacity));
                GhostModeChanged?.Invoke(value);
            }
        }
    }

    public double GhostOpacity
    {
        get => _ghostOpacity;
        set
        {
            if (SetField(ref _ghostOpacity, value))
            {
                _settings.GhostOpacity = value;
                _settingsService.Save(_settings);
                OnPropertyChanged(nameof(CurrentOpacity));
            }
        }
    }

    public bool ShowProcessInspector
    {
        get => _showProcessInspector;
        set
        {
            if (SetField(ref _showProcessInspector, value))
            {
                _settings.ShowProcessInspector = value;
                _settingsService.Save(_settings);
            }
        }
    }

    public bool IsProcessInspectorExpanded
    {
        get => _isProcessInspectorExpanded;
        set
        {
            if (SetField(ref _isProcessInspectorExpanded, value))
            {
                _settings.IsProcessInspectorExpanded = value;
                _settingsService.Save(_settings);
                OnPropertyChanged(nameof(ProcessInspectorToggleGlyph));
            }
        }
    }

    public bool IsProcessInspectorTotalMode
    {
        get => _isProcessInspectorTotalMode;
        set
        {
            if (SetField(ref _isProcessInspectorTotalMode, value))
            {
                _settings.IsProcessInspectorTotalMode = value;
                _settingsService.Save(_settings);
                RefreshTopProcesses();
            }
        }
    }

    public bool IsCompactMode
    {
        get => _isCompactMode;
        set
        {
            if (SetField(ref _isCompactMode, value))
            {
                _settings.IsCompactMode = value;
                _settingsService.Save(_settings);
                CompactModeToggled?.Invoke(value);
            }
        }
    }

    public void ToggleCompactMode()
    {
        IsCompactMode = !IsCompactMode;
    }

    public ITrafficJournalService Journal => _processNetworkService.Journal;
    public IRollingHistoryService RollingHistory => _rollingHistoryService;

    public bool HasTopProcesses
    {
        get => _hasTopProcesses;
        private set => SetField(ref _hasTopProcesses, value);
    }

    public string ProcessInspectorToggleGlyph => _isProcessInspectorExpanded ? "▴ Apps" : "▾ Apps";

    public bool IsConnected
    {
        get => _isConnected;
        set => SetField(ref _isConnected, value);
    }

    public string SelectedAdapterId
    {
        get => _selectedAdapterId;
        set => SetField(ref _selectedAdapterId, value);
    }

    public string SelectedAdapterName
    {
        get => _selectedAdapterName;
        set => SetField(ref _selectedAdapterName, value);
    }

    public string SessionStatsText
    {
        get => _sessionStatsText;
        set => SetField(ref _sessionStatsText, value);
    }

    public PointCollection DownloadPoints
    {
        get => _downloadPoints;
        private set => SetField(ref _downloadPoints, value);
    }

    public PointCollection UploadPoints
    {
        get => _uploadPoints;
        private set => SetField(ref _uploadPoints, value);
    }

    public ICommand CycleUnitCommand { get; }
    public ICommand SetUnitCommand { get; }
    public ICommand SetAdapterCommand { get; }
    public ICommand ToggleAlwaysOnTopCommand { get; }
    public ICommand SetOpacityCommand { get; }
    public ICommand ToggleSparklineCommand { get; }
    public ICommand TogglePingCommand { get; }
    public ICommand ToggleGhostModeCommand { get; }
    public ICommand ToggleProcessInspectorCommand { get; }
    public ICommand ToggleCompactModeCommand { get; }
    public ICommand SetThemeCommand { get; }
    public ICommand SetScaleCommand { get; }
    public ICommand ToggleAcrylicBlurCommand { get; }
    public ICommand SetInspectorLiveModeCommand { get; }
    public ICommand SetInspectorTotalModeCommand { get; }
    public ICommand OpenUsageReportCommand { get; }
    public ICommand OpenAnalyticsCommand { get; }
    public ICommand OpenDataCapConfigCommand { get; }
    public ICommand ResetPositionCommand { get; }
    public ICommand ExitCommand { get; }

    public void ReloadAdapters()
    {
        var list = _networkService.GetAvailableAdapters();
        Adapters.Clear();
        foreach (var item in list)
        {
            Adapters.Add(item);
        }

        var current = Adapters.FirstOrDefault(a => a.Id == _selectedAdapterId) ?? Adapters.FirstOrDefault();
        if (current != null)
        {
            _selectedAdapterId = current.Id;
            SelectedAdapterName = current.Name;
        }
    }

    public void CycleUnit()
    {
        CurrentUnit = CurrentUnit switch
        {
            SpeedUnit.MBps => SpeedUnit.Mbps,
            SpeedUnit.Mbps => SpeedUnit.KBps,
            SpeedUnit.KBps => SpeedUnit.Kbps,
            SpeedUnit.Kbps => SpeedUnit.MBps,
            _ => SpeedUnit.MBps
        };
    }

    public void SetUnit(SpeedUnit unit)
    {
        CurrentUnit = unit;
    }

    public void SetAdapter(string? adapterId)
    {
        if (string.IsNullOrEmpty(adapterId)) return;
        SelectedAdapterId = adapterId;
        _settings.SelectedAdapterId = adapterId;
        _settingsService.Save(_settings);

        var match = Adapters.FirstOrDefault(a => a.Id == adapterId);
        SelectedAdapterName = match?.Name ?? "Selected Adapter";
    }

    public void ToggleAlwaysOnTop()
    {
        AlwaysOnTop = !AlwaysOnTop;
    }

    public void SetOpacity(double opacity)
    {
        if (IsGhostMode)
        {
            IsGhostMode = false;
        }
        WindowOpacity = opacity;
    }

    public void SaveWindowCoordinates(double left, double top)
    {
        _settings.WindowLeft = left;
        _settings.WindowTop = top;
        _settingsService.Save(_settings);
    }

    public (double? left, double? top) GetSavedWindowCoordinates()
    {
        return (_settings.WindowLeft, _settings.WindowTop);
    }

    public void SaveCompactCoordinates(double left, double top)
    {
        _settings.CompactLeft = left;
        _settings.CompactTop = top;
        _settingsService.Save(_settings);
    }

    public (double? left, double? top) GetSavedCompactCoordinates()
    {
        return (_settings.CompactLeft, _settings.CompactTop);
    }

    public void ToggleProcessInspector()
    {
        IsProcessInspectorExpanded = !IsProcessInspectorExpanded;
    }

    private void OnTimerTick()
    {
        var (dlBytes, ulBytes, connected) = _networkService.SampleTraffic(SelectedAdapterId);
        var (deltaDl, deltaUl) = _networkService.GetLastSampleDeltas();
        IsConnected = connected;

        if (!connected)
        {
            LatencyText = "--";
            LatencyColorHex = "#64748B";
        }

        DownloadSpeedText = UnitFormatter.FormatSpeed(dlBytes, CurrentUnit);
        UploadSpeedText = UnitFormatter.FormatSpeed(ulBytes, CurrentUnit);

        var (totalDl, totalUl) = _networkService.GetSessionTotals();
        SessionStatsText = $"Session: ↓ {UnitFormatter.FormatDataSize(totalDl)}  ↑ {UnitFormatter.FormatDataSize(totalUl)}";

        _dlHistory.Dequeue();
        _dlHistory.Enqueue(dlBytes);
        _ulHistory.Dequeue();
        _ulHistory.Enqueue(ulBytes);

        UpdateSparklinePoints();

        // Always calibrate and record process network traffic with real physical adapter deltas
        _processNetworkService.UpdateTraffic(deltaDl, deltaUl, dlBytes, ulBytes);

        // Update instantaneous physical throughput for real-time bufferbloat evaluation
        _pingService.UpdateThroughput(dlBytes, ulBytes);

        // Record 1Hz detailed bandwidth telemetry with top process attribution
        var topProcess = _processNetworkService.GetTopProcesses(1, CurrentUnit, isTotalMode: false, dlBytes, ulBytes).FirstOrDefault();
        double topProcessSpeed = (topProcess?.DownloadSpeedBytes ?? 0) + (topProcess?.UploadSpeedBytes ?? 0);
        _rollingHistoryService.RecordSample(DateTime.Now, dlBytes, ulBytes, topProcess?.ProcessName, topProcessSpeed);

        if (_isDataCapEnabled)
        {
            _dataCapService.EvaluateQuota();
        }

        if (_showProcessInspector && _isProcessInspectorExpanded)
        {
            var top = _processNetworkService.GetTopProcesses(_maxProcessInspectorCount, CurrentUnit, _isProcessInspectorTotalMode, dlBytes, ulBytes);
            TopProcesses.Clear();
            foreach (var p in top)
            {
                TopProcesses.Add(p);
            }
            HasTopProcesses = TopProcesses.Count > 0;
        }
        else if (TopProcesses.Count > 0)
        {
            TopProcesses.Clear();
            HasTopProcesses = false;
        }
    }

    public void RefreshTopProcesses()
    {
        if (_showProcessInspector && _isProcessInspectorExpanded)
        {
            var top = _processNetworkService.GetTopProcesses(_maxProcessInspectorCount, CurrentUnit, _isProcessInspectorTotalMode, 0, 0);
            TopProcesses.Clear();
            foreach (var p in top)
            {
                TopProcesses.Add(p);
            }
            HasTopProcesses = TopProcesses.Count > 0;
        }
    }

    private void UpdateSparklinePoints()
    {
        DownloadPoints = GeneratePoints(_dlHistory);
        UploadPoints = GeneratePoints(_ulHistory);
    }

    private static PointCollection GeneratePoints(Queue<double> history)
    {
        var points = new PointCollection();
        var arr = history.ToArray();
        int count = arr.Length;
        if (count < 2) return points;

        double max = arr.Max();
        if (max < 1024) max = 1024; // baseline floor to prevent divide by zero and over-scaling idle noise

        double stepX = SparklineWidth / (count - 1);

        for (int i = 0; i < count; i++)
        {
            double x = i * stepX;
            double normalized = arr[i] / max;
            double y = (SparklineHeight - 2) - (normalized * (SparklineHeight - 4));
            points.Add(new System.Windows.Point(x, Math.Clamp(y, 1, SparklineHeight - 1)));
        }

        points.Freeze();
        return points;
    }

    private void OnQuotaEvaluated(DataCapQuotaInfo info)
    {
        void Apply()
        {
            CurrentQuota = info;
            _isDataCapEnabled = info.IsEnabled;
            _settings.DataCapEnabled = info.IsEnabled;
            if (info.CapBytes > 0)
            {
                _settings.DataCapGigabytes = Math.Round(info.CapBytes / (1024.0 * 1024.0 * 1024.0), 2);
            }
            OnPropertyChanged(nameof(IsDataCapEnabled));
            OnPropertyChanged(nameof(DataCapGigabytes));

            DataCapUsageText = $"{info.FormattedUsed} / {info.FormattedCap} ({info.UsagePercent:F1}%)";
            DataCapDaysLeftText = $"{info.DaysRemainingInCycle} days left";
            DataCapProgressPercent = Math.Clamp(info.UsagePercent, 0, 100);
            DataCapProgressColorHex = info.ProgressColorHex;
            try
            {
                var col = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(info.ProgressColorHex);
                DataCapProgressBrush = new SolidColorBrush(col);
            }
            catch
            {
                DataCapProgressBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
            }

            DataCapAlertLevel = info.AlertLevel;
            DataCapHasAlertWarning = info.AlertLevel != QuotaAlertLevel.Normal;
            DataCapStatusText = info.StatusText;

            string topInfo = !string.IsNullOrEmpty(info.TopConsumerProcess)
                ? $"\nTop Consumer: {info.TopConsumerProcess} ({InternetMeter.Services.DataCapService.FormatVolume(info.TopConsumerBytes)})"
                : "";

            DataCapToolTipText = $"Monthly Data Cap & Hotspot Quota\n" +
                                 $"Cycle: {info.CycleStartUtc:MMM dd} – {info.CycleEndUtc:MMM dd} ({info.DaysRemainingInCycle} days remaining)\n" +
                                 $"Used: {info.FormattedUsed} of {info.FormattedCap} ({info.UsagePercent:F1}%)\n" +
                                 $"Remaining: {info.FormattedRemaining} ({info.StatusText}){topInfo}\n" +
                                 $"Click to configure quota & cycle settings";
        }

        var app = System.Windows.Application.Current;
        if (app != null)
        {
            app.Dispatcher.InvokeAsync(Apply);
        }
        else
        {
            Apply();
        }
    }

    private void OnQuotaAlertTriggered(QuotaAlertNotification notification)
    {
        QuotaToastRequested?.Invoke(notification);
    }

    public void EvaluateDataCap()
    {
        _dataCapService.EvaluateQuota();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
