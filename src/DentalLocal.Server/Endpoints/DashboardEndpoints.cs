using DentalLocal.Application.Features.Analytics;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Dashboard.Summary.Replace("/summary", "")).WithTags("Dashboard");

        // GET /api/v1/dashboard/summary
        group.MapGet("/summary", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDashboardSummaryQuery(), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/dashboard/income-chart
        group.MapGet("/income-chart", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetIncomeChartQuery(), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/dashboard/procedures-top
        group.MapGet("/procedures-top", async (int? count, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTopProceduresQuery(count ?? 5), ct);
            return result.ToApiResult();
        });
    }
}
