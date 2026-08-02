using DentalLocal.Application.Features.Patients;
using DentalLocal.Application.Features.Payments;
using DentalLocal.Application.Features.Procedures;
using DentalLocal.Application.Features.Visits;
using DentalLocal.Integration.Tests.TestSupport;
using Gender = DentalLocal.Domain.Enums.Gender;
using PaymentMethod = DentalLocal.Shared.Enums.PaymentMethod;
using PaymentKind = DentalLocal.Shared.Enums.PaymentKind;

namespace DentalLocal.Integration.Tests.Features;

[Collection(DatabaseCollection.Name)]
public class VisitPaymentIntegrationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task CreateVisit_ThenPartialThenFullPayment_ClosesDebt()
    {
        using var scope = fixture.CreateScope();
        var mediator = fixture.CreateMediator(scope);
        var phone = $"93{Random.Shared.Next(1000000, 9999999)}";

        var patient = (await mediator.Send(new CreatePatientCommand(
            "Петров", "Пётр", null, Gender.Male, new DateOnly(1985, 3, 10),
            phone, null, null, null, null, null, null))).Value;

        var category = (await mediator.Send(new CreateProcedureCategoryCommand("Терапия-Тест", "#FF0000"))).Value;
        var procedure = (await mediator.Send(new CreateProcedureCommand(
            "Пломба-Тест", null, 100_000m, 30, category.Id, RequiresTooth: true))).Value;

        var visitResult = await mediator.Send(new CreateVisitCommand(
            patient.Id, DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(9, 0), "Курбонали",
            null, null, null,
            [new CreateVisitItemCommand(procedure.Id, 11, 100_000m, 1, null)]));

        Assert.True(visitResult.IsSuccess);
        var visit = visitResult.Value;
        Assert.Equal(100_000m, visit.TotalCost);
        Assert.Equal(0m, visit.TotalPaid);

        var partialPayment = await mediator.Send(new RegisterPaymentCommand(
            visit.Id, patient.Id, 40_000m, PaymentMethod.Cash, PaymentKind.AtTreatment, null));
        Assert.True(partialPayment.IsSuccess);

        var fullPayment = await mediator.Send(new RegisterPaymentCommand(
            visit.Id, patient.Id, 60_000m, PaymentMethod.Cash, PaymentKind.DebtRepayment, "остаток"));
        Assert.True(fullPayment.IsSuccess);

        var finalVisit = await mediator.Send(new GetVisitByIdQuery(visit.Id));
        Assert.True(finalVisit.IsSuccess);
        Assert.Equal(100_000m, finalVisit.Value.TotalPaid);
        Assert.Equal(0m, finalVisit.Value.RemainingAmount);
    }
}
