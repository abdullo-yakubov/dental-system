namespace DentalLocal.Domain.Common;

/// <summary>
/// Маркер сущностей, физическое удаление которых запрещено доменными правилами
/// (история лечения, финансов, аудита — см. п. O-6 ТЗ). Persistence отклоняет
/// любую попытку EF-удаления таких сущностей на уровне SaveChanges.
/// </summary>
public interface IImmutableRecord;
