namespace DentalLocal.Application.Common;

/// <summary>Выпуск и проверка локального bearer-токена сессии (см. п.11, 17 ТЗ).</summary>
public interface ITokenService
{
    Task<(string Token, DateTimeOffset ExpiresAt)> IssueAsync(CancellationToken cancellationToken = default);

    Task<bool> ValidateAsync(string token, CancellationToken cancellationToken = default);
}
