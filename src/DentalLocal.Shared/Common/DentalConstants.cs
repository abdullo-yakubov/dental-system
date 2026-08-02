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

    /// <summary>Все допустимые номера зубов по FDI (постоянные + молочные) — используется вместо
    /// «диапазона 11-85», который ошибочно пропускает несуществующие номера вроде 19, 20, 29, 30
    /// (см. промпт 28 аудита).</summary>
    public static readonly IReadOnlySet<short> AllToothNumbers =
        PermanentToothNumbers.Concat(DeciduousToothNumbers).ToHashSet();

    /// <summary>
    /// Молочный зуб → постоянный зуб в той же анатомической позиции (см. промпт 31 аудита — смена
    /// прикуса). Вторые и третьи постоянные моляры (16-18, 26-28, 36-38, 46-48) предшественника не
    /// имеют — они прорезываются без замены молочного зуба, поэтому в словаре их нет.
    /// </summary>
    public static readonly IReadOnlyDictionary<short, short> DeciduousPredecessorByPermanentTooth =
        new Dictionary<short, short>
        {
            [11] = 51, [12] = 52, [13] = 53, [14] = 54, [15] = 55,
            [21] = 61, [22] = 62, [23] = 63, [24] = 64, [25] = 65,
            [31] = 71, [32] = 72, [33] = 73, [34] = 74, [35] = 75,
            [41] = 81, [42] = 82, [43] = 83, [44] = 84, [45] = 85,
        };

    public const string DefaultCurrency = "TJS";
    public const string DefaultAccessCode = "111111";
    public const int MinAccessCodeLength = 4;
    public const int MaxLoginAttempts = 5;
    public const int LoginLockoutSeconds = 30;
    public const int DefaultBackupRetentionDays = 90;
    public const string DefaultBackupTime = "21:00";
}
