using InternetMeter.Models;
using InternetMeter.Services;
using Xunit;

namespace InternetMeter.Tests;

public class ServiceTests
{
    [Fact]
    public void NetworkService_EnumeratesAdapters_ContainsAllSources()
    {
        var service = new NetworkService();
        var adapters = service.GetAvailableAdapters();

        Assert.NotEmpty(adapters);
        Assert.Contains(adapters, a => a.Id == "ALL" && a.IsAllSources);
    }

    [Fact]
    public void NetworkService_SampleTraffic_ReturnsNonNegativeValues()
    {
        var service = new NetworkService();
        var (dl, ul, _) = service.SampleTraffic("ALL");

        Assert.True(dl >= 0);
        Assert.True(ul >= 0);
    }

    [Fact]
    public void SettingsService_DefaultValuesAreSound()
    {
        var settings = new AppSettings();

        Assert.True(settings.AlwaysOnTop);
        Assert.Equal("ALL", settings.SelectedAdapterId);
        Assert.Equal(SpeedUnit.Mbps, settings.Unit);
        Assert.True(settings.Opacity >= 0.4 && settings.Opacity <= 1.0);
        Assert.True(settings.ShowSparkline);
        Assert.True(settings.ShowFooter);
    }
}

