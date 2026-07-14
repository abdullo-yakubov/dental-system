using DentalLocal.Application.Features.Prescriptions;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;

namespace DentalLocal.Application.Tests.Features.Prescriptions;

public class CreatePrescriptionCommandHandlerTests
{
    private static Visit CreateVisit() =>
        Visit.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);

    [Fact]
    public async Task Handle_ValidRequest_CreatesPrescriptionLinkedToVisit()
    {
        var uow = new FakeUnitOfWork();
        var visit = CreateVisit();
        uow.VisitsData.Add(visit);

        var handler = new CreatePrescriptionCommandHandler(uow);
        var command = new CreatePrescriptionCommand(visit.Id, "Амоксиклав", "875+125 мг", "2 раза в день, 7 дней");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var prescription = Assert.Single(uow.PrescriptionsData);
        Assert.Equal(visit.Id, prescription.VisitId);
        Assert.Equal("Амоксиклав", prescription.MedicationName);
    }

    [Fact]
    public async Task Handle_VisitNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new CreatePrescriptionCommandHandler(uow);
        var command = new CreatePrescriptionCommand(Guid.NewGuid(), "Ибупрофен", "400 мг", null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.NotFound", result.Error.Code);
        Assert.Empty(uow.PrescriptionsData);
    }
}

public class DeletePrescriptionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingPrescription_RemovesItFromStore()
    {
        var uow = new FakeUnitOfWork();
        var prescription = Prescription.Create(Guid.NewGuid(), "Амоксиклав", "875+125 мг", null);
        uow.PrescriptionsData.Add(prescription);

        var handler = new DeletePrescriptionCommandHandler(uow);
        var result = await handler.Handle(new DeletePrescriptionCommand(prescription.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(uow.PrescriptionsData);
    }

    [Fact]
    public async Task Handle_PrescriptionNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new DeletePrescriptionCommandHandler(uow);
        var result = await handler.Handle(new DeletePrescriptionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Prescription.NotFound", result.Error.Code);
    }
}
