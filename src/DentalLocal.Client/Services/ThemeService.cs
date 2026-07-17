using DentalLocal.Client.Theming;
using Microsoft.JSInterop;
using MudBlazor;

namespace DentalLocal.Client.Services;

/// <summary>
/// Хранит текущий режим (light/dark) и акцент (blue/teal) интерфейса,
/// синхронизирует их с localStorage и атрибутом data-theme на &lt;html&gt; (используется CSS-переменными вне MudBlazor).
/// </summary>
public sealed class ThemeService
{
    private const string ModeKey = "dental-theme-mode";
    private const string AccentKey = "dental-theme-accent";
    private const string CustomAccentKey = "dental-theme-custom-accent";

    private readonly IJSRuntime _js;

    public bool IsDark { get; private set; }
    public AppAccent Accent { get; private set; } = AppAccent.Blue;

    /// <summary>Произвольный акцентный цвет (hex), заданный через color picker в Настройках. Null — используется стандартный Blue/Teal (раздел 16 ТЗ).</summary>
    public string? CustomAccentColor { get; private set; }

    public MudTheme CurrentTheme => CustomAccentColor is not null
        ? AppThemes.GetCustom(IsDark, CustomAccentColor)
        : AppThemes.Get(IsDark, Accent);

    public event Action? Changed;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        var mode = await GetItemAsync(ModeKey);
        var accent = await GetItemAsync(AccentKey);
        var customAccent = await GetItemAsync(CustomAccentKey);

        IsDark = mode == "dark";
        Accent = accent == "teal" ? AppAccent.Teal : AppAccent.Blue;
        CustomAccentColor = string.IsNullOrWhiteSpace(customAccent) ? null : customAccent;

        await ApplyHtmlAttributesAsync();
        Changed?.Invoke();
    }

    public async Task ToggleModeAsync()
    {
        IsDark = !IsDark;
        await SetItemAsync(ModeKey, IsDark ? "dark" : "light");
        await ApplyHtmlAttributesAsync();
        Changed?.Invoke();
    }

    public async Task SetAccentAsync(AppAccent accent)
    {
        Accent = accent;
        await SetItemAsync(AccentKey, accent == AppAccent.Teal ? "teal" : "blue");
        await ApplyHtmlAttributesAsync();
        Changed?.Invoke();
    }

    /// <summary>Задаёт произвольный акцентный цвет (hex, например "#7C3AED"), имеет приоритет над Blue/Teal.</summary>
    public async Task SetCustomAccentAsync(string hexColor)
    {
        CustomAccentColor = hexColor;
        await SetItemAsync(CustomAccentKey, hexColor);
        await ApplyHtmlAttributesAsync();
        Changed?.Invoke();
    }

    /// <summary>Сбрасывает кастомный акцент — интерфейс возвращается к переключателю Blue/Teal в шапке.</summary>
    public async Task ResetCustomAccentAsync()
    {
        CustomAccentColor = null;
        await SetItemAsync(CustomAccentKey, string.Empty);
        await ApplyHtmlAttributesAsync();
        Changed?.Invoke();
    }

    private async Task ApplyHtmlAttributesAsync()
    {
        try
        {
            var accentAttr = CustomAccentColor ?? (Accent == AppAccent.Teal ? "teal" : "blue");
            await _js.InvokeVoidAsync("dentalTheme.apply", IsDark ? "dark" : "light", accentAttr);
        }
        catch (JSException)
        {
            // игнорируем сбои во время prerender/до готовности DOM
        }
    }

    private async Task<string?> GetItemAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("dentalTheme.get", key);
        }
        catch
        {
            return null;
        }
    }

    private async Task SetItemAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("dentalTheme.set", key, value);
        }
        catch
        {
            // no-op
        }
    }
}
