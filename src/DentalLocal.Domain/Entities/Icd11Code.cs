using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

/// <summary>Справочная запись МКБ-11 (стоматологические разделы, см. п. 9 ТЗ).</summary>
public class Icd11Code : Entity
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private Icd11Code()
    {
    }

    public static Icd11Code Create(string code, string description) =>
        new() { Code = code, Description = description };
}
