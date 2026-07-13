namespace DentalLocal.Shared.Common;

public static class DentalConstants
{
    /// <summary>Номера постоянных зубов по FDI: 11-18, 21-28, 31-38, 41-48.</summary>
    public static readonly IReadOnlyList<short> PermanentToothNumbers =
        Enumerable.Range(11, 8).Concat(Enumerable.Range(21, 8))
            .Concat(Enumerable.Range(31, 8)).Concat(Enumerable.Range(41, 8))
            .Select(n => (short)n)
            .ToList();

    /// <summary>Номера молочных зубов по FDI: 55-51, 61-65, 85-81, 71-75.</summary>
    public static readonly IReadOnlyList<short> DeciduousToothNumbers =
        Enumerable.Range(51, 5).Concat(Enumerable.Range(61, 5))
            .Concat(Enumerable.Range(71, 5)).Concat(Enumerable.Range(81, 5))
            .Select(n => (short)n)
            .ToList();

    public const string DefaultCurrency = "TJS";
    public const string DefaultAccessCode = "1998";
    public const int MinAccessCodeLength = 4;
    public const int MaxLoginAttempts = 5;
    public const int LoginLockoutSeconds = 30;
    public const int DefaultBackupRetentionDays = 90;
    public const string DefaultBackupTime = "21:00";
}
