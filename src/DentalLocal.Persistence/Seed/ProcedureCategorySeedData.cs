namespace DentalLocal.Persistence.Seed;

internal static class ProcedureCategorySeedData
{
    public static readonly (string Name, string Color, int SortOrder)[] Categories =
    [
        ("Диагностика", "#2196F3", 1),
        ("Терапия", "#4CAF50", 2),
        ("Хирургия", "#F44336", 3),
        ("Ортопедия", "#9C27B0", 4),
        ("Ортодонтия", "#FF9800", 5),
        ("Эндодонтия", "#795548", 6),
        ("Гигиена и профилактика", "#00BCD4", 7),
        ("Эстетика", "#E91E63", 8),
    ];
}
