using AutoMapper;
using DentalLocal.Application.Features.Payments;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Application.Tests.Features.Payments;

public class RegisterPaymentCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    private static Visit CreateCompletedVisitWithCost(Guid patientId, decimal cost)
    {
        var visit = Visit.Create(patientId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);
        visit.AddItem(Guid.NewGuid(), null, cost, 1, null);
        return visit;
    }

    [Fact]
    public async Task Handle_VisitNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new RegisterPaymentCommandHandler(uow, CreateMapper());
        var command = new RegisterPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 10_000m,
            Shared.Enums.PaymentMethod.Cash, Shared.Enums.PaymentKind.AtTreatment, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_PatientNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var visit = CreateCompletedVisitWithCost(patient.Id, 100_000m);
        uow.VisitsData.Add(visit);
        // умышленно не добавляем пациента в uow.PatientsData

        var handler = new RegisterPaymentCommandHandler(uow, CreateMapper());
        var command = new RegisterPaymentCommand(visit.Id, patient.Id, 10_000m,
            Shared.Enums.PaymentMethod.Cash, Shared.Enums.PaymentKind.AtTreatment, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Patient.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_PartialPayment_CreatesOpenDebtForRemainingAmount()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var visit = CreateCompletedVisitWithCost(patient.Id, 100_000m);
        uow.PatientsData.Add(patient);
        uow.VisitsData.Add(visit);

        var handler = new RegisterPaymentCommandHandler(uow, CreateMapper());
        var command = new RegisterPaymentCommand(visit.Id, patient.Id, 40_000m,
            Shared.Enums.PaymentMethod.Cash, Shared.Enums.PaymentKind.AtTreatment, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(40_000m, visit.TotalPaid);
        var debt = Assert.Single(uow.DebtsData);
        Assert.Equal(60_000m, debt.RemainingAmount);
        Assert.Equal(DebtStatus.Open, debt.Status);
    }

    [Fact]
    public async Task Handle_FullPayment_DoesNotCreateDebt()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var visit = CreateCompletedVisitWithCost(patient.Id, 100_000m);
        uow.PatientsData.Add(patient);
        uow.VisitsData.Add(visit);

        var handler = new RegisterPaymentCommandHandler(uow, CreateMapper());
        var command = new RegisterPaymentCommand(visit.Id, patient.Id, 100_000m,
            Shared.Enums.PaymentMethod.Cash, Shared.Enums.PaymentKind.AtTreatment, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, visit.RemainingAmount);
        Assert.Empty(uow.DebtsData);
    }

    [Fact]
    public async Task Handle_ExistingDebt_IsReducedByPaymentAndClosedWhenPaidOff()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var visit = CreateCompletedVisitWithCost(patient.Id, 100_000m);
        visit.ApplyPayment(30_000m);
        var debt = Debt.Create(visit.Id, patient.Id, 70_000m);
        uow.PatientsData.Add(patient);
        uow.VisitsData.Add(visit);
        uow.DebtsData.Add(debt);

        var handler = new RegisterPaymentCommandHandler(uow, CreateMapper());
        var command = new RegisterPaymentCommand(visit.Id, patient.Id, 70_000m,
            Shared.Enums.PaymentMethod.Transfer, Shared.Enums.PaymentKind.DebtRepayment, "погашение долга");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DebtStatus.Closed, debt.Status);
        Assert.Equal(0m, debt.RemainingAmount);
    }

    [Fact]
    public async Task Handle_PaymentExceedsRemainingVisitAmount_ReturnsValidationError()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var visit = CreateCompletedVisitWithCost(patient.Id, 100_000m);
        uow.PatientsData.Add(patient);
        uow.VisitsData.Add(visit);

        var handler = new RegisterPaymentCommandHandler(uow, CreateMapper());
        var command = new RegisterPaymentCommand(visit.Id, patient.Id, 150_000m,
            Shared.Enums.PaymentMethod.Cash, Shared.Enums.PaymentKind.AtTreatment, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InvalidAction", result.Error.Code);
        Assert.Empty(uow.PaymentsData);
    }
}
