using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for business analytics and reports.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Analytics.Reports)]
public sealed class ReportTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_reports_revenue", ReadOnly = true, Idempotent = true)]
    [Description("Get revenue report with trends over time. Supports daily, weekly, or monthly period grouping.")]
    public async Task<RevenueReportDto> GetRevenueReport(
        [Description("Grouping period: daily, weekly, monthly (default: monthly)")] string period = "monthly",
        [Description("Start date for report range (ISO 8601)")] string? startDate = null,
        [Description("End date for report range (ISO 8601)")] string? endDate = null,
        CancellationToken ct = default)
    {
        var start = startDate is not null ? DateTimeOffset.Parse(startDate) : (DateTimeOffset?)null;
        var end = endDate is not null ? DateTimeOffset.Parse(endDate) : (DateTimeOffset?)null;

        var result = await bus.InvokeAsync<Result<RevenueReportDto>>(
            new GetRevenueReportQuery(period, start, end), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_reports_best_sellers", ReadOnly = true, Idempotent = true)]
    [Description("Get best-selling products report ranked by revenue and quantity sold.")]
    public async Task<BestSellersReportDto> GetBestSellersReport(
        [Description("Start date for report range (ISO 8601)")] string? startDate = null,
        [Description("End date for report range (ISO 8601)")] string? endDate = null,
        [Description("Number of top products to include (default: 10)")] int topN = 10,
        CancellationToken ct = default)
    {
        var start = startDate is not null ? DateTimeOffset.Parse(startDate) : (DateTimeOffset?)null;
        var end = endDate is not null ? DateTimeOffset.Parse(endDate) : (DateTimeOffset?)null;

        var result = await bus.InvokeAsync<Result<BestSellersReportDto>>(
            new GetBestSellersReportQuery(start, end, topN), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_reports_inventory", ReadOnly = true, Idempotent = true)]
    [Description("Get inventory report: stock levels, low-stock products, stock value, and category breakdown.")]
    public async Task<InventoryReportDto> GetInventoryReport(
        [Description("Threshold for low-stock warnings (default: 10)")] int lowStockThreshold = 10,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<InventoryReportDto>>(
            new GetInventoryReportQuery(lowStockThreshold), ct);
        return result.Unwrap();
    }
}
