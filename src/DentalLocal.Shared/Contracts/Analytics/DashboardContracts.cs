namespace DentalLocal.Shared.Contracts.Analytics;

public sealed record DashboardSummaryDto(
    int TotalPatients,
    int ActivePatients,
    int VisitsToday,
    decimal RevenueToday,
    decimal RevenueThisMonth,
    decimal TotalDebt,
    int DebtorsCount);

public sealed record IncomeChartPointDto(
    string Label,
    decimal Amount);

public sealed record ProcedureTopDto(
    string Name,
    int Count,
    decimal TotalAmount);

// ── Загрузка врача (раздел 17.8 ТЗ) ─────────────────────────────────────────

public sealed record WorkloadHeatmapCellDto(DayOfWeek DayOfWeek, int Hour, int VisitCount);

public sealed record ProcedureAverageDurationDto(string ProcedureName, double AverageDurationMinutes);

public sealed record DoctorWorkloadStatsDto(
    IReadOnlyList<WorkloadHeatmapCellDto> Heatmap,
    IReadOnlyList<ProcedureAverageDurationDto> AverageDurationByProcedure,
    double CancelledOrNoShowPercent,
    double IdlePercent,
    int TotalVisits);
