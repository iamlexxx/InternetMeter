using System.Diagnostics;
using Microsoft.Win32;

namespace InternetMeter.Services;

public class AutoStartService : IAutoStartService
{
    private const string RunRegistrySubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "InternetMeter";

    private readonly string _subKeyPath;

    public AutoStartService(string subKeyPath = RunRegistrySubKey)
    {
        _subKeyPath = subKeyPath;
    }

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_subKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read auto-start registry: {ex.Message}");
            return false;
        }
    }

    public void SetAutoStart(bool enable, bool startMinimized = true)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_subKeyPath, true);
            if (key == null) return;

            if (enable)
            {
                var command = BuildCommandLine(startMinimized);
                key.SetValue(AppName, command);
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to update auto-start registry: {ex.Message}");
        }
    }

    public void SyncWithCurrentPath(bool startMinimized = true)
    {
        if (IsAutoStartEnabled())
        {
            SetAutoStart(true, startMinimized);
        }
    }

    public static string BuildCommandLine(bool startMinimized, string? customExePath = null)
    {
        var exePath = customExePath ?? Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "InternetMeter.exe";
        return startMinimized ? $"\"{exePath}\" --autostart" : $"\"{exePath}\"";
    }
}

