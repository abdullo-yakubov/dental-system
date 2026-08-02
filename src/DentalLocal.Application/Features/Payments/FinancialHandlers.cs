using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Payments;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Enums;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Payments;

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record RegisterPaymentCommand(
    Guid VisitId,
    Guid PatientId,
    decimal Amount,
    Shared.Enums.PaymentMethod Method,
    Shared.Enums.PaymentKind Kind,
    string? Note) : ICommand<PaymentDto>;

public sealed class RegisterPaymentCommandValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentCommandValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Сумма должна быть больше нуля.");
    }
}

public sealed class RegisterPaymentCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<RegisterPaymentCommand, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var visit = await unitOfWork.Visits.GetByIdWithItemsAsync(request.VisitId, cancellationToken);
        if (visit is null)
            return Result.Failure<PaymentDto>(Error.NotFound("Visit.NotFound", "Визит не найден."));

        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<PaymentDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        // Map Shared enums to Domain enums
        var domainMethod = (Domain.Enums.PaymentMethod)(short)request.Method;
        var domainKind = (Domain.Enums.PaymentKind)(short)request.Kind;

        // Start transaction manually since pipeline behavior transaction might be configured,
        // but let's make sure it's secure. The transaction behavior will handle DB save/commit.
        Payment payment;
        try
        {
            visit.ApplyPayment(request.Amount);

            var debt = await unitOfWork.Debts.GetByVisitIdAsync(request.VisitId, cancellationToken);
            if (debt is not null)
            {
                debt.ApplyPayment(request.Amount);
            }
            else if (visit.RemainingAmount > 0)
            {
                // Create debt if not exists but remaining unpaid amount exists
                var newDebt = Debt.Create(visit.Id, visit.PatientId, visit.RemainingAmount);
                await unitOfWork.Debts.AddAsync(newDebt, cancellationToken);
            }

            payment = Payment.Create(
                request.VisitId,
                request.PatientId,
                request.Amount,
                domainMethod,
                domainKind,
                request.Note);

            await unitOfWork.Payments.AddAsync(payment, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or Domain.Exceptions.DomainException)
        {
            return Result.Failure<PaymentDto>(Error.Validation("Payment.InvalidAction", ex.Message));
        }

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PAYMENT_REGISTERED", "Payment", payment.Id, $"Amount: {payment.Amount}"),
            cancellationToken);

        // Fetch registered payment to ensure tracking
        var dbPayment = await unitOfWork.Payments.GetByIdAsync(payment.Id, cancellationToken);
        return Result.Success(mapper.Map<PaymentDto>(dbPayment));
    }
}

// ---------------------------------------------------------------------------

public sealed record ReversePaymentCommand(Guid PaymentId, string? Reason) : ICommand<PaymentDto>;

public sealed class ReversePaymentCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<ReversePaymentCommand, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(ReversePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.Payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure<PaymentDto>(Error.NotFound("Payment.NotFound", "Платеж не найден."));

        if (payment.ReversalOfPaymentId.HasValue)
            return Result.Failure<PaymentDto>(Error.Conflict("Payment.AlreadyReversed", "Этот платеж уже сторнирован или сам является сторно-платежом."));

        Payment reversal;
        try
        {
            reversal = Payment.CreateReversal(payment, $"Сторно: {request.Reason}");

            // Visit.ReversePayment/Debt.ReversePayment — доменные методы, симметричные ApplyPayment,
            // с той же проверкой инвариантов (раньше здесь была рефлексия по приватным сеттерам,
            // позволявшая увести TotalPaid/RemainingAmount в некорректное значение без единой проверки).
            var visit = await unitOfWork.Visits.GetByIdWithItemsAsync(payment.VisitId, cancellationToken);
            visit?.ReversePayment(payment.Amount);

            var debt = await unitOfWork.Debts.GetByVisitIdAsync(payment.VisitId, cancellationToken);
            debt?.ReversePayment(payment.Amount);

            await unitOfWork.Payments.AddAsync(reversal, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or DentalLocal.Domain.Exceptions.DomainException)
        {
            return Result.Failure<PaymentDto>(Error.Validation("Payment.ReversalFailed", ex.Message));
        }

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PAYMENT_REVERSED", "Payment", payment.Id, $"Reversed by: {reversal.Id}"),
            cancellationToken);

        return Result.Success(mapper.Map<PaymentDto>(reversal));
    }
}

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetPatientPaymentsQuery(Guid PatientId) : IQuery<IReadOnlyList<PaymentDto>>;

public sealed class GetPatientPaymentsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPatientPaymentsQuery, Result<IReadOnlyList<PaymentDto>>>
{
    public async Task<Result<IReadOnlyList<PaymentDto>>> Handle(
        GetPatientPaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await unitOfWork.Payments.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<PaymentDto>>(payments));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetPatientDebtsQuery(Guid PatientId) : IQuery<IReadOnlyList<DebtDto>>;

public sealed class GetPatientDebtsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPatientDebtsQuery, Result<IReadOnlyList<DebtDto>>>
{
    public async Task<Result<IReadOnlyList<DebtDto>>> Handle(
        GetPatientDebtsQuery request, CancellationToken cancellationToken)
    {
        var debts = await unitOfWork.Debts.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<DebtDto>>(debts));
    }
}

// ---------------------------------------------------------------------------

public sealed record SearchDebtsQuery(
    Shared.Enums.DebtStatus? Status,
    string? Search,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<DebtListItemDto>>;

public sealed class SearchDebtsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<SearchDebtsQuery, Result<PagedResult<DebtListItemDto>>>
{
    public async Task<Result<PagedResult<DebtListItemDto>>> Handle(
        SearchDebtsQuery request, CancellationToken cancellationToken)
    {
        Domain.Enums.DebtStatus? domainStatus = request.Status.HasValue 
            ? (Domain.Enums.DebtStatus)(short)request.Status.Value 
            : null;

        var (debts, totalCount) = await unitOfWork.Debts.SearchAsync(
            domainStatus,
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = mapper.Map<IReadOnlyList<DebtListItemDto>>(debts);
        return Result.Success(PagedResult<DebtListItemDto>.Create(dtos, totalCount, request.Page, request.PageSize));
    }
}
