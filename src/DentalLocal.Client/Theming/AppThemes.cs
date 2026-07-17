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
/// плюс соответствующие CSS-переменные для кастомных (не-MudBlazor) элементов, включая глассморфизм.
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

    private static MudTheme BuildTheme(bool isDark, AppAccent accent, string? customPrimaryHex = null)
    {
        var (primary, primaryDarken, secondary) = accent switch
        {
            AppAccent.Teal => ("#0F9B8E", "#0B7A70", "#5E60CE"),
            _ => ("#3E63DD", "#2F4FBF", "#00B8A9")
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
                AppbarBackground = "rgba(255,255,255,0.72)",
                AppbarText = "#1A2233",
                Background = "#F3F6FB",
                BackgroundGray = "#E9EEF6",
                Surface = "#FFFFFF",
                DrawerBackground = "rgba(255,255,255,0.65)",
                DrawerText = "#1A2233",
                TextPrimary = "#1A2233",
                TextSecondary = "#5B6478",
                Success = "#2FB380",
                Warning = "#E8A63C",
                Error = "#E5484D",
                Info = "#3E9CDD",
                Divider = "rgba(26,34,51,0.08)",
                LinesDefault = "rgba(26,34,51,0.08)",
            },
            PaletteDark = new PaletteDark
            {
                Primary = primary,
                PrimaryDarken = primaryDarken,
                Secondary = secondary,
                AppbarBackground = "rgba(20,24,34,0.72)",
                AppbarText = "#EDF1F8",
                Background = "#12151D",
                BackgroundGray = "#171B25",
                Surface = "#1B2029",
                DrawerBackground = "rgba(20,24,34,0.65)",
                DrawerText = "#EDF1F8",
                TextPrimary = "#EDF1F8",
                TextSecondary = "#9AA4B8",
                Success = "#35C990",
                Warning = "#F0B458",
                Error = "#F16368",
                Info = "#4FB1EE",
                Divider = "rgba(237,241,248,0.08)",
                LinesDefault = "rgba(237,241,248,0.08)",
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "14px",
                DrawerWidthLeft = "268px",
                AppbarHeight = "64px",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["Inter", "Segoe UI", "Helvetica Neue", "Arial", "sans-serif"]
                }
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
