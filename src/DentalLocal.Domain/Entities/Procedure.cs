using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class Procedure : Entity
{
    public Guid CategoryId { get; private set; }
    public ProcedureCategory Category { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public decimal PriceMin { get; private set; }
    public decimal PriceMax { get; private set; }
    public string? Specialty { get; private set; }
    public bool RequiresConsent { get; private set; }
    public int WarrantyMonths { get; private set; }

    /// <summary>Через сколько дней после визита предложить контрольный визит (0/null — не предлагать). См. п.17.13 ТЗ.</summary>
    public int? FollowUpDays { get; private set; }
    public int DurationMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public bool RequiresTooth { get; private set; }

    private Procedure()
    {
    }

    public static Procedure Create(
        Guid categoryId,
        string name,
        string? description,
        decimal price,
        int durationMinutes,
        bool requiresTooth,
        decimal priceMin = 0,
        decimal priceMax = 0,
        string? specialty = null,
        bool requiresConsent = false,
        int warrantyMonths = 0,
        int? followUpDays = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название процедуры обязательно.", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Цена процедуры не может быть отрицательной.");

        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Длительность процедуры должна быть положительной.");

        return new Procedure
        {
            CategoryId = categoryId,
            Name = name.Trim(),
            Description = description,
            Price = price,
            PriceMin = priceMin,
            PriceMax = priceMax,
            Specialty = specialty,
            RequiresConsent = requiresConsent,
            WarrantyMonths = warrantyMonths,
            FollowUpDays = followUpDays,
            DurationMinutes = durationMinutes,
            RequiresTooth = requiresTooth,
            IsActive = true,
        };
    }

    public void Update(
        Guid categoryId,
        string name,
        string? description,
        decimal price,
        int durationMinutes,
        bool requiresTooth,
        decimal priceMin = 0,
        decimal priceMax = 0,
        string? specialty = null,
        bool requiresConsent = false,
        int warrantyMonths = 0,
        int? followUpDays = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название процедуры обязательно.", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Цена процедуры не может быть отрицательной.");

        CategoryId = categoryId;
        Name = name.Trim();
        Description = description;
        Price = price;
        PriceMin = priceMin;
        PriceMax = priceMax;
        Specialty = specialty;
        RequiresConsent = requiresConsent;
        WarrantyMonths = warrantyMonths;
        FollowUpDays = followUpDays;
        DurationMinutes = durationMinutes;
        RequiresTooth = requiresTooth;
        Touch();
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "Цена процедуры не может быть отрицательной.");

        Price = newPrice;
        Touch();
    }

    /// <summary>Деактивация вместо физического удаления — процедура могла использоваться в истории лечения.</summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }
}
