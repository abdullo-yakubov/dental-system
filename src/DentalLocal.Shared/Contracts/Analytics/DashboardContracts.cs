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
