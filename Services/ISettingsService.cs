using InternetMeter.Models;

namespace InternetMeter.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}

