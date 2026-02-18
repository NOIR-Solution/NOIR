using NOIR.Application.Features.Dashboard.DTOs;

namespace NOIR.Application.Features.Dashboard;

/// <summary>
/// Service for aggregating dashboard metrics from the database.
/// Implemented in Infrastructure for direct DbContext access for efficient aggregation.
/// </summary>
public interface IDashboardQueryService
{
    Task<DashboardMetricsDto> GetMetricsAsync(
        int topProductsCount,
        int lowStockThreshold,
        int recentOrdersCount,
        int salesOverTimeDays,
        CancellationToken cancellationToken = default);
}
