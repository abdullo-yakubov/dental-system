using DentalLocal.Application.Features.Warranties;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;

namespace DentalLocal.Application.Tests.Features.Warranties;

public class GetPatientWarrantiesQueryHandlerTests
{
    private static Procedure CreateProcedure(string name, int warrantyMonths) =>
        Procedure.Create(Guid.NewGuid(), name, null, 500_000m, 60, true, warrantyMonths: warrantyMonths);

    [Fact]
    public async Task Handle_PatientWithWarranty_ReturnsDtoWithProcedureNameAndCorrectEndDate()
    {
        var uow = new FakeUnitOfWork();
        var patientId = Guid.NewGuid();
        var visitId = Guid.NewGuid();
        var procedure = CreateProcedure("Коронка металлокерамическая", 12);
        var startDate = new DateOnly(2026, 1, 10);
        var warranty = Warranty.Create(patientId, visitId, procedure.Id, 26, startDate, 12, null);

        uow.ProceduresData.Add(procedure);
        uow.WarrantiesData.Add(warranty);

        var handler = new GetPatientWarrantiesQueryHandler(uow);
        var result = await handler.Handle(new GetPatientWarrantiesQuery(patientId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal("Коронка металлокерамическая", dto.ProcedureName);
        Assert.Equal(new DateOnly(2027, 1, 10), dto.EndDate);
    }

    [Fact]
    public async Task Handle_PatientWithoutWarranties_ReturnsEmptyList()
    {
        var uow = new FakeUnitOfWork();
        var handler = new GetPatientWarrantiesQueryHandler(uow);

        var result = await handler.Handle(new GetPatientWarrantiesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}

public class GetToothWarrantiesQueryHandlerTests
{
    [Fact]
    public async Task Handle_FiltersWarrantiesByToothNumber()
    {
        var uow = new FakeUnitOfWork();
        var patientId = Guid.NewGuid();
        var procedure = Procedure.Create(Guid.NewGuid(), "Пломбирование", null, 250_000m, 40, true, warrantyMonths: 6);
        uow.ProceduresData.Add(procedure);

        var matchingWarranty = Warranty.Create(patientId, Guid.NewGuid(), procedure.Id, 36, new DateOnly(2026, 3, 1), 6, null);
        var otherToothWarranty = Warranty.Create(patientId, Guid.NewGuid(), procedure.Id, 47, new DateOnly(2026, 3, 1), 6, null);
        uow.WarrantiesData.Add(matchingWarranty);
        uow.WarrantiesData.Add(otherToothWarranty);

        var handler = new GetToothWarrantiesQueryHandler(uow);
        var result = await handler.Handle(new GetToothWarrantiesQuery(patientId, 36), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal((short)36, dto.ToothNumber);
    }
}
