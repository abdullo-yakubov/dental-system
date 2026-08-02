using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Teeth;
using DentalLocal.Shared.Enums;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Teeth;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetPatientOdontogramQuery(Guid PatientId) : IQuery<OdontogramDto>;

public sealed class GetPatientOdontogramQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPatientOdontogramQuery, Result<OdontogramDto>>
{
    public async Task<Result<OdontogramDto>> Handle(
        GetPatientOdontogramQuery request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<OdontogramDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var teeth = await unitOfWork.ToothStates.GetByPatientIdAsync(request.PatientId, cancellationToken);
        var history = await unitOfWork.ToothStateHistory.GetByPatientIdAsync(request.PatientId, cancellationToken);
        var surfaceHistory = await unitOfWork.ToothSurfaceHistory.GetByPatientIdAsync(request.PatientId, cancellationToken);

        var recentHistory = history.OrderByDescending(h => h.ChangedAt).Take(20).ToList();

        var toothNumberById = teeth.ToDictionary(t => t.Id, t => t.ToothNumber);
        var surfaceStates = surfaceHistory
            .GroupBy(h => (h.ToothId, h.SurfaceCode))
            .Select(g => g.OrderByDescending(h => h.ChangedAt).First())
            .Where(h => toothNumberById.ContainsKey(h.ToothId))
            .Select(h => new ToothSurfaceStateDto(toothNumberById[h.ToothId], h.SurfaceCode, (Shared.Enums.ToothStateType)(short)h.Status))
            .ToList();

        return Result.Success(new OdontogramDto(
            request.PatientId,
            mapper.Map<IReadOnlyList<ToothStateDto>>(teeth),
            mapper.Map<IReadOnlyList<ToothStateHistoryDto>>(recentHistory),
            surfaceStates));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetSurfaceHistoryQuery(Guid PatientId, short ToothNumber)
    : IQuery<IReadOnlyList<ToothTimelineEntryDto>>;

public sealed class GetSurfaceHistoryQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSurfaceHistoryQuery, Result<IReadOnlyList<ToothTimelineEntryDto>>>
{
    public async Task<Result<IReadOnlyList<ToothTimelineEntryDto>>> Handle(
        GetSurfaceHistoryQuery request, CancellationToken cancellationToken)
    {
        var tooth = await unitOfWork.ToothStates.GetAsync(request.PatientId, request.ToothNumber, cancellationToken);

        var toothHistory = await unitOfWork.ToothStateHistory.GetByToothAsync(
            request.PatientId, request.ToothNumber, cancellationToken);

        var entries = toothHistory
            .Select(h => new ToothTimelineEntryDto(
                h.ToothNumber, null, (Shared.Enums.ToothStateType)(short)h.NewState, null, null, null,
                h.VisitId, h.ChangedAt, h.Comment))
            .ToList();

        if (tooth is not null)
        {
            var surfaceHistory = await unitOfWork.ToothSurfaceHistory.GetByToothIdAsync(tooth.Id, cancellationToken);
            entries.AddRange(surfaceHistory.Select(h => new ToothTimelineEntryDto(
                request.ToothNumber, h.SurfaceCode, (Shared.Enums.ToothStateType)(short)h.Status, h.Diagnosis,
                h.Icd10Code, h.Icd11Code, h.VisitId, h.ChangedAt, h.DoctorComment)));
        }

        return Result.Success<IReadOnlyList<ToothTimelineEntryDto>>(
            entries.OrderByDescending(e => e.ChangedAt).ToList());
    }
}

// ---------------------------------------------------------------------------

public sealed record GetToothHistoryQuery(Guid PatientId, short ToothNumber)
    : IQuery<IReadOnlyList<ToothStateHistoryDto>>;

public sealed class GetToothHistoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetToothHistoryQuery, Result<IReadOnlyList<ToothStateHistoryDto>>>
{
    public async Task<Result<IReadOnlyList<ToothStateHistoryDto>>> Handle(
        GetToothHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await unitOfWork.ToothStateHistory.GetByToothAsync(
            request.PatientId, request.ToothNumber, cancellationToken);

        return Result.Success(mapper.Map<IReadOnlyList<ToothStateHistoryDto>>(history));
    }
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record UpdateToothStatusCommand(
    Guid PatientId,
    short ToothNumber,
    Shared.Enums.ToothStateType NewState,
    Guid? VisitId,
    string? Comment) : ICommand<ToothStateDto>;

public sealed class UpdateToothStatusCommandValidator : AbstractValidator<UpdateToothStatusCommand>
{
    public UpdateToothStatusCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ToothNumber).Must(n => DentalConstants.AllToothNumbers.Contains(n))
            .WithMessage("Некорректный номер зуба по FDI.");
    }
}

public sealed class UpdateToothStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateToothStatusCommand, Result<ToothStateDto>>
{
    public async Task<Result<ToothStateDto>> Handle(
        UpdateToothStatusCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<ToothStateDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var domainState = (Domain.Enums.ToothStateType)(short)request.NewState;

        var tooth = await unitOfWork.ToothStates.GetAsync(request.PatientId, request.ToothNumber, cancellationToken);
        if (tooth is null)
        {
            // First record for this tooth
            tooth = ToothState.Create(request.PatientId, request.ToothNumber, domainState);
            await unitOfWork.ToothStates.AddAsync(tooth, cancellationToken);
        }
        else
        {
            var oldState = tooth.ChangeState(domainState);
            var historyEntry = ToothStateHistory.Create(
                request.PatientId, request.ToothNumber,
                oldState, domainState,
                request.VisitId, request.Comment);

            await unitOfWork.ToothStateHistory.AddAsync(historyEntry, cancellationToken);
        }

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("TOOTH_UPDATED", "ToothState", tooth.Id, null), cancellationToken);

