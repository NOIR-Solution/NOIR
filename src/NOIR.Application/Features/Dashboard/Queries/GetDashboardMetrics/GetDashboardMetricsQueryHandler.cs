using NOIR.Application.Features.Dashboard.DTOs;

namespace NOIR.Application.Features.Dashboard.Queries.GetDashboardMetrics;

/// <summary>
/// Wolverine handler for getting dashboard metrics.
/// </summary>
public class GetDashboardMetricsQueryHandler
{
    private readonly IDashboardQueryService _dashboardService;

    public GetDashboardMetricsQueryHandler(IDashboardQueryService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<Result<DashboardMetricsDto>> Handle(
        GetDashboardMetricsQuery query,
        CancellationToken cancellationToken)
    {
        var metrics = await _dashboardService.GetMetricsAsync(
            query.TopProductsCount,
            query.LowStockThreshold,
            query.RecentOrdersCount,
            query.SalesOverTimeDays,
            cancellationToken);

        return Result.Success(metrics);
    }
}
