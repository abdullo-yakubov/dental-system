using DentalLocal.Application.Features.Analytics;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Analytics.Base).WithTags("Analytics");

        // GET /api/v1/analytics/workload?from=...&to=...
        group.MapGet("/workload", async (DateOnly? from, DateOnly? to, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDoctorWorkloadStatsQuery(from, to), ct);
            return result.ToApiResult();
        }).WithRequestTimeout(RequestTimeoutPolicies.Long);

        // GET /api/v1/analytics/profit?from=...&to=...
        group.MapGet("/profit", async (DateOnly? from, DateOnly? to, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProfitStatsQuery(from, to), ct);
            return result.ToApiResult();
        }).WithRequestTimeout(RequestTimeoutPolicies.Long);
    }
}
