using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using InternetMeter.Common;
using InternetMeter.Models;
using InternetMeter.Services;

namespace InternetMeter.Views;

public partial class UsageReportWindow : Window
{
    private readonly ITrafficJournalService _journal;
    private readonly SpeedUnit _unit;
    private TrafficTimeframe _currentTimeframe = TrafficTimeframe.Session;

    private static readonly SolidColorBrush ActiveTabBg = new(System.Windows.Media.Color.FromRgb(99, 102, 241));
    private static readonly SolidColorBrush ActiveTabText = new(System.Windows.Media.Color.FromRgb(255, 255, 255));
    private static readonly SolidColorBrush InactiveTabBg = new(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
    private static readonly SolidColorBrush InactiveTabText = new(System.Windows.Media.Color.FromRgb(148, 163, 184));

    public UsageReportWindow(ITrafficJournalService journal, SpeedUnit unit)
    {
        _journal = journal;
        _unit = unit;

        InitializeComponent();
        Loaded += UsageReportWindow_Loaded;
    }

    private void UsageReportWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTabStyles();
        ReloadData();
    }

    private void ReloadData()
    {
        string? query = SearchBox.Text?.Trim();
        var records = _journal.GetRecords(_currentTimeframe, _unit, query, sortBy: "Total", descending: true);
        var summary = _journal.GetSummary(_currentTimeframe);

        RecordsGrid.ItemsSource = records;
        EmptyStateText.Visibility = records.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        SummaryTotalText.Text = UnitFormatter.FormatDataSize(summary.TotalCombined);
        SummaryDlText.Text = $"↓ {UnitFormatter.FormatDataSize(summary.TotalDownload)}";
        SummaryUlText.Text = $"↑ {UnitFormatter.FormatDataSize(summary.TotalUpload)}";
        SummaryAppsText.Text = $"{summary.ActiveCount} active · {summary.ClosedCount} closed";
    }

    private void UpdateTabStyles()
    {
        TabSessionBtn.Background = _currentTimeframe == TrafficTimeframe.Session ? ActiveTabBg : InactiveTabBg;
        TabSessionBtn.Foreground = _currentTimeframe == TrafficTimeframe.Session ? ActiveTabText : InactiveTabText;

        TabTodayBtn.Background = _currentTimeframe == TrafficTimeframe.Today ? ActiveTabBg : InactiveTabBg;
        TabTodayBtn.Foreground = _currentTimeframe == TrafficTimeframe.Today ? ActiveTabText : InactiveTabText;

        TabMonthBtn.Background = _currentTimeframe == TrafficTimeframe.Month ? ActiveTabBg : InactiveTabBg;
        TabMonthBtn.Foreground = _currentTimeframe == TrafficTimeframe.Month ? ActiveTabText : InactiveTabText;
    }

    private void TabSession_Click(object sender, RoutedEventArgs e)
    {
        _currentTimeframe = TrafficTimeframe.Session;
        UpdateTabStyles();
        ReloadData();
    }

    private void TabToday_Click(object sender, RoutedEventArgs e)
    {
        _currentTimeframe = TrafficTimeframe.Today;
        UpdateTabStyles();
        ReloadData();
    }

    private void TabMonth_Click(object sender, RoutedEventArgs e)
    {
        _currentTimeframe = TrafficTimeframe.Month;
        UpdateTabStyles();
        ReloadData();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        bool hasText = !string.IsNullOrEmpty(SearchBox.Text);
        SearchPlaceholder.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
        ClearSearchBtn.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
        ReloadData();
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        SearchBox.Focus();
    }

    private void RecordsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RecordsGrid.SelectedItem is ProcessUsageRecord record && !string.IsNullOrEmpty(record.ExecutablePath))
        {
            try
            {
                if (File.Exists(record.ExecutablePath))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{record.ExecutablePath}\"")
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                // Fallback or permission restricted
            }
        }
    }

    private void ResetSession_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to reset the current session traffic data?\nHistorical daily records will not be deleted.",
            "Reset Current Session Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _journal.ResetSession();
            ReloadData();
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Process Traffic Usage to CSV",
                Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"InternetMeter_Usage_{_currentTimeframe}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dlg.ShowDialog() == true)
            {
                string csvContent = _journal.ExportToCsv(_currentTimeframe);
                File.WriteAllText(dlg.FileName, csvContent, Encoding.UTF8);

                System.Windows.MessageBox.Show(
                    $"Report saved successfully to:\n{dlg.FileName}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to export CSV:\n{ex.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
    }
}
