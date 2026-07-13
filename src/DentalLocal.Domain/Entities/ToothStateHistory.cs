using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Entities;

/// <summary>Неудаляемая история изменений состояния зуба.</summary>
public class ToothStateHistory : Entity, IImmutableRecord
{
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public short ToothNumber { get; private set; }
    public ToothStateType OldState { get; private set; }
    public ToothStateType NewState { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public Guid? VisitId { get; private set; }
    public string? Comment { get; private set; }

    private ToothStateHistory()
    {
    }

    public static ToothStateHistory Create(
        Guid patientId,
        short toothNumber,
        ToothStateType oldState,
        ToothStateType newState,
        Guid? visitId,
        string? comment)
    {
        return new ToothStateHistory
        {
            PatientId = patientId,
            ToothNumber = toothNumber,
            OldState = oldState,
            NewState = newState,
            VisitId = visitId,
            Comment = comment,
            ChangedAt = DateTimeOffset.UtcNow,
        };
    }
}
