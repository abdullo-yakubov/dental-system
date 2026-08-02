using System.Security.Cryptography;
using System.Text;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLocal.Infrastructure.Auth;

/// <summary>
/// Локальный bearer-токен вида "{expiryUnixSeconds}.{base64(HMAC-SHA256)}", подписанный секретом,
/// сгенерированным при первом запуске и сохранённым в app_settings (см. п.17 ТЗ: срок 12 ч).
/// Секрет кэшируется в процессе — сервис регистрируется как singleton, а зависимые от DbContext
/// репозитории резолвятся через <see cref="IServiceScopeFactory"/> только один раз при первом обращении.
/// </summary>
public sealed class HmacTokenService(IServiceScopeFactory scopeFactory) : ITokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan FileLinkLifetime = TimeSpan.FromMinutes(5);

    private byte[]? _secret;
    private readonly SemaphoreSlim _secretLock = new(1, 1);

    public async Task<(string Token, DateTimeOffset ExpiresAt)> IssueAsync(CancellationToken cancellationToken = default)
    {
        var secret = await GetSecretAsync(cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        return (CreateToken(secret, expiresAt), expiresAt);
    }

    public async Task<bool> ValidateAsync(string token, CancellationToken cancellationToken = default)
    {
        var dotIndex = token.IndexOf('.');
        if (dotIndex <= 0)
            return false;

        if (!long.TryParse(token.AsSpan(0, dotIndex), out var expiryUnixSeconds))
            return false;

        if (DateTimeOffset.FromUnixTimeSeconds(expiryUnixSeconds) < DateTimeOffset.UtcNow)
            return false;

        byte[] providedSignature;
        try
        {
            providedSignature = Convert.FromBase64String(token[(dotIndex + 1)..]);
        }
        catch (FormatException)
        {
            return false;
        }

        var secret = await GetSecretAsync(cancellationToken);
        var expectedSignature = ComputeSignature(secret, expiryUnixSeconds);

        return CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature);
    }

    private static string CreateToken(byte[] secret, DateTimeOffset expiresAt)
    {
        var expiryUnixSeconds = expiresAt.ToUnixTimeSeconds();
        var signature = ComputeSignature(secret, expiryUnixSeconds);

        return $"{expiryUnixSeconds}.{Convert.ToBase64String(signature)}";
    }

    private static byte[] ComputeSignature(byte[] secret, long expiryUnixSeconds) =>
        HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(expiryUnixSeconds.ToString()));

    public async Task<(string Signature, DateTimeOffset ExpiresAt)> IssueFileLinkAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var secret = await GetSecretAsync(cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.Add(FileLinkLifetime);
        var expiryUnixSeconds = expiresAt.ToUnixTimeSeconds();
        var signature = ComputeFileLinkSignature(secret, fileId, expiryUnixSeconds);

        return (Convert.ToBase64String(signature), expiresAt);
    }

    public async Task<bool> ValidateFileLinkAsync(Guid fileId, long expiryUnixSeconds, string signature, CancellationToken cancellationToken = default)
    {
        if (DateTimeOffset.FromUnixTimeSeconds(expiryUnixSeconds) < DateTimeOffset.UtcNow)
            return false;

        byte[] providedSignature;
        try
        {
            providedSignature = Convert.FromBase64String(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        var secret = await GetSecretAsync(cancellationToken);
        var expectedSignature = ComputeFileLinkSignature(secret, fileId, expiryUnixSeconds);

        return CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature);
    }

    private static byte[] ComputeFileLinkSignature(byte[] secret, Guid fileId, long expiryUnixSeconds) =>
        HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes($"file:{fileId:N}:{expiryUnixSeconds}"));

    private async Task<byte[]> GetSecretAsync(CancellationToken cancellationToken)
    {
        if (_secret is not null)
            return _secret;

        await _secretLock.WaitAsync(cancellationToken);
        try
        {
            if (_secret is not null)
                return _secret;

            using var scope = scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var existing = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.AuthTokenSecret, cancellationToken);
            if (existing?.Value is not null)
            {
                _secret = Convert.FromBase64String(existing.Value);
                return _secret;
            }

            var generated = RandomNumberGenerator.GetBytes(32);
            await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.AuthTokenSecret, Convert.ToBase64String(generated), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _secret = generated;
            return _secret;
        }
        finally
        {
            _secretLock.Release();
        }
    }
}
