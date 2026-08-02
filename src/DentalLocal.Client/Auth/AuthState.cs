namespace DentalLocal.Client.Auth;

/// <summary>
/// Токен сессии хранится только в памяти WASM-приложения (не в localStorage/sessionStorage) — см. п.17 ТЗ:
/// перезагрузка страницы требует повторного входа по коду доступа.
/// </summary>
public sealed class AuthState
{
    public string? Token { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    public bool IsAuthenticated => Token is not null && ExpiresAt is { } expiresAt && expiresAt > DateTimeOffset.UtcNow;

    public event Action? Changed;

    public void SetSession(string token, DateTimeOffset expiresAt)
    {
        Token = token;
        ExpiresAt = expiresAt;
        _unauthorizedHandled = false;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Token = null;
        ExpiresAt = null;
        Changed?.Invoke();
    }

    /// <summary>
    /// Single-flight обработка 401 (см. промпт 43 аудита): несколько параллельных запросов, у которых
    /// протух токен, получат 401 одновременно — без этой защёлки каждый из них дёрнул бы Clear() и
    /// событие Changed по кругу. Возвращает true только для первого вызова после успешного входа —
    /// вызывающий код (BearerTokenHandler) должен разлогинить пользователя тогда и только тогда.
    /// </summary>
    private bool _unauthorizedHandled;

    public bool TryHandleUnauthorizedOnce()
    {
        if (_unauthorizedHandled)
            return false;

        _unauthorizedHandled = true;
        Clear();
        return true;
    }
}