        await ExfoliateDeciduousPredecessorAsync(request.PatientId, request.ToothNumber, request.VisitId, unitOfWork, cancellationToken);

        return Result.Success(mapper.Map<ToothStateDto>(tooth));
    }

    /// <summary>
    /// Смена прикуса (см. промпт 31 аудита): если для зуба, который сейчас обновили, есть молочный
    /// предшественник в той же позиции (11↔51 и т.д.) и у него ещё есть запись в ToothStates, не
    /// помеченная Exfoliated/Extracted/Missing — считаем, что он выпал, и переводим его в Exfoliated
    /// с записью в историю. Одонтограмма не должна показывать активными молочный и постоянный зуб
    /// одной позиции одновременно.
    /// </summary>
    private static async Task ExfoliateDeciduousPredecessorAsync(
        Guid patientId, short toothNumber, Guid? visitId, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        if (!DentalConstants.DeciduousPredecessorByPermanentTooth.TryGetValue(toothNumber, out var predecessorNumber))
            return;

        var predecessor = await unitOfWork.ToothStates.GetAsync(patientId, predecessorNumber, cancellationToken);
        if (predecessor is null)
            return;

        if (predecessor.State is Domain.Enums.ToothStateType.Exfoliated
            or Domain.Enums.ToothStateType.Extracted
            or Domain.Enums.ToothStateType.Missing)
        {
            return;
        }

        var oldState = predecessor.ChangeState(Domain.Enums.ToothStateType.Exfoliated);
        await unitOfWork.ToothStateHistory.AddAsync(
            ToothStateHistory.Create(
                patientId, predecessorNumber, oldState, Domain.Enums.ToothStateType.Exfoliated,
                visitId, "Автоматически: сменился постоянным зубом."),
            cancellationToken);
    }
}

// ---------------------------------------------------------------------------

