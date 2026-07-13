using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

/// <summary>Журнал критических операций. Только вставка, изменение и удаление запрещены.</summary>
public class AuditLog : Entity, IImmutableRecord
{
    public DateTimeOffset OccurredAt { get; private set; }
    public string Action { get; private set; } = null!;
    public string? Entity { get; private set; }
    public Guid? EntityId { get; private set; }

    /// <summary>Произвольные детали события в формате JSON (маппится на jsonb).</summary>
    public string? DetailsJson { get; private set; }

    private AuditLog()
    {
    }

    public static AuditLog Create(string action, string? entity, Guid? entityId, string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Действие обязательно для записи аудита.", nameof(action));

        return new AuditLog
        {
            Action = action,
            Entity = entity,
            EntityId = entityId,
            DetailsJson = detailsJson,
            OccurredAt = DateTimeOffset.UtcNow,
        };
    }
}
