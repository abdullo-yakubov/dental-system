namespace DentalLocal.Shared.Contracts.Auth;

public sealed record ResetAccessCodeWithRecoveryRequest(string RecoveryCode, string NewCode);
