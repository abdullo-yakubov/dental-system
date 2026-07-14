using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Procedures;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Procedures;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetProceduresQuery(bool IncludeInactive = false) : IQuery<IReadOnlyList<ProcedureDto>>;

public sealed class GetProceduresQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetProceduresQuery, Result<IReadOnlyList<ProcedureDto>>>
{
    public async Task<Result<IReadOnlyList<ProcedureDto>>> Handle(
        GetProceduresQuery request, CancellationToken cancellationToken)
    {
        var procedures = await unitOfWork.Procedures.GetAllAsync(request.IncludeInactive, cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<ProcedureDto>>(procedures));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetProcedureByIdQuery(Guid Id) : IQuery<ProcedureDto>;

public sealed class GetProcedureByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetProcedureByIdQuery, Result<ProcedureDto>>
{
    public async Task<Result<ProcedureDto>> Handle(GetProcedureByIdQuery request, CancellationToken cancellationToken)
    {
        var procedure = await unitOfWork.Procedures.GetByIdAsync(request.Id, cancellationToken);
        if (procedure is null)
            return Result.Failure<ProcedureDto>(Error.NotFound("Procedure.NotFound", "Процедура не найдена."));

        return Result.Success(mapper.Map<ProcedureDto>(procedure));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetProcedureCategoriesQuery : IQuery<IReadOnlyList<ProcedureCategoryDto>>;

public sealed class GetProcedureCategoriesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetProcedureCategoriesQuery, Result<IReadOnlyList<ProcedureCategoryDto>>>
{
    public async Task<Result<IReadOnlyList<ProcedureCategoryDto>>> Handle(
        GetProcedureCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await unitOfWork.ProcedureCategories.GetAllAsync(cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<ProcedureCategoryDto>>(categories));
    }
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreateProcedureCommand(
    string Name,
    string? Description,
    decimal Price,
    int DurationMinutes,
    Guid CategoryId,
    bool RequiresTooth,
    bool IsActive = true) : ICommand<ProcedureDto>;

public sealed class CreateProcedureCommandValidator : AbstractValidator<CreateProcedureCommand>
{
    public CreateProcedureCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("Укажите название процедуры.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Цена не может быть отрицательной.");
        RuleFor(x => x.DurationMinutes).GreaterThan(0).WithMessage("Длительность должна быть больше 0.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Укажите категорию процедуры.");
    }
}

public sealed class CreateProcedureCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateProcedureCommand, Result<ProcedureDto>>
{
    public async Task<Result<ProcedureDto>> Handle(CreateProcedureCommand request, CancellationToken cancellationToken)
    {
        var category = await unitOfWork.ProcedureCategories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
            return Result.Failure<ProcedureDto>(Error.NotFound("ProcedureCategory.NotFound", "Категория не найдена."));

        Procedure procedure;
        try
        {
            procedure = Procedure.Create(
                request.CategoryId, request.Name, request.Description,
                request.Price, request.DurationMinutes, request.RequiresTooth);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProcedureDto>(Error.Validation("Procedure.InvalidData", ex.Message));
        }

        await unitOfWork.Procedures.AddAsync(procedure, cancellationToken);
        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PROCEDURE_CREATED", "Procedure", procedure.Id, null), cancellationToken);

        return Result.Success(mapper.Map<ProcedureDto>(procedure));
    }
}

// ---------------------------------------------------------------------------

public sealed record UpdateProcedureCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int DurationMinutes,
    Guid CategoryId,
    bool RequiresTooth,
    bool IsActive) : ICommand<ProcedureDto>;

public sealed class UpdateProcedureCommandValidator : AbstractValidator<UpdateProcedureCommand>
{
    public UpdateProcedureCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
    }
}

public sealed class UpdateProcedureCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateProcedureCommand, Result<ProcedureDto>>
{
    public async Task<Result<ProcedureDto>> Handle(UpdateProcedureCommand request, CancellationToken cancellationToken)
    {
        var procedure = await unitOfWork.Procedures.GetByIdAsync(request.Id, cancellationToken);
        if (procedure is null)
            return Result.Failure<ProcedureDto>(Error.NotFound("Procedure.NotFound", "Процедура не найдена."));

        try
        {
            procedure.Update(request.CategoryId, request.Name, request.Description,
                request.Price, request.DurationMinutes, request.RequiresTooth);

            if (!request.IsActive) procedure.Deactivate();
            else procedure.Activate();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProcedureDto>(Error.Validation("Procedure.InvalidData", ex.Message));
        }

        return Result.Success(mapper.Map<ProcedureDto>(procedure));
    }
}

// ---------------------------------------------------------------------------

public sealed record CreateProcedureCategoryCommand(
    string Name,
    string Color,
    int SortOrder = 0) : ICommand<ProcedureCategoryDto>;

public sealed class CreateProcedureCategoryCommandValidator : AbstractValidator<CreateProcedureCategoryCommand>
{
    public CreateProcedureCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("Укажите название категории.");
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Цвет должен быть в формате HEX (#RRGGBB).");
    }
}

public sealed class CreateProcedureCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateProcedureCategoryCommand, Result<ProcedureCategoryDto>>
{
    public async Task<Result<ProcedureCategoryDto>> Handle(
        CreateProcedureCategoryCommand request, CancellationToken cancellationToken)
    {
        ProcedureCategory category;
        try
        {
            category = ProcedureCategory.Create(request.Name, request.Color, request.SortOrder);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProcedureCategoryDto>(Error.Validation("ProcedureCategory.InvalidData", ex.Message));
        }

        await unitOfWork.ProcedureCategories.AddAsync(category, cancellationToken);
        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PROCEDURE_CATEGORY_CREATED", "ProcedureCategory", category.Id, null), cancellationToken);

        return Result.Success(mapper.Map<ProcedureCategoryDto>(category));
    }
}
