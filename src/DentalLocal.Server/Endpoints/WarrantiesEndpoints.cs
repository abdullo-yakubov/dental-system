using DentalLocal.Application.Features.Warranties;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class WarrantiesEndpoints
{
    public static void MapWarrantiesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Warranties.Base).WithTags("Warranties");

        // GET /api/v1/warranties/patients/{patientId}
        group.MapGet("/patients/{patientId:guid}", async (Guid patientId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientWarrantiesQuery(patientId), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/warranties/patients/{patientId}/teeth/{toothNumber}
        group.MapGet("/patients/{patientId:guid}/teeth/{toothNumber:int}", async (Guid patientId, short toothNumber, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetToothWarrantiesQuery(patientId, toothNumber), ct);
            return result.ToApiResult();
        });
    }
}
