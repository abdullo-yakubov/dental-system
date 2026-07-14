using DentalLocal.Application.Features.Materials;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Materials;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DentalLocal.Server.Endpoints;

public static class MaterialsEndpoints
{
    public static void MapMaterialsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Materials.Base).WithTags("Materials");

        // GET /api/v1/materials?search=...
        group.MapGet("", async (string? search, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMaterialsQuery(search), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/materials/low-stock
        group.MapGet("/low-stock", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLowStockMaterialsQuery(), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/materials/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMaterialByIdQuery(id), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/materials
        group.MapPost("", async (CreateMaterialRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateMaterialCommand(
                request.Name,
                request.Unit,
                request.QuantityInStock,
                request.MinStockThreshold,
                request.PricePerUnit);

            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.Materials.Base}/{dto.Id}");
        });

        // PUT /api/v1/materials/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpdateMaterialRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateMaterialCommand(
                id,
                request.Name,
                request.Unit,
                request.MinStockThreshold,
                request.PricePerUnit);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        // POST /api/v1/materials/{id}/adjust
        group.MapPost("/{id:guid}/adjust", async (Guid id, AdjustStockRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AdjustMaterialStockCommand(id, request.Amount);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        // GET /api/v1/materials/usages?visitId=...
        group.MapGet("/usages", async (Guid visitId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetVisitMaterialUsagesQuery(visitId), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/materials/usages
        group.MapPost("/usages", async (Guid visitId, RecordMaterialUsageRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new RecordMaterialUsageCommand(visitId, request.MaterialId, request.QuantityUsed);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });
    }
}
