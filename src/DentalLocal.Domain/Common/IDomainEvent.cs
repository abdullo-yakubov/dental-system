namespace DentalLocal.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
