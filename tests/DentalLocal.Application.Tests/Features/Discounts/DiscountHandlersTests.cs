using DentalLocal.Application.Features.Discounts;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;
using DomainEnums = DentalLocal.Domain.Enums;

namespace DentalLocal.Application.Tests.Features.Discounts;

public class ApplyDiscountCommandHandlerTests
{
    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, DomainEnums.Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    [Fact]
    public async Task Handle_ZeroDiscountWithoutReason_Succeeds()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        uow.PatientsData.Add(patient);

        var handler = new ApplyDiscountCommandHandler(uow);
        var command = new ApplyDiscountCommand(patient.Id, null, "Patient", 0m, 0m, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(uow.DiscountHistoryData);
    }

    [Fact]
    public async Task Handle_NonZeroDiscount_RecordsAppendOnlyHistoryEntry()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        uow.PatientsData.Add(patient);

        var handler = new ApplyDiscountCommandHandler(uow);
        var command = new ApplyDiscountCommand(patient.Id, null, "Visit", 0m, 15m, "постоянный клиент");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var discount = Assert.Single(uow.DiscountHistoryData);
        Assert.Equal(15m, discount.Percent);
        Assert.Equal("постоянный клиент", discount.Reason);
    }

    [Fact]
    public async Task Handle_PatientNotFound_ReturnsNotFoundAndDoesNotRecordDiscount()
    {
        var uow = new FakeUnitOfWork();
        var handler = new ApplyDiscountCommandHandler(uow);
        var command = new ApplyDiscountCommand(Guid.NewGuid(), null, "Patient", 0m, 10m, "скидка");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Patient.NotFound", result.Error.Code);
        Assert.Empty(uow.DiscountHistoryData);
    }
}
