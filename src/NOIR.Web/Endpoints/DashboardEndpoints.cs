using NOIR.Application.Features.Dashboard.DTOs;
using NOIR.Application.Features.Dashboard.Queries.GetDashboardMetrics;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Dashboard API endpoints.
/// Provides aggregated metrics for the admin dashboard.
/// </summary>
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        // Get dashboard metrics
        group.MapGet("/metrics", async (
            [FromQuery] int? topProducts,
            [FromQuery] int? lowStockThreshold,
            [FromQuery] int? recentOrders,
            [FromQuery] int? salesDays,
            IMessageBus bus) =>
        {
            var query = new GetDashboardMetricsQuery(
                topProducts ?? 5,
                lowStockThreshold ?? 10,
                recentOrders ?? 10,
                salesDays ?? 30);
            var result = await bus.InvokeAsync<Result<DashboardMetricsDto>>(query);
            return result.ToHttpResult();
        })
        // Dashboard aggregates data across orders, products, and inventory.
        // Uses OrdersRead as the minimum required permission since revenue
        // and order metrics are the primary dashboard content.
        .RequireAuthorization(Permissions.OrdersRead)
        .WithName("GetDashboardMetrics")
        .WithSummary("Get dashboard metrics")
        .WithDescription("Returns aggregated dashboard metrics including revenue, order counts, top products, and more.")
        .Produces<DashboardMetricsDto>(StatusCodes.Status200OK);
    }
}
