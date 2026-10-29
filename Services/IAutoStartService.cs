namespace InternetMeter.Services;

public interface IAutoStartService
{
    bool IsAutoStartEnabled();
    void SetAutoStart(bool enable, bool startMinimized = true);
    void SyncWithCurrentPath(bool startMinimized = true);
}

