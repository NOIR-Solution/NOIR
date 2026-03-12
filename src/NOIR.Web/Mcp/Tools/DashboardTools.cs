using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for dashboard metrics.
/// Provides situational awareness for AI agents about the platform state.
/// </summary>
[McpServerToolType]
public sealed class DashboardTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_dashboard_core", ReadOnly = true, Idempotent = true)]
    [Description("Get core dashboard data including user count, recent activity, and system metrics. Use this to understand the current state of the platform.")]
    public async Task<CoreDashboardDto> GetCoreDashboard(
        [Description("Number of recent activities to return (default: 10)")] int activityCount = 10,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<CoreDashboardDto>>(
            new GetCoreDashboardQuery(activityCount), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_dashboard_ecommerce", ReadOnly = true, Idempotent = true)]
    [Description("Get ecommerce dashboard metrics including revenue, order counts, top-selling products, and low-stock alerts. Requires Ecommerce.Orders module.")]
    public async Task<DashboardMetricsDto> GetEcommerceDashboard(
        [Description("Number of top products to include (default: 5)")] int topProducts = 5,
        [Description("Inventory threshold for low-stock alerts (default: 10)")] int lowStockThreshold = 10,
        [Description("Number of recent orders to include (default: 10)")] int recentOrders = 10,
        [Description("Number of days for sales trend data (default: 30)")] int salesDays = 30,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<DashboardMetricsDto>>(
            new GetEcommerceDashboardQuery(topProducts, lowStockThreshold, recentOrders, salesDays), ct);
        return result.Unwrap();
    }
}
