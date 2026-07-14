namespace DentalLocal.Persistence.Seed;

internal static class ProcedureCategorySeedData
{
    public static readonly (string Name, string Color, int SortOrder)[] Categories =
    [
        ("Терапевтическое лечение", "#4CAF50", 1),
        ("Ортопедическое лечение", "#9C27B0", 2),
        ("Хирургическое лечение", "#F44336", 3),
        ("Ортодонтия", "#FF9800", 4),
        ("Детская стоматология лечение", "#E91E63", 5),
        ("Имплантация", "#2196F3", 6),
        ("Детская хирургия", "#FF5722", 7),
        ("Гигиена", "#00BCD4", 8),
        ("Анестезия", "#607D8B", 9)
    ];
}
