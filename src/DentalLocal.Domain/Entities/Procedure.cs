using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class Procedure : Entity
{
    public Guid CategoryId { get; private set; }
    public ProcedureCategory Category { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
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
        bool requiresTooth)
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
            DurationMinutes = durationMinutes,
            RequiresTooth = requiresTooth,
            IsActive = true,
        };
    }

    public void Update(Guid categoryId, string name, string? description, decimal price, int durationMinutes, bool requiresTooth)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название процедуры обязательно.", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Цена процедуры не может быть отрицательной.");

        CategoryId = categoryId;
        Name = name.Trim();
        Description = description;
        Price = price;
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