public sealed record AddToothSurfaceRecordCommand(
    Guid PatientId,
    short ToothNumber,
    string SurfaceCode,
    Shared.Enums.ToothStateType Status,
    string? Diagnosis,
    string? Icd10Code,
    string? Icd11Code,
    Guid? VisitId,
    string? DoctorComment) : ICommand<ToothSurfaceHistoryDto>;

public sealed class AddToothSurfaceRecordCommandValidator : AbstractValidator<AddToothSurfaceRecordCommand>
{
    // "I" (режущий край) добавлен для 3D-одонтограммы (§6/§7 ТЗ на 3D) — 2D-режим
    // по-прежнему всегда передаёт "O" и для окклюзионной, и для режущей поверхности.
    private static readonly string[] ValidSurfaceCodes = ["M", "D", "O", "I", "B", "L"];

    public AddToothSurfaceRecordCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ToothNumber).Must(n => DentalConstants.AllToothNumbers.Contains(n))
            .WithMessage("Некорректный номер зуба по FDI.");
        RuleFor(x => x.SurfaceCode).Must(c => ValidSurfaceCodes.Contains(c))
            .WithMessage("Некорректный код поверхности (допустимо: M, D, O, I, B, L).");
        RuleFor(x => x.Diagnosis).MaximumLength(255);
    }
}

public sealed class AddToothSurfaceRecordCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<AddToothSurfaceRecordCommand, Result<ToothSurfaceHistoryDto>>
{
    public async Task<Result<ToothSurfaceHistoryDto>> Handle(
        AddToothSurfaceRecordCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<ToothSurfaceHistoryDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var domainStatus = (Domain.Enums.ToothStateType)(short)request.Status;

        var tooth = await unitOfWork.ToothStates.GetAsync(request.PatientId, request.ToothNumber, cancellationToken);

        // Клиническая осмысленность (см. промпт 29 аудита): на удалённый/отсутствующий зуб нельзя
        // завести новую лечебную запись — только явный перевод в Implant (зуб восстановлен имплантом).
        if (tooth is not null
            && tooth.State is Domain.Enums.ToothStateType.Extracted or Domain.Enums.ToothStateType.Missing
            && domainStatus != Domain.Enums.ToothStateType.Implant)
        {
            return Result.Failure<ToothSurfaceHistoryDto>(Error.Validation(
                "ToothSurface.ToothNotTreatable",
                "Зуб отмечен как удалённый/отсутствующий — лечение возможно только через перевод в статус «Имплант»."));
        }

        if (tooth is null)
        {
            tooth = ToothState.Create(request.PatientId, request.ToothNumber, domainStatus);
            await unitOfWork.ToothStates.AddAsync(tooth, cancellationToken);
        }
        else if (tooth.State != domainStatus)
        {
            var oldState = tooth.ChangeState(domainStatus);
            await unitOfWork.ToothStateHistory.AddAsync(
                ToothStateHistory.Create(request.PatientId, request.ToothNumber, oldState, domainStatus, request.VisitId, request.DoctorComment),
                cancellationToken);
        }

        var surfaceRecord = ToothSurfaceHistory.Create(
            tooth.Id, request.SurfaceCode, domainStatus,
            request.Diagnosis, request.Icd10Code, request.Icd11Code,
            request.VisitId, request.DoctorComment);

        await unitOfWork.ToothSurfaceHistory.AddAsync(surfaceRecord, cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("TOOTH_SURFACE_UPDATED", "ToothSurfaceHistory", surfaceRecord.Id, null), cancellationToken);

        return Result.Success(new ToothSurfaceHistoryDto(
            surfaceRecord.Id, request.ToothNumber, surfaceRecord.SurfaceCode,
            (Shared.Enums.ToothStateType)(short)surfaceRecord.Status, surfaceRecord.Diagnosis,
            surfaceRecord.Icd10Code, surfaceRecord.Icd11Code, surfaceRecord.VisitId,
            surfaceRecord.ChangedAt, surfaceRecord.DoctorComment));
    }
}
