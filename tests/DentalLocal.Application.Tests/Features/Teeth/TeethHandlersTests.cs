using AutoMapper;
using DentalLocal.Application.Features.Teeth;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;
using SharedEnums = DentalLocal.Shared.Enums;
using DomainEnums = DentalLocal.Domain.Enums;

namespace DentalLocal.Application.Tests.Features.Teeth;

public class UpdateToothStatusCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, DomainEnums.Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    [Fact]
    public async Task Handle_FirstRecordForTooth_CreatesToothStateWithoutHistory()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        uow.PatientsData.Add(patient);

        var handler = new UpdateToothStatusCommandHandler(uow, CreateMapper());
        var command = new UpdateToothStatusCommand(patient.Id, 11, SharedEnums.ToothStateType.Caries, null, "первичный осмотр");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tooth = Assert.Single(uow.ToothStatesData);
        Assert.Equal(DomainEnums.ToothStateType.Caries, tooth.State);
        Assert.Empty(uow.ToothStateHistoryData);
    }

    [Fact]
    public async Task Handle_ExistingTooth_ChangesStateAndRecordsHistoryEntry()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        uow.PatientsData.Add(patient);
        var tooth = ToothState.Create(patient.Id, 21, DomainEnums.ToothStateType.Healthy);
        uow.ToothStatesData.Add(tooth);

        var visitId = Guid.NewGuid();
        var handler = new UpdateToothStatusCommandHandler(uow, CreateMapper());
        var command = new UpdateToothStatusCommand(patient.Id, 21, SharedEnums.ToothStateType.RootCanalTreated, visitId, "лечение каналов");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DomainEnums.ToothStateType.RootCanalTreated, tooth.State);
        var history = Assert.Single(uow.ToothStateHistoryData);
        Assert.Equal(DomainEnums.ToothStateType.Healthy, history.OldState);
        Assert.Equal(DomainEnums.ToothStateType.RootCanalTreated, history.NewState);
        Assert.Equal(visitId, history.VisitId);
        Assert.Equal("лечение каналов", history.Comment);
    }

    [Fact]
    public async Task Handle_PatientNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new UpdateToothStatusCommandHandler(uow, CreateMapper());
        var command = new UpdateToothStatusCommand(Guid.NewGuid(), 11, SharedEnums.ToothStateType.Caries, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Patient.NotFound", result.Error.Code);
    }
}

public class AddToothSurfaceRecordCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, DomainEnums.Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    [Fact]
    public async Task Handle_NewSurfaceRecord_CreatesToothAndSurfaceHistory()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        uow.PatientsData.Add(patient);

        var handler = new AddToothSurfaceRecordCommandHandler(uow, CreateMapper());
        var command = new AddToothSurfaceRecordCommand(
            patient.Id, 36, "O", SharedEnums.ToothStateType.Filling, "Кариес среднего слоя", "K02.1", null, null, "Пломбирование окклюзионной поверхности");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(uow.ToothStatesData);
        var surface = Assert.Single(uow.ToothSurfaceHistoryData);
        Assert.Equal("O", surface.SurfaceCode);
        Assert.Equal(DomainEnums.ToothStateType.Filling, surface.Status);
        Assert.Equal("K02.1", surface.Icd10Code);
    }

    [Fact]
    public async Task Handle_SameStatusAsCurrentToothState_DoesNotDuplicateToothStateHistory()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        uow.PatientsData.Add(patient);
        var tooth = ToothState.Create(patient.Id, 46, DomainEnums.ToothStateType.Crown);
        uow.ToothStatesData.Add(tooth);

        var handler = new AddToothSurfaceRecordCommandHandler(uow, CreateMapper());
        var command = new AddToothSurfaceRecordCommand(
            patient.Id, 46, "B", SharedEnums.ToothStateType.Crown, null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(uow.ToothStateHistoryData);
        Assert.Single(uow.ToothSurfaceHistoryData);
    }
}
