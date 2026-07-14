using DentalLocal.Application.Features.Icd;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class IcdEndpoints
{
    public static void MapIcdEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/v1/icd10?search=...
        app.MapGet(ApiRoutes.Icd.Icd10,
            async (string? search, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SearchIcd10CodesQuery(search), ct);
                return result.ToApiResult();
            }).WithTags("Icd");

        // GET /api/v1/icd11?search=...
        app.MapGet(ApiRoutes.Icd.Icd11,
            async (string? search, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SearchIcd11CodesQuery(search), ct);
                return result.ToApiResult();
            }).WithTags("Icd");
    }
}
