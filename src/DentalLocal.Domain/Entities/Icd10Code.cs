using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

/// <summary>Справочная запись МКБ-10 (стоматологические разделы K00-K14, см. п. 9 ТЗ).</summary>
public class Icd10Code : Entity
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private Icd10Code()
    {
    }

    public static Icd10Code Create(string code, string description) =>
        new() { Code = code, Description = description };
}
