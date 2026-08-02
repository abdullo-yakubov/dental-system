namespace DentalLocal.Shared.Contracts.Auth;

/// <summary>Код показывается ровно один раз в момент генерации — сервер хранит только его хэш.</summary>
public sealed record GenerateRecoveryCodeResponse(string RecoveryCode);
