using System.Text.Json;
using Microsoft.JSInterop;

namespace DentalLocal.Client.Services;

/// <summary>
/// Автосохранение черновиков форм в localStorage (см. ТЗ раздел 10: autosave раз в 10 сек,
/// очистка при успешном сохранении в БД). Используется как переиспользуемый JS-interop мост,
/// конкретные формы сами решают, что и когда сохранять/восстанавливать.
/// </summary>
public sealed class DraftAutosaveService(IJSRuntime js)
{
    public async Task SaveAsync<T>(string key, T draft)
    {
        var json = JsonSerializer.Serialize(draft);
        await js.InvokeVoidAsync("dentalDraft.set", key, json);
    }

    public async Task<T?> LoadAsync<T>(string key)
    {
        var json = await js.InvokeAsync<string?>("dentalDraft.get", key);
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public async Task ClearAsync(string key)
    {
        await js.InvokeVoidAsync("dentalDraft.remove", key);
    }
}

/// <summary>
/// Периодически (раз в 10 сек, как того требует ТЗ) вызывает переданный делегат сохранения черновика,
/// пока не будет остановлен/удалён. Используется внутри диалогов форм на время их открытия.
/// </summary>
public sealed class DraftAutosaveTimer : IAsyncDisposable
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(10));
    private readonly Task _loop;

    public DraftAutosaveTimer(Func<Task> saveDraftAsync)
    {
        _loop = RunAsync(saveDraftAsync);
    }

    private async Task RunAsync(Func<Task> saveDraftAsync)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync())
            {
                await saveDraftAsync();
            }
        }
        catch (ObjectDisposedException)
        {
            // Timer disposed — normal shutdown path.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _timer.Dispose();
        try
        {
            await _loop;
        }
        catch
        {
            // Ignore errors from the final loop iteration during disposal.
        }
    }
}
