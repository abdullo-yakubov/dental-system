namespace DentalLocal.Shared.Contracts.Auth;

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt);
