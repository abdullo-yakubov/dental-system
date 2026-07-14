using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Prescriptions;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Prescriptions;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetVisitPrescriptionsQuery(Guid VisitId) : IQuery<IReadOnlyList<PrescriptionDto>>;

public sealed class GetVisitPrescriptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetVisitPrescriptionsQuery, Result<IReadOnlyList<PrescriptionDto>>>
{
    public async Task<Result<IReadOnlyList<PrescriptionDto>>> Handle(
        GetVisitPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var prescriptions = await unitOfWork.Prescriptions.GetByVisitIdAsync(request.VisitId, cancellationToken);
        return Result.Success<IReadOnlyList<PrescriptionDto>>(prescriptions.Select(ToDto).ToList());
    }

    internal static PrescriptionDto ToDto(Prescription p) =>
        new(p.Id, p.VisitId, p.MedicationName, p.Dosage, p.Instructions, p.CreatedAt);
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreatePrescriptionCommand(
    Guid VisitId,
    string MedicationName,
    string Dosage,
    string? Instructions) : ICommand<PrescriptionDto>;

public sealed class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.MedicationName).NotEmpty().MaximumLength(200).WithMessage("Укажите название лекарства.");
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(200).WithMessage("Укажите дозировку.");
    }
}

public sealed class CreatePrescriptionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePrescriptionCommand, Result<PrescriptionDto>>
{
    public async Task<Result<PrescriptionDto>> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var visit = await unitOfWork.Visits.GetByIdAsync(request.VisitId, cancellationToken);
        if (visit is null)
            return Result.Failure<PrescriptionDto>(Error.NotFound("Visit.NotFound", "Визит не найден."));

        Prescription prescription;
        try
        {
            prescription = Prescription.Create(request.VisitId, request.MedicationName, request.Dosage, request.Instructions);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<PrescriptionDto>(Error.Validation("Prescription.InvalidData", ex.Message));
        }

        await unitOfWork.Prescriptions.AddAsync(prescription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PRESCRIPTION_CREATED", "Prescription", prescription.Id, $"VisitId: {request.VisitId}"), cancellationToken);

        return Result.Success(GetVisitPrescriptionsQueryHandler.ToDto(prescription));
    }
}

// ---------------------------------------------------------------------------

public sealed record DeletePrescriptionCommand(Guid Id) : ICommand;

public sealed class DeletePrescriptionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePrescriptionCommand, Result>
{
    public async Task<Result> Handle(DeletePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await unitOfWork.Prescriptions.GetByIdAsync(request.Id, cancellationToken);
        if (prescription is null)
            return Result.Failure(Error.NotFound("Prescription.NotFound", "Назначение не найдено."));

        await unitOfWork.Prescriptions.DeleteAsync(prescription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PRESCRIPTION_DELETED", "Prescription", prescription.Id, null), cancellationToken);

        return Result.Success();
    }
}
