using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Materials;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Materials;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetMaterialsQuery(string? Search = null) : IQuery<IReadOnlyList<MaterialDto>>;

public sealed class GetMaterialsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetMaterialsQuery, Result<IReadOnlyList<MaterialDto>>>
{
    public async Task<Result<IReadOnlyList<MaterialDto>>> Handle(
        GetMaterialsQuery request, CancellationToken cancellationToken)
    {
        var materials = await unitOfWork.Materials.GetAllAsync(cancellationToken);
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            materials = materials.Where(m => m.Name.ToLowerInvariant().Contains(search)).ToList();
        }

        return Result.Success(mapper.Map<IReadOnlyList<MaterialDto>>(materials));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetLowStockMaterialsQuery : IQuery<IReadOnlyList<MaterialDto>>;

public sealed class GetLowStockMaterialsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetLowStockMaterialsQuery, Result<IReadOnlyList<MaterialDto>>>
{
    public async Task<Result<IReadOnlyList<MaterialDto>>> Handle(
        GetLowStockMaterialsQuery request, CancellationToken cancellationToken)
    {
        var materials = await unitOfWork.Materials.GetLowStockAsync(cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<MaterialDto>>(materials));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetMaterialByIdQuery(Guid Id) : IQuery<MaterialDto>;

public sealed class GetMaterialByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetMaterialByIdQuery, Result<MaterialDto>>
{
    public async Task<Result<MaterialDto>> Handle(GetMaterialByIdQuery request, CancellationToken cancellationToken)
    {
        var material = await unitOfWork.Materials.GetByIdAsync(request.Id, cancellationToken);
        if (material is null)
            return Result.Failure<MaterialDto>(Error.NotFound("Material.NotFound", "Материал не найден."));

        return Result.Success(mapper.Map<MaterialDto>(material));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetVisitMaterialUsagesQuery(Guid VisitId) : IQuery<IReadOnlyList<MaterialUsageDto>>;

public sealed class GetVisitMaterialUsagesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetVisitMaterialUsagesQuery, Result<IReadOnlyList<MaterialUsageDto>>>
{
    public async Task<Result<IReadOnlyList<MaterialUsageDto>>> Handle(
        GetVisitMaterialUsagesQuery request, CancellationToken cancellationToken)
    {
        var usages = await unitOfWork.MaterialUsages.GetByVisitIdAsync(request.VisitId, cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<MaterialUsageDto>>(usages));
    }
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreateMaterialCommand(
    string Name,
    string Unit,
    decimal QuantityInStock,
    decimal MinStockThreshold,
    decimal PricePerUnit) : ICommand<MaterialDto>;

public sealed class CreateMaterialCommandValidator : AbstractValidator<CreateMaterialCommand>
{
    public CreateMaterialCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).WithMessage("Укажите название материала.");
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50).WithMessage("Укажите единицу измерения.");
        RuleFor(x => x.QuantityInStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStockThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PricePerUnit).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateMaterialCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateMaterialCommand, Result<MaterialDto>>
{
    public async Task<Result<MaterialDto>> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
    {
        Material material;
        try
        {
            material = Material.Create(
                request.Name,
                request.Unit,
                request.QuantityInStock,
                request.MinStockThreshold,
                request.PricePerUnit);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<MaterialDto>(Error.Validation("Material.InvalidData", ex.Message));
        }

        await unitOfWork.Materials.AddAsync(material, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("MATERIAL_CREATED", "Material", material.Id, null), cancellationToken);

        return Result.Success(mapper.Map<MaterialDto>(material));
    }
}

// ---------------------------------------------------------------------------

public sealed record UpdateMaterialCommand(
    Guid Id,
    string Name,
    string Unit,
    decimal MinStockThreshold,
    decimal PricePerUnit) : ICommand<MaterialDto>;

public sealed class UpdateMaterialCommandValidator : AbstractValidator<UpdateMaterialCommand>
{
    public UpdateMaterialCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MinStockThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PricePerUnit).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateMaterialCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateMaterialCommand, Result<MaterialDto>>
{
    public async Task<Result<MaterialDto>> Handle(UpdateMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await unitOfWork.Materials.GetByIdAsync(request.Id, cancellationToken);
        if (material is null)
            return Result.Failure<MaterialDto>(Error.NotFound("Material.NotFound", "Материал не найден."));

        try
        {
            material.Update(request.Name, request.Unit, request.MinStockThreshold, request.PricePerUnit);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<MaterialDto>(Error.Validation("Material.InvalidData", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("MATERIAL_UPDATED", "Material", material.Id, null), cancellationToken);

        return Result.Success(mapper.Map<MaterialDto>(material));
    }
}

// ---------------------------------------------------------------------------

public sealed record AdjustMaterialStockCommand(
    Guid Id,
    decimal Amount) : ICommand<MaterialDto>;

public sealed class AdjustMaterialStockCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<AdjustMaterialStockCommand, Result<MaterialDto>>
{
    public async Task<Result<MaterialDto>> Handle(AdjustMaterialStockCommand request, CancellationToken cancellationToken)
    {
        var material = await unitOfWork.Materials.GetByIdAsync(request.Id, cancellationToken);
        if (material is null)
            return Result.Failure<MaterialDto>(Error.NotFound("Material.NotFound", "Материал не найден."));

        try
        {
            material.AdjustStock(request.Amount);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<MaterialDto>(Error.Validation("Material.InsufficientStock", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("MATERIAL_STOCK_ADJUSTED", "Material", material.Id, $"Amount: {request.Amount}"), cancellationToken);

        return Result.Success(mapper.Map<MaterialDto>(material));
    }
}

// ---------------------------------------------------------------------------

public sealed record RecordMaterialUsageCommand(
    Guid VisitId,
    Guid MaterialId,
    decimal QuantityUsed) : ICommand<MaterialUsageDto>;

public sealed class RecordMaterialUsageCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<RecordMaterialUsageCommand, Result<MaterialUsageDto>>
{
    public async Task<Result<MaterialUsageDto>> Handle(RecordMaterialUsageCommand request, CancellationToken cancellationToken)
    {
        var visit = await unitOfWork.Visits.GetByIdAsync(request.VisitId, cancellationToken);
        if (visit is null)
            return Result.Failure<MaterialUsageDto>(Error.NotFound("Visit.NotFound", "Визит не найден."));

        var material = await unitOfWork.Materials.GetByIdAsync(request.MaterialId, cancellationToken);
        if (material is null)
            return Result.Failure<MaterialUsageDto>(Error.NotFound("Material.NotFound", "Материал не найден."));

        MaterialUsage usage;
        try
        {
            // First adjust stock — deduct quantity used
            material.AdjustStock(-request.QuantityUsed);

            usage = MaterialUsage.Create(request.VisitId, request.MaterialId, request.QuantityUsed);
            await unitOfWork.MaterialUsages.AddAsync(usage, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<MaterialUsageDto>(Error.Validation("MaterialUsage.InvalidAction", ex.Message));
        }

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("MATERIAL_USAGE_RECORDED", "MaterialUsage", usage.Id, $"VisitId: {request.VisitId}, MaterialId: {request.MaterialId}, Qty: {request.QuantityUsed}"),
            cancellationToken);

        var dbUsage = await unitOfWork.MaterialUsages.GetByIdAsync(usage.Id, cancellationToken);
        return Result.Success(mapper.Map<MaterialUsageDto>(dbUsage));
    }
}
