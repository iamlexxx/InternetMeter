using System.Windows;
using InternetMeter.Common;
using InternetMeter.Services;
using InternetMeter.ViewModels;
using WpfApplication = System.Windows.Application;

namespace InternetMeter;

public partial class App : WpfApplication
{
    private static Mutex? _instanceMutex;
    private const string MutexName = @"Global\InternetMeter_SingleInstance_Mutex_9D4F28A1";
    private const string FallbackMutexName = @"Local\InternetMeter_SingleInstance_Mutex_9D4F28A1";

    private MainWindow? _mainWindow;
    private PingService? _pingService;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        bool isOnlyInstance;
        try
        {
            _instanceMutex = new Mutex(true, MutexName, out isOnlyInstance);
        }
        catch (AbandonedMutexException)
        {
            // Previous instance closed without releasing
            isOnlyInstance = true;
        }
        catch
        {
            try
            {
                _instanceMutex = new Mutex(true, FallbackMutexName, out isOnlyInstance);
            }
            catch (AbandonedMutexException)
            {
                isOnlyInstance = true;
            }
            catch
            {
                isOnlyInstance = true;
            }
        }

        if (!isOnlyInstance)
        {
            // Signal the already running instance to restore & activate itself
            NativeMethods.SignalExistingInstance();

            // Terminate immediately with zero delay
            Environment.Exit(0);
            return;
        }

        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var networkService = new NetworkService();
        var settingsService = new SettingsService();
        var autoStartService = new AutoStartService();
        var processNetworkService = new ProcessNetworkService();
        var rollingHistoryService = new RollingHistoryService();
        _pingService = new PingService();
        _viewModel = new MainViewModel(networkService, settingsService, _pingService, autoStartService, processNetworkService, rollingHistoryService);

        _mainWindow = new MainWindow(_viewModel);

        bool isAutostart = e.Args.Any(a => string.Equals(a, "--autostart", StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

        if (isAutostart && _viewModel.StartMinimizedOnBoot)
        {
            // Keep window hidden in tray during auto-start boot
            _mainWindow.Visibility = Visibility.Hidden;
        }
        else
        {
            _mainWindow.Show();
        }
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _viewModel?.Journal.SaveHistory();
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Journal.SaveHistory();
        _pingService?.Dispose();

        if (_instanceMutex != null)
        {
            try
            {
                _instanceMutex.ReleaseMutex();
                _instanceMutex.Dispose();
            }
            catch
            {
                // Process teardown
            }
            finally
            {
                _instanceMutex = null;
            }
        }

        base.OnExit(e);
    }
}
