using DentalLocal.Application.Common;
using DentalLocal.Application.Interfaces;
using DentalLocal.Shared.Contracts.Analytics;
using MediatR;

namespace DentalLocal.Application.Features.Analytics;

public sealed record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;

public sealed class GetDashboardSummaryQueryHandler(IDashboardService dashboardService)
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async Task<Result<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await dashboardService.GetSummaryAsync(cancellationToken);
        return Result.Success(summary);
    }
}

// ---------------------------------------------------------------------------

public sealed record GetIncomeChartQuery : IQuery<IReadOnlyList<IncomeChartPointDto>>;

public sealed class GetIncomeChartQueryHandler(IDashboardService dashboardService)
    : IRequestHandler<GetIncomeChartQuery, Result<IReadOnlyList<IncomeChartPointDto>>>
{
    public async Task<Result<IReadOnlyList<IncomeChartPointDto>>> Handle(
        GetIncomeChartQuery request, CancellationToken cancellationToken)
    {
        var chart = await dashboardService.GetIncomeChartAsync(cancellationToken);
        return Result.Success(chart);
    }
}

// ---------------------------------------------------------------------------

public sealed record GetTopProceduresQuery(int Count = 5) : IQuery<IReadOnlyList<ProcedureTopDto>>;

public sealed class GetTopProceduresQueryHandler(IDashboardService dashboardService)
    : IRequestHandler<GetTopProceduresQuery, Result<IReadOnlyList<ProcedureTopDto>>>
{
    public async Task<Result<IReadOnlyList<ProcedureTopDto>>> Handle(
        GetTopProceduresQuery request, CancellationToken cancellationToken)
    {
        var procedures = await dashboardService.GetTopProceduresAsync(request.Count, cancellationToken);
        return Result.Success(procedures);
    }
}
