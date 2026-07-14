namespace DentalLocal.Shared.Contracts.Materials;

public sealed record MaterialDto(
    Guid Id,
    string Name,
    string Unit,
    decimal QuantityInStock,
    decimal MinStockThreshold,
    decimal PricePerUnit,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record MaterialUsageDto(
    Guid Id,
    Guid VisitId,
    Guid MaterialId,
    string MaterialName,
    decimal QuantityUsed,
    DateTimeOffset UsedAt);

public sealed record CreateMaterialRequest(
    string Name,
    string Unit,
    decimal QuantityInStock,
    decimal MinStockThreshold,
    decimal PricePerUnit);

public sealed record UpdateMaterialRequest(
    string Name,
    string Unit,
    decimal MinStockThreshold,
    decimal PricePerUnit);

public sealed record AdjustStockRequest(
    decimal Amount);

public sealed record RecordMaterialUsageRequest(
    Guid MaterialId,
    decimal QuantityUsed);
