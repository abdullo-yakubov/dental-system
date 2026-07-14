using DentalLocal.Application.Interfaces;
using DentalLocal.Shared.Contracts.Analytics;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DentalLocal.Persistence.Services;

public class DashboardService(DentalDbContext context) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        var firstDayOfMonth = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var totalPatients = await context.Patients.CountAsync(cancellationToken);
        var activePatients = await context.Patients.CountAsync(p => !p.IsArchived, cancellationToken);
        var visitsToday = await context.Visits.CountAsync(v => v.VisitDate == today, cancellationToken);

        // Net revenue calculations
        var paymentsToday = await context.Payments
            .Where(p => p.PaidAt >= todayStart && p.PaidAt < todayEnd)
            .ToListAsync(cancellationToken);

        var revenueToday = paymentsToday.Sum(p => p.ReversalOfPaymentId == null ? p.Amount : -p.Amount);

        var paymentsMonth = await context.Payments
            .Where(p => p.PaidAt >= firstDayOfMonth && p.PaidAt < todayEnd)
            .ToListAsync(cancellationToken);

        var revenueThisMonth = paymentsMonth.Sum(p => p.ReversalOfPaymentId == null ? p.Amount : -p.Amount);

        var totalDebt = await context.Debts
            .Where(d => d.Status == Domain.Enums.DebtStatus.Open)
            .SumAsync(d => (decimal?)d.RemainingAmount, cancellationToken) ?? 0m;

        var debtorsCount = await context.Debts
            .Where(d => d.Status == Domain.Enums.DebtStatus.Open)
            .Select(d => d.PatientId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new DashboardSummaryDto(
            totalPatients,
            activePatients,
            visitsToday,
            revenueToday,
            revenueThisMonth,
            totalDebt,
            debtorsCount);
    }

    public async Task<IReadOnlyList<IncomeChartPointDto>> GetIncomeChartAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limitDate = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-11);

        var payments = await context.Payments
            .AsNoTracking()
            .Where(p => p.PaidAt >= limitDate)
            .ToListAsync(cancellationToken);

        var points = new List<IncomeChartPointDto>();
        var culture = new CultureInfo("ru-RU");

        for (int i = 11; i >= 0; i--)
        {
            var targetMonth = DateTime.UtcNow.AddMonths(-i);
            var year = targetMonth.Year;
            var month = targetMonth.Month;

            var monthStart = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
            var monthEnd = monthStart.AddMonths(1);

            var monthPayments = payments.Where(p => p.PaidAt >= monthStart && p.PaidAt < monthEnd);
            var amount = monthPayments.Sum(p => p.ReversalOfPaymentId == null ? p.Amount : -p.Amount);

            var label = targetMonth.ToString("MMM yyyy", culture);
            points.Add(new IncomeChartPointDto(label, amount));
        }

        return points;
    }

    public async Task<IReadOnlyList<ProcedureTopDto>> GetTopProceduresAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        var items = await context.TreatmentItems
            .AsNoTracking()
            .Include(i => i.Procedure)
            .GroupBy(i => i.ProcedureId)
            .Select(g => new
            {
                ProcedureId = g.Key,
                Name = g.Max(i => i.Procedure.Name) ?? "Неизвестно",
                Count = g.Sum(i => i.Quantity),
                TotalAmount = g.Sum(i => i.Price * i.Quantity)
            })
            .OrderByDescending(x => x.Count)
            .Take(count)
            .ToListAsync(cancellationToken);

        return items.Select(x => new ProcedureTopDto(x.Name, x.Count, x.TotalAmount)).ToList();
    }
}
