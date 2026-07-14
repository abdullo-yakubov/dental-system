using DentalLocal.Application.Features.Discounts;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Discounts;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class DiscountsEndpoints
{
    public static void MapDiscountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Discounts.Base).WithTags("Discounts");

        // GET /api/v1/discounts/patients/{patientId}
        group.MapGet("/patients/{patientId:guid}", async (Guid patientId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientDiscountHistoryQuery(patientId), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/discounts/patients/{patientId}
        group.MapPost("/patients/{patientId:guid}", async (Guid patientId, ApplyDiscountRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new ApplyDiscountCommand(patientId, request.VisitId, request.DiscountType, request.Amount, request.Percent, request.Reason);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });
    }
}
