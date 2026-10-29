using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InternetMeter.Converters;

public class EqualityToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        // Case-sensitive comparison: 'B' (Byte) vs 'b' (bit) are distinct units
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.Ordinal);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ConnectionStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isConnected = value is true;
        return isConnected ? "#10B981" : "#EF4444";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is true;
        return b ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class OpacityToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double actual && double.TryParse(parameter?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double expected))
        {
            return Math.Abs(actual - expected) < 0.05;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && double.TryParse(parameter?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double target))
        {
            return target;
        }
        return System.Windows.Data.Binding.DoNothing;
    }
}

public class MultiplyConverter : IValueConverter
{
    public static readonly MultiplyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && double.TryParse(parameter?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double factor))
        {
            return d * factor;
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
