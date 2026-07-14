using DentalLocal.Application.Common;
using DentalLocal.Application.Interfaces;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
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

// ---------------------------------------------------------------------------

/// <summary>Статистика загрузки врача за период: heatmap по часам/дням, средняя длительность визита
/// по типам процедур, % отменённых визитов, % простоя относительно рабочего графика (раздел 17.8 ТЗ).</summary>
public sealed record GetDoctorWorkloadStatsQuery(DateOnly? From, DateOnly? To) : IQuery<DoctorWorkloadStatsDto>;

public sealed class GetDoctorWorkloadStatsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetDoctorWorkloadStatsQuery, Result<DoctorWorkloadStatsDto>>
{
    public async Task<Result<DoctorWorkloadStatsDto>> Handle(GetDoctorWorkloadStatsQuery request, CancellationToken cancellationToken)
    {
        var to = request.To ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = request.From ?? to.AddMonths(-1);

        var visits = await unitOfWork.Visits.GetByDateRangeAsync(from, to, cancellationToken);
        var schedules = await unitOfWork.DoctorSchedules.GetAllAsync(cancellationToken);

        var heatmap = visits
            .GroupBy(v => (v.VisitDate.DayOfWeek, v.VisitTime.Hour))
            .Select(g => new WorkloadHeatmapCellDto(g.Key.DayOfWeek, g.Key.Hour, g.Count()))
            .OrderBy(c => c.DayOfWeek).ThenBy(c => c.Hour)
            .ToList();

        var averageDuration = visits
            .SelectMany(v => v.Items, (v, i) => i.Procedure)
            .Where(p => p is not null)
            .GroupBy(p => p!.Name)
            .Select(g => new ProcedureAverageDurationDto(g.Key, g.Average(p => p!.DurationMinutes)))
            .OrderByDescending(d => d.AverageDurationMinutes)
            .ToList();

        var totalVisits = visits.Count;
        var cancelledOrNoShow = visits.Count(v => v.Status == VisitStatus.Cancelled);
        var cancelledPercent = totalVisits == 0 ? 0 : 100.0 * cancelledOrNoShow / totalVisits;

        var scheduledMinutes = schedules
            .Where(s => s.IsActive)
            .Sum(s => (s.EndTime - s.StartTime).TotalMinutes) * WeeksBetween(from, to);

        var busyMinutes = visits
            .SelectMany(v => v.Items, (v, i) => i.Procedure)
            .Where(p => p is not null)
            .Sum(p => p!.DurationMinutes);

        var idlePercent = scheduledMinutes <= 0 ? 0 : Math.Clamp(100.0 * (1 - busyMinutes / scheduledMinutes), 0, 100);

        var dto = new DoctorWorkloadStatsDto(heatmap, averageDuration, cancelledPercent, idlePercent, totalVisits);
        return Result.Success(dto);
    }

    private static double WeeksBetween(DateOnly from, DateOnly to) =>
        Math.Max(1, (to.DayNumber - from.DayNumber) / 7.0);
}
