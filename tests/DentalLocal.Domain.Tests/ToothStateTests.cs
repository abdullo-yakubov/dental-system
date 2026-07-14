using DentalLocal.Domain.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Tests;

public class ToothStateTests
{
    [Fact]
    public void Create_SetsPatientToothNumberAndState()
    {
        var patientId = Guid.NewGuid();

        var tooth = ToothState.Create(patientId, 11, ToothStateType.Healthy);

        Assert.Equal(patientId, tooth.PatientId);
        Assert.Equal((short)11, tooth.ToothNumber);
        Assert.Equal(ToothStateType.Healthy, tooth.State);
    }

    [Fact]
    public void ChangeState_UpdatesStateAndReturnsPreviousState()
    {
        var tooth = ToothState.Create(Guid.NewGuid(), 21, ToothStateType.Healthy);

        var previousState = tooth.ChangeState(ToothStateType.Caries);

        Assert.Equal(ToothStateType.Healthy, previousState);
        Assert.Equal(ToothStateType.Caries, tooth.State);
    }

    [Fact]
    public void ChangeState_CalledTwice_ReturnsStateFromImmediatelyBeforeEachChange()
    {
        var tooth = ToothState.Create(Guid.NewGuid(), 36, ToothStateType.Caries);

        var firstPrevious = tooth.ChangeState(ToothStateType.Filling);
        var secondPrevious = tooth.ChangeState(ToothStateType.Crown);

        Assert.Equal(ToothStateType.Caries, firstPrevious);
        Assert.Equal(ToothStateType.Filling, secondPrevious);
        Assert.Equal(ToothStateType.Crown, tooth.State);
    }
}

public class ToothStateHistoryTests
{
    [Fact]
    public void Create_ImmutableRecord_CapturesTransitionAndVisitLink()
    {
        var patientId = Guid.NewGuid();
        var visitId = Guid.NewGuid();

        var entry = ToothStateHistory.Create(
            patientId,
            toothNumber: 46,
            oldState: ToothStateType.Healthy,
            newState: ToothStateType.RootCanalTreated,
            visitId: visitId,
            comment: "Пульпит, лечение каналов");

        Assert.Equal(patientId, entry.PatientId);
        Assert.Equal((short)46, entry.ToothNumber);
        Assert.Equal(ToothStateType.Healthy, entry.OldState);
        Assert.Equal(ToothStateType.RootCanalTreated, entry.NewState);
        Assert.Equal(visitId, entry.VisitId);
        Assert.Equal("Пульпит, лечение каналов", entry.Comment);
        Assert.True(entry is IImmutableRecord);
    }

    [Fact]
    public void Create_WithoutVisitOrComment_AllowsNullValues()
    {
        var entry = ToothStateHistory.Create(
            Guid.NewGuid(),
            toothNumber: 18,
            oldState: ToothStateType.Caries,
            newState: ToothStateType.Extracted,
            visitId: null,
            comment: null);

        Assert.Null(entry.VisitId);
        Assert.Null(entry.Comment);
    }
}
