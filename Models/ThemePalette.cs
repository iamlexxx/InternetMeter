using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace InternetMeter.Models;

public enum ThemePalette
{
    EmeraldSky,
    CyberpunkNeon,
    MonochromeStealth,
    SolarAmber
}

public record ThemeTokens(
    string Name,
    string DownloadColorHex,
    string UploadColorHex,
    string AccentGlowHex,
    string CardBackgroundHex,
    string CardBorderHex,
    string TextSecondaryHex,
    double CardBackdropAlpha)
{
    public MediaColor DownloadColor => (MediaColor)MediaColorConverter.ConvertFromString(DownloadColorHex);
    public MediaColor UploadColor => (MediaColor)MediaColorConverter.ConvertFromString(UploadColorHex);
    public MediaColor AccentGlowColor => (MediaColor)MediaColorConverter.ConvertFromString(AccentGlowHex);
    public MediaColor CardBackgroundColor => (MediaColor)MediaColorConverter.ConvertFromString(CardBackgroundHex);
    public MediaColor CardBorderColor => (MediaColor)MediaColorConverter.ConvertFromString(CardBorderHex);
    public MediaColor TextSecondaryColor => (MediaColor)MediaColorConverter.ConvertFromString(TextSecondaryHex);

    public SolidColorBrush DownloadBrush => CreateFrozenBrush(DownloadColor);
    public SolidColorBrush UploadBrush => CreateFrozenBrush(UploadColor);
    public SolidColorBrush AccentGlowBrush => CreateFrozenBrush(AccentGlowColor);
    public SolidColorBrush CardBackgroundBrush => CreateFrozenBrush(CardBackgroundColor);
    public SolidColorBrush CardBorderBrush => CreateFrozenBrush(CardBorderColor);
    public SolidColorBrush TextSecondaryBrush => CreateFrozenBrush(TextSecondaryColor);

    public LinearGradientBrush DownloadAreaBrush
    {
        get
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1)
            };
            brush.GradientStops.Add(new GradientStop(MediaColor.FromArgb((byte)(255 * 0.25), DownloadColor.R, DownloadColor.G, DownloadColor.B), 0.0));
            brush.GradientStops.Add(new GradientStop(MediaColor.FromArgb(0, DownloadColor.R, DownloadColor.G, DownloadColor.B), 1.0));
            brush.Freeze();
            return brush;
        }
    }

    public LinearGradientBrush UploadAreaBrush
    {
        get
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1)
            };
            brush.GradientStops.Add(new GradientStop(MediaColor.FromArgb((byte)(255 * 0.25), UploadColor.R, UploadColor.G, UploadColor.B), 0.0));
            brush.GradientStops.Add(new GradientStop(MediaColor.FromArgb(0, UploadColor.R, UploadColor.G, UploadColor.B), 1.0));
            brush.Freeze();
            return brush;
        }
    }

    private static SolidColorBrush CreateFrozenBrush(MediaColor c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
