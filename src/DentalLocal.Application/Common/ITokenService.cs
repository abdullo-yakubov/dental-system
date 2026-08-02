namespace DentalLocal.Application.Common;

/// <summary>Выпуск и проверка локального bearer-токена сессии (см. п.11, 17 ТЗ).</summary>
public interface ITokenService
{
    Task<(string Token, DateTimeOffset ExpiresAt)> IssueAsync(CancellationToken cancellationToken = default);

    Task<bool> ValidateAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Короткоживущая подпись для доступа к файлу пациента по прямой ссылке (например, из
    /// &lt;img src&gt;, который не может передать заголовок Authorization) — см. промпт 11 аудита:
    /// файлы больше не отдаются анонимно через PhysicalFileProvider.
    /// </summary>
    Task<(string Signature, DateTimeOffset ExpiresAt)> IssueFileLinkAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task<bool> ValidateFileLinkAsync(Guid fileId, long expiryUnixSeconds, string signature, CancellationToken cancellationToken = default);
}
