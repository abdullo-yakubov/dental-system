using DentalLocal.Shared.Contracts.Analytics;

namespace DentalLocal.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<IncomeChartPointDto>> GetIncomeChartAsync(CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<ProcedureTopDto>> GetTopProceduresAsync(int count = 5, CancellationToken cancellationToken = default);
}
