using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Entities;

/// <summary>Актуальное состояние одного зуба пациента. Уникально по (PatientId, ToothNumber).</summary>
public class ToothState : Entity
{
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public short ToothNumber { get; private set; }
    public ToothStateType State { get; private set; }
    public uint Version { get; private set; }

    private ToothState()
    {
    }

    public static ToothState Create(Guid patientId, short toothNumber, ToothStateType state)
    {
        return new ToothState
        {
            PatientId = patientId,
            ToothNumber = toothNumber,
            State = state,
        };
    }

    public ToothStateType ChangeState(ToothStateType newState)
    {
        var oldState = State;
        State = newState;
        Touch();
        return oldState;
    }
}
