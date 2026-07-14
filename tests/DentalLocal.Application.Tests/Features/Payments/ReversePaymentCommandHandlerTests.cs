using AutoMapper;
using DentalLocal.Application.Features.Payments;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Application.Tests.Features.Payments;

public class ReversePaymentCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    [Fact]
    public async Task Handle_PaymentNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new ReversePaymentCommandHandler(uow, CreateMapper());

        var result = await handler.Handle(new ReversePaymentCommand(Guid.NewGuid(), "ошибка"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_AlreadyReversedPayment_ReturnsConflict()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var visit = Visit.Create(patient.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);
        visit.AddItem(Guid.NewGuid(), null, 100_000m, 1, null);
        var original = Payment.Create(visit.Id, patient.Id, 50_000m, PaymentMethod.Cash, PaymentKind.AtTreatment, null);
        var alreadyReversed = Payment.CreateReversal(original, "первое сторно");
        uow.VisitsData.Add(visit);
        uow.PatientsData.Add(patient);
        uow.PaymentsData.Add(alreadyReversed);

        var handler = new ReversePaymentCommandHandler(uow, CreateMapper());
        var result = await handler.Handle(new ReversePaymentCommand(alreadyReversed.Id, "повторное сторно"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.AlreadyReversed", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ValidPayment_RestoresVisitBalanceAndReopensDebt()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var visit = Visit.Create(patient.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);
        visit.AddItem(Guid.NewGuid(), null, 100_000m, 1, null);
        visit.ApplyPayment(100_000m); // визит полностью оплачен одним платежом

        var payment = Payment.Create(visit.Id, patient.Id, 100_000m, PaymentMethod.Cash, PaymentKind.AtTreatment, null);
        var debt = Debt.Create(visit.Id, patient.Id, 100_000m);
        debt.ApplyPayment(100_000m); // долг закрыт той же оплатой

        uow.VisitsData.Add(visit);
        uow.PatientsData.Add(patient);
        uow.PaymentsData.Add(payment);
        uow.DebtsData.Add(debt);

        var handler = new ReversePaymentCommandHandler(uow, CreateMapper());
        var result = await handler.Handle(new ReversePaymentCommand(payment.Id, "оплата внесена ошибочно"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, visit.TotalPaid);
        Assert.Equal(DebtStatus.Open, debt.Status);
        Assert.Equal(100_000m, debt.RemainingAmount);
        Assert.Null(debt.ClosedAt);

        var reversal = uow.PaymentsData.Single(p => p.ReversalOfPaymentId == payment.Id);
        Assert.Equal(payment.Amount, reversal.Amount);
    }
}
