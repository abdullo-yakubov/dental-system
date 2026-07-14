using DentalLocal.Application.Features.Teeth;
using DentalLocal.Domain.Enums;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Teeth;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class TeethEndpoints
{
    public static void MapTeethEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/v1/patients/{id}/odontogram
        app.MapGet($"{ApiRoutes.Patients.Base}/{{patientId:guid}}/odontogram",
            async (Guid patientId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPatientOdontogramQuery(patientId), ct);
                return result.ToApiResult();
            }).WithTags("Teeth");

        // GET /api/v1/patients/{id}/teeth/{number}/history
        app.MapGet($"{ApiRoutes.Patients.Base}/{{patientId:guid}}/teeth/{{toothNumber:int}}/history",
            async (Guid patientId, short toothNumber, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetToothHistoryQuery(patientId, toothNumber), ct);
                return result.ToApiResult();
            }).WithTags("Teeth");

        // PUT /api/v1/patients/{id}/teeth/{number}
        app.MapPut($"{ApiRoutes.Patients.Base}/{{patientId:guid}}/teeth/{{toothNumber:int}}",
            async (Guid patientId, short toothNumber, UpdateToothRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateToothStatusCommand(
                    patientId,
                    request.ToothNumber != 0 ? request.ToothNumber : toothNumber,
                    (DentalLocal.Shared.Enums.ToothStateType)(short)request.NewState,
                    request.VisitId,
                    request.Comment);

                var result = await sender.Send(command, ct);
                return result.ToApiResult();
            }).WithTags("Teeth");
    }
}
