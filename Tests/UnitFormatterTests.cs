using InternetMeter.Models;
using InternetMeter.Services;
using Xunit;

namespace InternetMeter.Tests;

public class UnitFormatterTests
{
    [Theory]
    [InlineData(0, SpeedUnit.KBps, "0.0")]
    [InlineData(1024, SpeedUnit.KBps, "1.0")]
    [InlineData(5120, SpeedUnit.KBps, "5.0")]
    [InlineData(1024 * 1024, SpeedUnit.MBps, "1.00")]
    [InlineData(1024 * 1024 * 15.5, SpeedUnit.MBps, "15.50")]
    [InlineData(125, SpeedUnit.Kbps, "1")] // 125 bytes * 8 = 1000 bits = 1 Kbps
    [InlineData(125000, SpeedUnit.Mbps, "1.00")] // 125,000 bytes * 8 = 1,000,000 bits = 1.00 Mbps
    [InlineData(12500000, SpeedUnit.Mbps, "100.00")] // 100 Mbps
    public void FormatSpeed_ConvertsCorrectly(double bytesPerSecond, SpeedUnit unit, string expected)
    {
        var result = UnitFormatter.FormatSpeed(bytesPerSecond, unit);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(SpeedUnit.KBps, "KB/s")]
    [InlineData(SpeedUnit.MBps, "MB/s")]
    [InlineData(SpeedUnit.Kbps, "Kbps")]
    [InlineData(SpeedUnit.Mbps, "Mbps")]
    public void GetUnitLabel_ReturnsExpectedLabel(SpeedUnit unit, string expected)
    {
        Assert.Equal(expected, UnitFormatter.GetUnitLabel(unit));
    }

    [Fact]
    public void FormatDataSize_HandlesSizesGracefully()
    {
        Assert.Equal("500 B", UnitFormatter.FormatDataSize(500));
        Assert.Equal("1.0 KB", UnitFormatter.FormatDataSize(1024));
        Assert.Equal("2.5 MB", UnitFormatter.FormatDataSize((long)(2.5 * 1024 * 1024)));
        Assert.Equal("3.20 GB", UnitFormatter.FormatDataSize((long)(3.20 * 1024 * 1024 * 1024)));
    }
}

