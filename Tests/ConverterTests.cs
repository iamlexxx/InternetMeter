using System.Globalization;
using InternetMeter.Converters;
using InternetMeter.Models;
using Xunit;

namespace InternetMeter.Tests;

public class ConverterTests
{
    [Theory]
    [InlineData(SpeedUnit.Mbps, "Mbps", true)]
    [InlineData(SpeedUnit.Mbps, "MBps", false)] // Critical: Mbps != MBps
    [InlineData(SpeedUnit.MBps, "MBps", true)]
    [InlineData(SpeedUnit.MBps, "Mbps", false)] // Critical: MBps != Mbps
    [InlineData(SpeedUnit.Kbps, "Kbps", true)]
    [InlineData(SpeedUnit.Kbps, "KBps", false)] // Critical: Kbps != KBps
    [InlineData(SpeedUnit.KBps, "KBps", true)]
    [InlineData(SpeedUnit.KBps, "Kbps", false)] // Critical: KBps != Kbps
    public void EqualityToBooleanConverter_DistinguishesBytesAndBits(SpeedUnit currentUnit, string checkTarget, bool expected)
    {
        var converter = new EqualityToBooleanConverter();
        var result = (bool)converter.Convert(currentUnit, typeof(bool), checkTarget, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1.0, "1.0", true)]
    [InlineData(0.8, "0.8", true)]
    [InlineData(0.6, "0.6", true)]
    [InlineData(0.4, "0.4", true)]
    [InlineData(0.8, "0.6", false)]
    [InlineData(1.0, "0.4", false)]
    public void OpacityToBooleanConverter_EvaluatesTolerance(double actual, string parameter, bool expected)
    {
        var converter = new OpacityToBooleanConverter();
        var result = (bool)converter.Convert(actual, typeof(bool), parameter, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, System.Windows.Visibility.Collapsed)]
    [InlineData(false, System.Windows.Visibility.Visible)]
    public void InverseBooleanToVisibilityConverter_InvertsCorrectly(bool input, System.Windows.Visibility expected)
    {
        var converter = new InverseBooleanToVisibilityConverter();
        var result = (System.Windows.Visibility)converter.Convert(input, typeof(System.Windows.Visibility), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }
}
