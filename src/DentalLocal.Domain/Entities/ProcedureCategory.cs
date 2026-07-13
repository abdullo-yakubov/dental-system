using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class ProcedureCategory : Entity
{
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public int SortOrder { get; private set; }

    private readonly List<Procedure> _procedures = [];
    public IReadOnlyCollection<Procedure> Procedures => _procedures.AsReadOnly();

    private ProcedureCategory()
    {
    }

    public static ProcedureCategory Create(string name, string color, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название категории обязательно.", nameof(name));

        if (string.IsNullOrWhiteSpace(color) || color[0] != '#' || color.Length != 7)
            throw new ArgumentException("Цвет категории должен быть в формате HEX (#RRGGBB).", nameof(color));

        return new ProcedureCategory
        {
            Name = name.Trim(),
            Color = color,
            SortOrder = sortOrder,
        };
    }

    public void Update(string name, string color, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название категории обязательно.", nameof(name));

        Name = name.Trim();
        Color = color;
        SortOrder = sortOrder;
        Touch();
    }
}
