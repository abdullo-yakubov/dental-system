using DentalLocal.Application.Features.Procedures;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Procedures;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class ProceduresEndpoints
{
    public static void MapProceduresEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Procedures.Base).WithTags("Procedures");
        var categoryGroup = app.MapGroup(ApiRoutes.Procedures.Categories).WithTags("Procedures");

        // GET /api/v1/procedures
        group.MapGet("", async (bool? includeInactive, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProceduresQuery(includeInactive ?? false), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/procedures/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProcedureByIdQuery(id), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/procedures
        group.MapPost("", async (CreateProcedureRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateProcedureCommand(
                request.Name,
                request.Description,
                request.Price,
                request.DurationMinutes ?? 30,
                request.CategoryId ?? Guid.Empty,
                request.RequiresTooth,
                request.IsActive);

            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.Procedures.Base}/{dto.Id}");
        });

        // PUT /api/v1/procedures/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpdateProcedureRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateProcedureCommand(
                id,
                request.Name,
                request.Description,
                request.Price,
                request.DurationMinutes ?? 30,
                request.CategoryId ?? Guid.Empty,
                request.RequiresTooth,
                request.IsActive);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        // GET /api/v1/procedure-categories
        categoryGroup.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProcedureCategoriesQuery(), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/procedure-categories
        categoryGroup.MapPost("", async (CreateProcedureCategoryRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateProcedureCategoryCommand(request.Name, request.Color ?? "#3B82F6", 0);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.Procedures.Categories}/{dto.Id}");
        });
    }
}
