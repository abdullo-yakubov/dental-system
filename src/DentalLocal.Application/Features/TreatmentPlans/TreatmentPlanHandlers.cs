using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.TreatmentPlans;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.TreatmentPlans;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetPatientTreatmentPlansQuery(Guid PatientId) : IQuery<IReadOnlyList<TreatmentPlanDto>>;

public sealed class GetPatientTreatmentPlansQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPatientTreatmentPlansQuery, Result<IReadOnlyList<TreatmentPlanDto>>>
{
    public async Task<Result<IReadOnlyList<TreatmentPlanDto>>> Handle(
        GetPatientTreatmentPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await unitOfWork.TreatmentPlans.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<TreatmentPlanDto>>(plans));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetTreatmentPlanByIdQuery(Guid Id) : IQuery<TreatmentPlanDto>;

public sealed class GetTreatmentPlanByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetTreatmentPlanByIdQuery, Result<TreatmentPlanDto>>
{
    public async Task<Result<TreatmentPlanDto>> Handle(GetTreatmentPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await unitOfWork.TreatmentPlans.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("TreatmentPlan.NotFound", "План лечения не найден."));

        return Result.Success(mapper.Map<TreatmentPlanDto>(plan));
    }
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreateTreatmentPlanCommand(Guid PatientId, string Title, string? Notes) : ICommand<TreatmentPlanDto>;

public sealed class CreateTreatmentPlanCommandValidator : AbstractValidator<CreateTreatmentPlanCommand>
{
    public CreateTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255).WithMessage("Укажите название плана лечения.");
    }
}

public sealed class CreateTreatmentPlanCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateTreatmentPlanCommand, Result<TreatmentPlanDto>>
{
    public async Task<Result<TreatmentPlanDto>> Handle(CreateTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var plan = TreatmentPlan.Create(request.PatientId, request.Title, request.Notes);

        await unitOfWork.TreatmentPlans.AddAsync(plan, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("TREATMENT_PLAN_CREATED", "TreatmentPlan", plan.Id, null), cancellationToken);

        return Result.Success(mapper.Map<TreatmentPlanDto>(plan));
    }
}

// ---------------------------------------------------------------------------

public sealed record UpdateTreatmentPlanCommand(Guid Id, string Title, string? Notes) : ICommand<TreatmentPlanDto>;

public sealed class UpdateTreatmentPlanCommandValidator : AbstractValidator<UpdateTreatmentPlanCommand>
{
    public UpdateTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
    }
}

public sealed class UpdateTreatmentPlanCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateTreatmentPlanCommand, Result<TreatmentPlanDto>>
{
    public async Task<Result<TreatmentPlanDto>> Handle(UpdateTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await unitOfWork.TreatmentPlans.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("TreatmentPlan.NotFound", "План лечения не найден."));

        try
        {
            plan.Update(request.Title, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<TreatmentPlanDto>(Error.Validation("TreatmentPlan.InvalidData", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(mapper.Map<TreatmentPlanDto>(plan));
    }
}

// ---------------------------------------------------------------------------

public sealed record AddTreatmentPlanItemCommand(
    Guid TreatmentPlanId,
    Guid ProcedureId,
    short? ToothNumber,
    decimal EstimatedPrice,
    int OrderIndex) : ICommand<TreatmentPlanDto>;

public sealed class AddTreatmentPlanItemCommandValidator : AbstractValidator<AddTreatmentPlanItemCommand>
{
    public AddTreatmentPlanItemCommandValidator()
    {
        RuleFor(x => x.ProcedureId).NotEmpty();
        RuleFor(x => x.EstimatedPrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddTreatmentPlanItemCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<AddTreatmentPlanItemCommand, Result<TreatmentPlanDto>>
{
    public async Task<Result<TreatmentPlanDto>> Handle(AddTreatmentPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await unitOfWork.TreatmentPlans.GetByIdWithItemsAsync(request.TreatmentPlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("TreatmentPlan.NotFound", "План лечения не найден."));

        var procedure = await unitOfWork.Procedures.GetByIdAsync(request.ProcedureId, cancellationToken);
        if (procedure is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("Procedure.NotFound", "Процедура не найдена."));

        try
        {
            plan.AddItem(request.ProcedureId, request.ToothNumber, request.EstimatedPrice, request.OrderIndex);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<TreatmentPlanDto>(Error.Validation("TreatmentPlan.InvalidItem", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var reloaded = await unitOfWork.TreatmentPlans.GetByIdWithItemsAsync(request.TreatmentPlanId, cancellationToken);
        return Result.Success(mapper.Map<TreatmentPlanDto>(reloaded));
    }
}

// ---------------------------------------------------------------------------

public sealed record ActivateTreatmentPlanCommand(Guid Id) : ICommand<TreatmentPlanDto>;

public sealed class ActivateTreatmentPlanCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<ActivateTreatmentPlanCommand, Result<TreatmentPlanDto>>
{
    public async Task<Result<TreatmentPlanDto>> Handle(ActivateTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await unitOfWork.TreatmentPlans.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("TreatmentPlan.NotFound", "План лечения не найден."));

        try
        {
            plan.Activate();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TreatmentPlanDto>(Error.Validation("TreatmentPlan.InvalidTransition", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(mapper.Map<TreatmentPlanDto>(plan));
    }
}

// ---------------------------------------------------------------------------

public sealed record CancelTreatmentPlanCommand(Guid Id) : ICommand<TreatmentPlanDto>;

public sealed class CancelTreatmentPlanCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CancelTreatmentPlanCommand, Result<TreatmentPlanDto>>
{
    public async Task<Result<TreatmentPlanDto>> Handle(CancelTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await unitOfWork.TreatmentPlans.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("TreatmentPlan.NotFound", "План лечения не найден."));

        try
        {
            plan.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TreatmentPlanDto>(Error.Validation("TreatmentPlan.InvalidTransition", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("TREATMENT_PLAN_CANCELLED", "TreatmentPlan", plan.Id, null), cancellationToken);

        return Result.Success(mapper.Map<TreatmentPlanDto>(plan));
    }
}

// ---------------------------------------------------------------------------

public sealed record CompleteTreatmentPlanItemCommand(Guid TreatmentPlanId, Guid ItemId, Guid VisitId) : ICommand<TreatmentPlanDto>;

public sealed class CompleteTreatmentPlanItemCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CompleteTreatmentPlanItemCommand, Result<TreatmentPlanDto>>
{
    public async Task<Result<TreatmentPlanDto>> Handle(CompleteTreatmentPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await unitOfWork.TreatmentPlans.GetByIdWithItemsAsync(request.TreatmentPlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("TreatmentPlan.NotFound", "План лечения не найден."));

        var visit = await unitOfWork.Visits.GetByIdAsync(request.VisitId, cancellationToken);
        if (visit is null)
            return Result.Failure<TreatmentPlanDto>(Error.NotFound("Visit.NotFound", "Визит не найден."));

        try
        {
            plan.CompleteItem(request.ItemId, request.VisitId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TreatmentPlanDto>(Error.Validation("TreatmentPlan.InvalidItem", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("TREATMENT_PLAN_ITEM_COMPLETED", "TreatmentPlan", plan.Id, $"ItemId: {request.ItemId}, VisitId: {request.VisitId}"),
            cancellationToken);

        return Result.Success(mapper.Map<TreatmentPlanDto>(plan));
    }
}
