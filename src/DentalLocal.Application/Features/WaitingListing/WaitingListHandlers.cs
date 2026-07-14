using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.WaitingList;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.WaitingListing;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetWaitingListQuery(string? Status) : IQuery<IReadOnlyList<WaitingListEntryDto>>;

public sealed class GetWaitingListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetWaitingListQuery, Result<IReadOnlyList<WaitingListEntryDto>>>
{
    public async Task<Result<IReadOnlyList<WaitingListEntryDto>>> Handle(
        GetWaitingListQuery request, CancellationToken cancellationToken)
    {
        var entries = await unitOfWork.WaitingList.GetAllAsync(request.Status, cancellationToken);
        return Result.Success<IReadOnlyList<WaitingListEntryDto>>(entries.Select(ToDto).ToList());
    }

    internal static WaitingListEntryDto ToDto(WaitingList w) =>
        new(w.Id, w.PatientId, w.Patient.FullName, w.DesiredDateFrom, w.DesiredDateTo,
            w.ProcedureId, w.Procedure?.Name, w.Notes, w.Status, w.CreatedAt);
}

// ---------------------------------------------------------------------------

public sealed record GetPatientWaitingListQuery(Guid PatientId) : IQuery<IReadOnlyList<WaitingListEntryDto>>;

public sealed class GetPatientWaitingListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPatientWaitingListQuery, Result<IReadOnlyList<WaitingListEntryDto>>>
{
    public async Task<Result<IReadOnlyList<WaitingListEntryDto>>> Handle(
        GetPatientWaitingListQuery request, CancellationToken cancellationToken)
    {
        var entries = await unitOfWork.WaitingList.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return Result.Success<IReadOnlyList<WaitingListEntryDto>>(entries.Select(w =>
            new WaitingListEntryDto(w.Id, w.PatientId, string.Empty, w.DesiredDateFrom, w.DesiredDateTo,
                w.ProcedureId, null, w.Notes, w.Status, w.CreatedAt)).ToList());
    }
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreateWaitingListEntryCommand(
    Guid PatientId,
    DateOnly DesiredDateFrom,
    DateOnly DesiredDateTo,
    Guid? ProcedureId,
    string? Notes) : ICommand<WaitingListEntryDto>;

public sealed class CreateWaitingListEntryCommandValidator : AbstractValidator<CreateWaitingListEntryCommand>
{
    public CreateWaitingListEntryCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DesiredDateTo).GreaterThanOrEqualTo(x => x.DesiredDateFrom)
            .WithMessage("Конечная дата не может быть раньше начальной.");
    }
}

public sealed class CreateWaitingListEntryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateWaitingListEntryCommand, Result<WaitingListEntryDto>>
{
    public async Task<Result<WaitingListEntryDto>> Handle(
        CreateWaitingListEntryCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<WaitingListEntryDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        WaitingList entry;
        try
        {
            entry = WaitingList.Create(request.PatientId, request.DesiredDateFrom, request.DesiredDateTo, request.ProcedureId, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<WaitingListEntryDto>(Error.Validation("WaitingList.InvalidData", ex.Message));
        }

        await unitOfWork.WaitingList.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("WAITING_LIST_CREATED", "WaitingList", entry.Id, null), cancellationToken);

        return Result.Success(new WaitingListEntryDto(
            entry.Id, entry.PatientId, patient.FullName, entry.DesiredDateFrom, entry.DesiredDateTo,
            entry.ProcedureId, null, entry.Notes, entry.Status, entry.CreatedAt));
    }
}

// ---------------------------------------------------------------------------

public sealed record UpdateWaitingListStatusCommand(Guid Id, string Status) : ICommand<WaitingListEntryDto>;

public sealed class UpdateWaitingListStatusCommandValidator : AbstractValidator<UpdateWaitingListStatusCommand>
{
    private static readonly string[] AllowedStatuses = ["Waiting", "Scheduled", "Cancelled"];

    public UpdateWaitingListStatusCommandValidator()
    {
        RuleFor(x => x.Status).Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"Статус должен быть одним из: {string.Join(", ", AllowedStatuses)}.");
    }
}

public sealed class UpdateWaitingListStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateWaitingListStatusCommand, Result<WaitingListEntryDto>>
{
    public async Task<Result<WaitingListEntryDto>> Handle(
        UpdateWaitingListStatusCommand request, CancellationToken cancellationToken)
    {
        var entry = await unitOfWork.WaitingList.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
            return Result.Failure<WaitingListEntryDto>(Error.NotFound("WaitingList.NotFound", "Запись не найдена."));

        entry.UpdateStatus(request.Status);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("WAITING_LIST_STATUS_UPDATED", "WaitingList", entry.Id, $"Status: {request.Status}"), cancellationToken);

        var patient = await unitOfWork.Patients.GetByIdAsync(entry.PatientId, cancellationToken);

        return Result.Success(new WaitingListEntryDto(
            entry.Id, entry.PatientId, patient?.FullName ?? string.Empty, entry.DesiredDateFrom, entry.DesiredDateTo,
            entry.ProcedureId, null, entry.Notes, entry.Status, entry.CreatedAt));
    }
}
