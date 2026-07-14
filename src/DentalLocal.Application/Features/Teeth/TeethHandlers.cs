using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
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

        var recentHistory = history.OrderByDescending(h => h.ChangedAt).Take(20).ToList();

        return Result.Success(new OdontogramDto(
            request.PatientId,
            mapper.Map<IReadOnlyList<ToothStateDto>>(teeth),
            mapper.Map<IReadOnlyList<ToothStateHistoryDto>>(recentHistory)));
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
        RuleFor(x => x.ToothNumber).InclusiveBetween((short)11, (short)85)
            .WithMessage("Некорректный номер зуба (FDI: 11-85).");
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

        return Result.Success(mapper.Map<ToothStateDto>(tooth));
    }
}
