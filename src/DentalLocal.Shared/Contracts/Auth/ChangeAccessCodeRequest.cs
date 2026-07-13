namespace DentalLocal.Shared.Contracts.Auth;

public sealed record ChangeAccessCodeRequest(string OldCode, string NewCode);
