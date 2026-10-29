using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.Services;

namespace InternetMeter.ViewModels;

public class BandwidthAnalyticsViewModel : INotifyPropertyChanged
{
    private readonly IRollingHistoryService _rollingHistory;
    private TimeSpan _selectedTimeframe = TimeSpan.FromMinutes(60);
    private SpeedUnit _currentUnit = SpeedUnit.MBps;
    private ThemePalette _currentTheme = ThemePalette.EmeraldSky;

    private string _peakDownloadText = "↓ 0 B/s";
    private string _peakDownloadSubtext = "No peak recorded";
    private string _peakUploadText = "↑ 0 B/s";
    private string _peakUploadSubtext = "No peak recorded";
    private string _totalVolumeText = "0 B";
    private string _totalVolumeSubtext = "↓ 0 B · ↑ 0 B";
    private string _averageRateText = "0 B/s";
    private string _averageRateSubtext = "↓ 0 B/s · ↑ 0 B/s";
    private string _sampleCountText = "0 samples";

    private string _yAxisLabel100 = "10 MB/s";
    private string _yAxisLabel75 = "7.5 MB/s";
    private string _yAxisLabel50 = "5.0 MB/s";
    private string _yAxisLabel25 = "2.5 MB/s";
    private string _yAxisLabel0 = "0 MB/s";
    private double _maxYBytes = 10 * 1024 * 1024;

    private IReadOnlyList<BandwidthSample> _currentSamples = Array.Empty<BandwidthSample>();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestRedraw;
    public event Action<string>? CsvExportCompleted;

    public BandwidthAnalyticsViewModel(IRollingHistoryService rollingHistory, SpeedUnit unit, ThemePalette theme)
    {
        _rollingHistory = rollingHistory;
        _currentUnit = unit;
        _currentTheme = theme;

        Set15mCommand = new RelayCommand(() => SetTimeframe(TimeSpan.FromMinutes(15)));
        Set30mCommand = new RelayCommand(() => SetTimeframe(TimeSpan.FromMinutes(30)));
        Set60mCommand = new RelayCommand(() => SetTimeframe(TimeSpan.FromMinutes(60)));
        ExportCsvCommand = new RelayCommand(ExportCsv);

        RefreshAnalytics();
    }

