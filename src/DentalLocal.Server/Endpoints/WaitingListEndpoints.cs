using DentalLocal.Application.Features.WaitingListing;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.WaitingList;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class WaitingListEndpoints
{
    public static void MapWaitingListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.WaitingList.Base).WithTags("WaitingList");

        // GET /api/v1/waiting-list?status=...
        group.MapGet("", async (string? status, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWaitingListQuery(status), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/waiting-list/patients/{patientId}
        group.MapGet("/patients/{patientId:guid}", async (Guid patientId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientWaitingListQuery(patientId), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/waiting-list
        group.MapPost("", async (CreateWaitingListEntryRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateWaitingListEntryCommand(
                request.PatientId, request.DesiredDateFrom, request.DesiredDateTo, request.ProcedureId, request.Notes);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.WaitingList.Base}/{dto.Id}");
        });

        // PUT /api/v1/waiting-list/{id}/status
        group.MapPut("/{id:guid}/status", async (Guid id, UpdateWaitingListStatusRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateWaitingListStatusCommand(id, request.Status);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });
    }
}
