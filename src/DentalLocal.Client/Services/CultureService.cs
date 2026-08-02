using Microsoft.JSInterop;

namespace DentalLocal.Client.Services;

/// <summary>
/// Персистентный выбор языка интерфейса (ru/tj/en, см. Settings.razor) — хранится в localStorage
/// и применяется к CultureInfo при старте (см. Program.cs), т.к. WASM должен знать культуру
/// до первой отрисовки, чтобы подгрузить нужную сателлитную сборку .resx.
/// </summary>
public static class CultureService
{
    public const string StorageKey = "dental-culture";

    public static readonly Dictionary<string, string> SupportedCultures = new()
    {
        ["ru"] = "ru-RU",
        ["tj"] = "tg-TJ",
        ["en"] = "en-US"
    };

    public static async Task<string?> GetStoredCultureAsync(IJSRuntime js)
    {
        try
        {
            return await js.InvokeAsync<string?>("dentalTheme.get", StorageKey);
        }
        catch
        {
            return null;
        }
    }

    public static async Task SetStoredCultureAsync(IJSRuntime js, string languageCode)
    {
        try
        {
            await js.InvokeVoidAsync("dentalTheme.set", StorageKey, languageCode);
        }
        catch
        {
            // no-op
        }
    }
}
