using DentalLocal.Application.Features.WaitingListing;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;
using DomainEnums = DentalLocal.Domain.Enums;

namespace DentalLocal.Application.Tests.Features.WaitingListing;

public class CreateWaitingListEntryCommandHandlerTests
{
    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, DomainEnums.Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    [Fact]
    public async Task Handle_ValidRequest_CreatesEntryWithWaitingStatus()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        uow.PatientsData.Add(patient);

        var handler = new CreateWaitingListEntryCommandHandler(uow);
        var command = new CreateWaitingListEntryCommand(
            patient.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 10), null, "любой день утром");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(uow.WaitingListData);
        Assert.Equal("Waiting", entry.Status);
    }

    [Fact]
    public async Task Handle_PatientNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new CreateWaitingListEntryCommandHandler(uow);
        var command = new CreateWaitingListEntryCommand(
            Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 10), null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Patient.NotFound", result.Error.Code);
    }
}

public class UpdateWaitingListStatusCommandHandlerTests
{
    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, DomainEnums.Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    [Fact]
    public async Task Handle_ExistingEntry_UpdatesStatus()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var entry = WaitingList.Create(patient.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 10), null, null);
        uow.PatientsData.Add(patient);
        uow.WaitingListData.Add(entry);

        var handler = new UpdateWaitingListStatusCommandHandler(uow);
        var result = await handler.Handle(new UpdateWaitingListStatusCommand(entry.Id, "Scheduled"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Scheduled", entry.Status);
    }

    [Fact]
    public async Task Handle_EntryNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new UpdateWaitingListStatusCommandHandler(uow);
        var result = await handler.Handle(new UpdateWaitingListStatusCommand(Guid.NewGuid(), "Cancelled"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("WaitingList.NotFound", result.Error.Code);
    }
}
