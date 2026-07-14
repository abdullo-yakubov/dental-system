namespace DentalLocal.Application.Common;

/// <summary>Ключи IMemoryCache для редко изменяемых справочников (раздел 12 ТЗ).</summary>
public static class CacheKeys
{
    public const string Procedures = "cache:procedures";
    public const string ProcedureCategories = "cache:procedure-categories";
    public const string Icd10Codes = "cache:icd10-codes";
    public const string Icd11Codes = "cache:icd11-codes";
    public const string Settings = "cache:settings";
}
