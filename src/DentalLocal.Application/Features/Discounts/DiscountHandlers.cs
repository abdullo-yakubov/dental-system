using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Discounts;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Discounts;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetPatientDiscountHistoryQuery(Guid PatientId) : IQuery<IReadOnlyList<DiscountHistoryDto>>;

public sealed class GetPatientDiscountHistoryQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPatientDiscountHistoryQuery, Result<IReadOnlyList<DiscountHistoryDto>>>
{
    public async Task<Result<IReadOnlyList<DiscountHistoryDto>>> Handle(
        GetPatientDiscountHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await unitOfWork.DiscountHistory.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return Result.Success<IReadOnlyList<DiscountHistoryDto>>(history.Select(ToDto).ToList());
    }

    internal static DiscountHistoryDto ToDto(DiscountHistory d) =>
        new(d.Id, d.PatientId, d.VisitId, d.DiscountType, d.Amount, d.Percent, d.Reason, d.AppliedAt);
}

// ── Commands ─────────────────────────────────────────────────────────────────

/// <summary>
/// Записывает скидку в неизменяемый журнал DiscountHistory. Не пересчитывает итоговую сумму визита —
/// это ответственность вызывающей стороны (клиент должен применить скидку к цене визита/процедуры до сохранения);
/// это упрощение, чтобы не завязываться на пересчёт Visit.TotalAmount в рамках этой задачи.
/// </summary>
public sealed record ApplyDiscountCommand(
    Guid PatientId,
    Guid? VisitId,
    string DiscountType,
    decimal Amount,
    decimal Percent,
    string? Reason) : ICommand<DiscountHistoryDto>;

public sealed class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
{
    public ApplyDiscountCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DiscountType).NotEmpty().MaximumLength(50).WithMessage("Укажите тип скидки.");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Percent).InclusiveBetween(0, 100);
        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.Amount > 0 || x.Percent > 0)
            .WithMessage("Причина скидки обязательна, если скидка больше нуля.");
    }
}

public sealed class ApplyDiscountCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ApplyDiscountCommand, Result<DiscountHistoryDto>>
{
    public async Task<Result<DiscountHistoryDto>> Handle(ApplyDiscountCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<DiscountHistoryDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        DiscountHistory discount;
        try
        {
            discount = DiscountHistory.Create(
                request.PatientId, request.VisitId, request.DiscountType, request.Amount, request.Percent, request.Reason);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<DiscountHistoryDto>(Error.Validation("Discount.InvalidData", ex.Message));
        }

        await unitOfWork.DiscountHistory.AddAsync(discount, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("DISCOUNT_APPLIED", "DiscountHistory", discount.Id, $"PatientId: {request.PatientId}"), cancellationToken);

        return Result.Success(GetPatientDiscountHistoryQueryHandler.ToDto(discount));
    }
}
