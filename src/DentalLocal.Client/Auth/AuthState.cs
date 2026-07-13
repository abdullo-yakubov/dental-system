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
        Changed?.Invoke();
    }

    public void Clear()
    {
        Token = null;
        ExpiresAt = null;
        Changed?.Invoke();
    }
}