    public TimeSpan SelectedTimeframe
    {
        get => _selectedTimeframe;
        set
        {
            if (_selectedTimeframe != value)
            {
                _selectedTimeframe = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Is15mActive));
                OnPropertyChanged(nameof(Is30mActive));
                OnPropertyChanged(nameof(Is60mActive));
                RefreshAnalytics();
            }
        }
    }

    public bool Is15mActive => _selectedTimeframe.TotalMinutes == 15;
    public bool Is30mActive => _selectedTimeframe.TotalMinutes == 30;
    public bool Is60mActive => _selectedTimeframe.TotalMinutes == 60;

    public SpeedUnit CurrentUnit
    {
        get => _currentUnit;
        set
        {
            if (_currentUnit != value)
            {
                _currentUnit = value;
                OnPropertyChanged();
                RefreshAnalytics();
            }
        }
    }

    public ThemePalette CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnPropertyChanged();
                NotifyThemeProperties();
                RequestRedraw?.Invoke();
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
    }

    public string PeakDownloadText
    {
        get => _peakDownloadText;
        private set => SetField(ref _peakDownloadText, value);
    }

    public string PeakDownloadSubtext
    {
        get => _peakDownloadSubtext;
        private set => SetField(ref _peakDownloadSubtext, value);
    }

    public string PeakUploadText
    {
        get => _peakUploadText;
        private set => SetField(ref _peakUploadText, value);
    }

    public string PeakUploadSubtext
    {
        get => _peakUploadSubtext;
        private set => SetField(ref _peakUploadSubtext, value);
    }

    public string TotalVolumeText
    {
        get => _totalVolumeText;
        private set => SetField(ref _totalVolumeText, value);
    }

    public string TotalVolumeSubtext
    {
        get => _totalVolumeSubtext;
        private set => SetField(ref _totalVolumeSubtext, value);
    }

    public string AverageRateText
    {
        get => _averageRateText;
        private set => SetField(ref _averageRateText, value);
    }

    public string AverageRateSubtext
    {
        get => _averageRateSubtext;
        private set => SetField(ref _averageRateSubtext, value);
    }

    public string SampleCountText
    {
        get => _sampleCountText;
        private set => SetField(ref _sampleCountText, value);
    }

    public string YAxisLabel100 { get => _yAxisLabel100; private set => SetField(ref _yAxisLabel100, value); }
    public string YAxisLabel75 { get => _yAxisLabel75; private set => SetField(ref _yAxisLabel75, value); }
    public string YAxisLabel50 { get => _yAxisLabel50; private set => SetField(ref _yAxisLabel50, value); }
    public string YAxisLabel25 { get => _yAxisLabel25; private set => SetField(ref _yAxisLabel25, value); }
    public string YAxisLabel0 { get => _yAxisLabel0; private set => SetField(ref _yAxisLabel0, value); }
    public double MaxYBytes { get => _maxYBytes; private set => SetField(ref _maxYBytes, value); }

    public IReadOnlyList<BandwidthSample> CurrentSamples => _currentSamples;

    public ICommand Set15mCommand { get; }
    public ICommand Set30mCommand { get; }
    public ICommand Set60mCommand { get; }
    public ICommand ExportCsvCommand { get; }

    public void SetTimeframe(TimeSpan timeframe)
    {
        SelectedTimeframe = timeframe;
    }

    public void RefreshAnalytics()
    {
        _currentSamples = _rollingHistory.GetSamples(_selectedTimeframe);
        int count = _currentSamples.Count;

        string unitLbl = UnitFormatter.GetUnitLabel(CurrentUnit);

        if (count == 0)
        {
            PeakDownloadText = $"↓ {UnitFormatter.FormatSpeed(0, CurrentUnit)} {unitLbl}";
            PeakDownloadSubtext = "No samples";
            PeakUploadText = $"↑ {UnitFormatter.FormatSpeed(0, CurrentUnit)} {unitLbl}";
            PeakUploadSubtext = "No samples";
            TotalVolumeText = UnitFormatter.FormatDataSize(0);
            TotalVolumeSubtext = "↓ 0 B · ↑ 0 B";
            AverageRateText = $"{UnitFormatter.FormatSpeed(0, CurrentUnit)} {unitLbl}";
            AverageRateSubtext = $"↓ 0 {unitLbl} · ↑ 0 {unitLbl}";
            SampleCountText = $"0 samples ({(int)_selectedTimeframe.TotalMinutes}m window)";

            MaxYBytes = 1024 * 1024;
            UpdateYAxisLabels(MaxYBytes);
            RequestRedraw?.Invoke();
            return;
        }

        double peakDl = 0;
        BandwidthSample? peakDlSample = null;
        double peakUl = 0;
        BandwidthSample? peakUlSample = null;

        double sumDl = 0;
        double sumUl = 0;

        foreach (var s in _currentSamples)
        {
            sumDl += s.DownloadSpeedBytes;
            sumUl += s.UploadSpeedBytes;

            if (s.DownloadSpeedBytes >= peakDl)
            {
                peakDl = s.DownloadSpeedBytes;
                peakDlSample = s;
            }
            if (s.UploadSpeedBytes >= peakUl)
            {
                peakUl = s.UploadSpeedBytes;
                peakUlSample = s;
            }
        }

        PeakDownloadText = $"↓ {UnitFormatter.FormatSpeed(peakDl, CurrentUnit)} {unitLbl}";
        if (peakDlSample != null && peakDl > 0)
        {
            string app = !string.IsNullOrEmpty(peakDlSample.TopProcessName) ? $" ({peakDlSample.TopProcessName})" : "";
            PeakDownloadSubtext = $"at {peakDlSample.Timestamp:HH:mm:ss}{app}";
        }
        else
        {
            PeakDownloadSubtext = "Idle";
        }

        PeakUploadText = $"↑ {UnitFormatter.FormatSpeed(peakUl, CurrentUnit)} {unitLbl}";
        if (peakUlSample != null && peakUl > 0)
        {
            string app = !string.IsNullOrEmpty(peakUlSample.TopProcessName) ? $" ({peakUlSample.TopProcessName})" : "";
            PeakUploadSubtext = $"at {peakUlSample.Timestamp:HH:mm:ss}{app}";
        }
        else
        {
            PeakUploadSubtext = "Idle";
        }

        // Each sample is 1-second interval, so sum(bytes/sec) is approx total bytes transferred in this window
        long totalDlBytes = (long)sumDl;
        long totalUlBytes = (long)sumUl;
        TotalVolumeText = UnitFormatter.FormatDataSize(totalDlBytes + totalUlBytes);
        TotalVolumeSubtext = $"↓ {UnitFormatter.FormatDataSize(totalDlBytes)} · ↑ {UnitFormatter.FormatDataSize(totalUlBytes)}";

        double avgDl = sumDl / count;
        double avgUl = sumUl / count;
        AverageRateText = $"{UnitFormatter.FormatSpeed(avgDl + avgUl, CurrentUnit)} {unitLbl}";
        AverageRateSubtext = $"↓ {UnitFormatter.FormatSpeed(avgDl, CurrentUnit)} · ↑ {UnitFormatter.FormatSpeed(avgUl, CurrentUnit)} {unitLbl}";

        SampleCountText = $"{count:N0} samples ({(int)_selectedTimeframe.TotalMinutes}m window)";

        double maxObserved = Math.Max(peakDl, peakUl);
        MaxYBytes = CalculateNiceYMax(maxObserved);
        UpdateYAxisLabels(MaxYBytes);

        RequestRedraw?.Invoke();
    }

    private void UpdateYAxisLabels(double maxY)
    {
        string unitLbl = UnitFormatter.GetUnitLabel(CurrentUnit);
        YAxisLabel100 = $"{UnitFormatter.FormatSpeed(maxY, CurrentUnit)} {unitLbl}";
        YAxisLabel75 = $"{UnitFormatter.FormatSpeed(maxY * 0.75, CurrentUnit)} {unitLbl}";
        YAxisLabel50 = $"{UnitFormatter.FormatSpeed(maxY * 0.50, CurrentUnit)} {unitLbl}";
        YAxisLabel25 = $"{UnitFormatter.FormatSpeed(maxY * 0.25, CurrentUnit)} {unitLbl}";
        YAxisLabel0 = $"0 {unitLbl}";
    }

    public static double CalculateNiceYMax(double maxBytesPerSec)
    {
        if (maxBytesPerSec < 1024 * 64) return 1024 * 1024; // 1 MB/s floor
        
        // Target nice step
        double mbps = maxBytesPerSec / (1024 * 1024);
        double niceMb;
        if (mbps <= 1) niceMb = 1.0;
        else if (mbps <= 2) niceMb = 2.0;
        else if (mbps <= 5) niceMb = 5.0;
        else if (mbps <= 10) niceMb = 10.0;
        else if (mbps <= 25) niceMb = 25.0;
        else if (mbps <= 50) niceMb = 50.0;
        else if (mbps <= 100) niceMb = 100.0;
        else if (mbps <= 250) niceMb = 250.0;
        else if (mbps <= 500) niceMb = 500.0;
        else if (mbps <= 1000) niceMb = 1000.0;
        else
        {
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(mbps)));
            niceMb = Math.Ceiling((mbps * 1.1) / magnitude) * magnitude;
        }
        return niceMb * 1024 * 1024;
    }

    public BandwidthSample? GetSampleAt(double normalizedX)
    {
        if (_currentSamples.Count == 0) return null;
        if (normalizedX <= 0) return _currentSamples[0];
        if (normalizedX >= 1.0) return _currentSamples[_currentSamples.Count - 1];

        int index = (int)Math.Round(normalizedX * (_currentSamples.Count - 1));
        index = Math.Clamp(index, 0, _currentSamples.Count - 1);
        return _currentSamples[index];
    }

    public string GenerateCsvContent()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,IsoUtcTime,DownloadBytesPerSec,UploadBytesPerSec,DownloadFormatted,UploadFormatted,TopProcess,TopProcessBytesPerSec");

        foreach (var s in _currentSamples)
        {
            string ts = s.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            string iso = s.Timestamp.ToUniversalTime().ToString("o");
            string dlFmt = UnitFormatter.FormatSpeed(s.DownloadSpeedBytes, CurrentUnit).Replace(",", "");
            string ulFmt = UnitFormatter.FormatSpeed(s.UploadSpeedBytes, CurrentUnit).Replace(",", "");
            string proc = (s.TopProcessName ?? "").Replace("\"", "\"\"");
            sb.AppendLine($"{ts},{iso},{(long)s.DownloadSpeedBytes},{(long)s.UploadSpeedBytes},{dlFmt},{ulFmt},\"{proc}\",{(long)s.TopProcessSpeedBytes}");
        }

        return sb.ToString();
    }

    public void ExportCsv()
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(folder))
            {
                folder = Path.GetTempPath();
            }

            string filename = $"InternetMeter_Bandwidth_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string fullPath = Path.Combine(folder, filename);

            string content = GenerateCsvContent();
            File.WriteAllText(fullPath, content, Encoding.UTF8);

            CsvExportCompleted?.Invoke(fullPath);
        }
        catch
        {
            // Silently handled or notified
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
