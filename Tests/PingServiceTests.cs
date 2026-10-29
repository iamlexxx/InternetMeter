using System.Net.NetworkInformation;
using InternetMeter.Models;
using InternetMeter.Services;
using Xunit;

namespace InternetMeter.Tests;

public class PingServiceTests
{
    [Theory]
    [InlineData(15, LatencyTier.Optimal, "15 ms", "#10B981")]
    [InlineData(39, LatencyTier.Optimal, "39 ms", "#10B981")]
    [InlineData(40, LatencyTier.Moderate, "40 ms", "#F59E0B")]
    [InlineData(85, LatencyTier.Moderate, "85 ms", "#F59E0B")]
    [InlineData(120, LatencyTier.Moderate, "120 ms", "#F59E0B")]
    [InlineData(121, LatencyTier.Critical, "121 ms", "#F43F5E")]
    [InlineData(250, LatencyTier.Critical, "250 ms", "#F43F5E")]
    public void PingTelemetry_Evaluate_SuccessCases(long rtt, LatencyTier expectedTier, string expectedText, string expectedColor)
    {
        var result = PingTelemetry.Evaluate(rtt, IPStatus.Success);

        Assert.Equal(expectedTier, result.Tier);
        Assert.Equal(expectedText, result.DisplayText);
        Assert.Equal(expectedColor, result.ColorHex);
    }

    [Theory]
    [InlineData(IPStatus.TimedOut, "Loss")]
    [InlineData(IPStatus.DestinationHostUnreachable, "Unreach")]
    [InlineData(IPStatus.DestinationNetworkUnreachable, "Unreach")]
    [InlineData(IPStatus.HardwareError, "Drop")]
    public void PingTelemetry_Evaluate_FailureCases(IPStatus status, string expectedText)
    {
        var result = PingTelemetry.Evaluate(-1, status);

        Assert.Equal(LatencyTier.Critical, result.Tier);
        Assert.Equal(expectedText, result.DisplayText);
        Assert.Equal("#F43F5E", result.ColorHex);
    }

    [Fact]
    public void PingTelemetry_CreateOffline_ReturnsExpectedDefaults()
    {
        var result = PingTelemetry.CreateOffline();

        Assert.Equal(LatencyTier.Offline, result.Tier);
        Assert.Equal("--", result.DisplayText);
        Assert.Equal("#64748B", result.ColorHex);
    }

    [Fact]
    public void PingService_StartAndStop_TogglesStateCorrectly()
    {
        using var service = new PingService();

        Assert.False(service.IsRunning);

        service.Start(5, "1.1.1.1");
        Assert.True(service.IsRunning);

        service.Stop();
        Assert.False(service.IsRunning);
    }
}

