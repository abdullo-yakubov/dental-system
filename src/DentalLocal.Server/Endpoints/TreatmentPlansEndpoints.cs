using DentalLocal.Application.Features.TreatmentPlans;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.TreatmentPlans;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class TreatmentPlansEndpoints
{
    public static void MapTreatmentPlansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.TreatmentPlans.Base).WithTags("TreatmentPlans");

        // GET /api/v1/treatment-plans?patientId=...
        group.MapGet("", async (Guid patientId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientTreatmentPlansQuery(patientId), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/treatment-plans/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTreatmentPlanByIdQuery(id), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/treatment-plans
        group.MapPost("", async (CreateTreatmentPlanRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateTreatmentPlanCommand(request.PatientId, request.Title, request.Notes);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.TreatmentPlans.Base}/{dto.Id}");
        });

        // PUT /api/v1/treatment-plans/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpdateTreatmentPlanRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateTreatmentPlanCommand(id, request.Title, request.Notes);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        // POST /api/v1/treatment-plans/{id}/items
        group.MapPost("/{id:guid}/items", async (Guid id, AddTreatmentPlanItemRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AddTreatmentPlanItemCommand(id, request.ProcedureId, request.ToothNumber, request.EstimatedPrice, request.OrderIndex);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        // POST /api/v1/treatment-plans/{id}/activate
        group.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ActivateTreatmentPlanCommand(id), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/treatment-plans/{id}/cancel
        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CancelTreatmentPlanCommand(id), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/treatment-plans/{id}/items/{itemId}/complete
        group.MapPost("/{id:guid}/items/{itemId:guid}/complete", async (Guid id, Guid itemId, CompleteTreatmentPlanItemRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CompleteTreatmentPlanItemCommand(id, itemId, request.VisitId);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });
    }
}
