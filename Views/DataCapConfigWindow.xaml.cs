using System.Windows;
using System.Windows.Input;
using InternetMeter.Common;
using InternetMeter.Services;
using InternetMeter.ViewModels;

namespace InternetMeter.Views;

public partial class DataCapConfigWindow : Window
{
    private readonly DataCapConfigViewModel _viewModel;

    public DataCapConfigWindow(ISettingsService settingsService, IDataCapService dataCapService)
    {
        _viewModel = new DataCapConfigViewModel(settingsService, dataCapService);
        _viewModel.RequestClose += Close;
        DataContext = _viewModel;

        InitializeComponent();
        Loaded += DataCapConfigWindow_Loaded;
    }

    private void DataCapConfigWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                NativeMethods.ApplySystemBackdrop(hwnd, NativeMethods.DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW);
            }
        }
        catch
        {
            // Pre-Windows 11 fallback
        }
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
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
    }
}
