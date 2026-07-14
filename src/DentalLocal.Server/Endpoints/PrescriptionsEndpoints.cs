using DentalLocal.Application.Features.Prescriptions;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Prescriptions;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class PrescriptionsEndpoints
{
    public static void MapPrescriptionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Prescriptions.Base).WithTags("Prescriptions");

        // GET /api/v1/prescriptions/visits/{visitId}
        group.MapGet("/visits/{visitId:guid}", async (Guid visitId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetVisitPrescriptionsQuery(visitId), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/prescriptions/visits/{visitId}
        group.MapPost("/visits/{visitId:guid}", async (Guid visitId, CreatePrescriptionRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreatePrescriptionCommand(visitId, request.MedicationName, request.Dosage, request.Instructions);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.Prescriptions.Base}/{dto.Id}");
        });

        // DELETE /api/v1/prescriptions/{id}
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeletePrescriptionCommand(id), ct);
            return result.ToApiResult();
        });
    }
}
