using System.Windows.Media;
using InternetMeter.Models;

namespace InternetMeter.Common;

public static class ThemeManager
{
    private static readonly Dictionary<ThemePalette, ThemeTokens> Palettes = new()
    {
        [ThemePalette.EmeraldSky] = new ThemeTokens(
            Name: "Emerald & Sky",
            DownloadColorHex: "#10B981",     // Vibrant Emerald
            UploadColorHex: "#38BDF8",       // Sky Blue
            AccentGlowHex: "#6366F1",        // Indigo
            CardBackgroundHex: "#0D0F18",    // OLED Dark Slate
            CardBorderHex: "#1E2436",        // Subtle Border
            TextSecondaryHex: "#94A3B8",     // Slate
            CardBackdropAlpha: 0.88),

        [ThemePalette.CyberpunkNeon] = new ThemeTokens(
            Name: "Cyberpunk Neon",
            DownloadColorHex: "#F43F5E",     // Hot Magenta / Neon Rose
            UploadColorHex: "#06B6D4",       // Electric Cyan
            AccentGlowHex: "#A855F7",        // Neon Purple
            CardBackgroundHex: "#0B0C16",    // Midnight Void
            CardBorderHex: "#271B38",        // Deep Violet Border
            TextSecondaryHex: "#A1A1AA",     // Zinc
            CardBackdropAlpha: 0.85),

        [ThemePalette.MonochromeStealth] = new ThemeTokens(
            Name: "Monochrome Stealth",
            DownloadColorHex: "#F8FAFC",     // Pure Crisp White
            UploadColorHex: "#94A3B8",       // Muted Slate Silver
            AccentGlowHex: "#E2E8F0",        // Light Slate
            CardBackgroundHex: "#090A0F",    // Pitch Carbon
            CardBorderHex: "#222530",        // Low-key Border
            TextSecondaryHex: "#64748B",     // Dim Slate
            CardBackdropAlpha: 0.90),

        [ThemePalette.SolarAmber] = new ThemeTokens(
            Name: "Solar Amber",
            DownloadColorHex: "#F59E0B",     // Amber
            UploadColorHex: "#EAB308",       // Golden Sun
            AccentGlowHex: "#F97316",        // Solar Orange
            CardBackgroundHex: "#14100D",    // Smoked Obsidian
            CardBorderHex: "#332214",        // Warm Bronze Border
            TextSecondaryHex: "#A8A29E",     // Stone
            CardBackdropAlpha: 0.88)
    };

    public static ThemeTokens GetTokens(ThemePalette palette)
    {
        return Palettes.TryGetValue(palette, out var tokens) ? tokens : Palettes[ThemePalette.EmeraldSky];
    }
}

