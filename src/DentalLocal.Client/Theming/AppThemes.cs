using MudBlazor;

namespace DentalLocal.Client.Theming;

/// <summary>
/// Палитра акцентных цветов интерфейса.
/// </summary>
public enum AppAccent
{
    Blue,
    Teal
}

/// <summary>
/// Готовые темы MudBlazor для комбинаций режим (light/dark) x акцент (blue/teal),
/// плюс соответствующие CSS-переменные для кастомных (не-MudBlazor) элементов — премиум-минималистичный стиль
/// (чистые поверхности, мягкие тени, без глассморфизма/блюра).
/// </summary>
public static class AppThemes
{
    public static readonly MudTheme LightBlue = BuildTheme(isDark: false, accent: AppAccent.Blue);
    public static readonly MudTheme DarkBlue = BuildTheme(isDark: true, accent: AppAccent.Blue);
    public static readonly MudTheme LightTeal = BuildTheme(isDark: false, accent: AppAccent.Teal);
    public static readonly MudTheme DarkTeal = BuildTheme(isDark: true, accent: AppAccent.Teal);

    public static MudTheme Get(bool isDark, AppAccent accent) => (isDark, accent) switch
    {
        (false, AppAccent.Blue) => LightBlue,
        (true, AppAccent.Blue) => DarkBlue,
        (false, AppAccent.Teal) => LightTeal,
        (true, AppAccent.Teal) => DarkTeal,
        _ => LightBlue
    };

    /// <summary>Тема с произвольным акцентным цветом, заданным пользователем через color picker в Настройках (раздел 16 ТЗ).</summary>
    public static MudTheme GetCustom(bool isDark, string primaryHex) =>
        BuildTheme(isDark, AppAccent.Blue, customPrimaryHex: primaryHex);

    private static readonly string[] FontStack =
        ["Inter", "Segoe UI Variable", "-apple-system", "SF Pro Display", "Segoe UI", "sans-serif"];

    private static MudTheme BuildTheme(bool isDark, AppAccent accent, string? customPrimaryHex = null)
    {
        var (primary, primaryDarken, secondary) = accent switch
        {
            AppAccent.Teal => ("#0EA5A0", "#0B7F7A", "#6366F1"),
            _ => ("#2F6FED", "#2454BE", "#10B981")
        };

        if (customPrimaryHex is not null)
        {
            primary = customPrimaryHex;
            primaryDarken = Darken(customPrimaryHex, 0.18);
        }

        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = primary,
                PrimaryDarken = primaryDarken,
                Secondary = secondary,
                AppbarBackground = "#FFFFFF",
                AppbarText = "#111318",
                Background = "#FAFAFB",
                BackgroundGray = "#F1F2F5",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#111318",
                TextPrimary = "#111318",
                TextSecondary = "#6B7280",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                Info = "#0284C7",
                Divider = "rgba(17,19,24,0.08)",
                LinesDefault = "rgba(17,19,24,0.08)",
                GrayLight = "#F1F2F5",
                TableLines = "rgba(17,19,24,0.06)",
            },
            PaletteDark = new PaletteDark
            {
                Primary = primary,
                PrimaryDarken = primaryDarken,
                Secondary = secondary,
                AppbarBackground = "#16171C",
                AppbarText = "#F3F4F6",
                Background = "#0B0C0F",
                BackgroundGray = "#1D1F26",
                Surface = "#16171C",
                DrawerBackground = "#16171C",
                DrawerText = "#F3F4F6",
                TextPrimary = "#F3F4F6",
                TextSecondary = "#9AA0AC",
                Success = "#4ADE80",
                Warning = "#FBBF24",
                Error = "#F87171",
                Info = "#38BDF8",
                Divider = "rgba(243,244,246,0.08)",
                LinesDefault = "rgba(243,244,246,0.08)",
                TableLines = "rgba(243,244,246,0.06)",
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "10px",
                DrawerWidthLeft = "260px",
                AppbarHeight = "64px",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = FontStack,
                    FontSize = "0.9375rem",
                    LineHeight = "1.55",
                },
                H1 = new H1Typography { FontFamily = FontStack, FontSize = "2.5rem", FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-0.02em" },
                H2 = new H2Typography { FontFamily = FontStack, FontSize = "2rem", FontWeight = "700", LineHeight = "1.25", LetterSpacing = "-0.02em" },
                H3 = new H3Typography { FontFamily = FontStack, FontSize = "1.75rem", FontWeight = "650", LineHeight = "1.3", LetterSpacing = "-0.015em" },
                H4 = new H4Typography { FontFamily = FontStack, FontSize = "1.5rem", FontWeight = "650", LineHeight = "1.3", LetterSpacing = "-0.01em" },
                H5 = new H5Typography { FontFamily = FontStack, FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.35" },
                H6 = new H6Typography { FontFamily = FontStack, FontSize = "1.0625rem", FontWeight = "600", LineHeight = "1.4" },
                Subtitle1 = new Subtitle1Typography { FontFamily = FontStack, FontSize = "1rem", FontWeight = "500", LineHeight = "1.5" },
                Subtitle2 = new Subtitle2Typography { FontFamily = FontStack, FontSize = "0.875rem", FontWeight = "500", LineHeight = "1.45" },
                Body1 = new Body1Typography { FontFamily = FontStack, FontSize = "0.9375rem", FontWeight = "400", LineHeight = "1.6" },
                Body2 = new Body2Typography { FontFamily = FontStack, FontSize = "0.8438rem", FontWeight = "400", LineHeight = "1.55" },
                Button = new ButtonTypography { FontFamily = FontStack, FontSize = "0.875rem", FontWeight = "600", LineHeight = "1.4" },
                Caption = new CaptionTypography { FontFamily = FontStack, FontSize = "0.75rem", FontWeight = "400", LineHeight = "1.4" },
                Overline = new OverlineTypography { FontFamily = FontStack, FontSize = "0.6875rem", FontWeight = "600", LetterSpacing = "0.06em" },
            }
        };
    }

    /// <summary>Затемняет hex-цвет на заданную долю (0-1) — используется для PrimaryDarken кастомного акцента.</summary>
    private static string Darken(string hex, double amount)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value))
            return hex.Length == 6 ? $"#{hex}" : "#2F4FBF";

        var r = (value >> 16) & 0xFF;
        var g = (value >> 8) & 0xFF;
        var b = value & 0xFF;

        r = (int)(r * (1 - amount));
        g = (int)(g * (1 - amount));
        b = (int)(b * (1 - amount));

        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
