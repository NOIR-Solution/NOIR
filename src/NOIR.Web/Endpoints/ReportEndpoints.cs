namespace NOIR.Web.Endpoints;

/// <summary>
/// Reports API endpoints.
/// Provides analytics reports for revenue, best-sellers, inventory, and customers.
/// </summary>
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        // Get revenue report
        group.MapGet("/revenue", async (
            [FromQuery] string? period,
            [FromQuery] DateTimeOffset? startDate,
            [FromQuery] DateTimeOffset? endDate,
            IMessageBus bus) =>
        {
            var query = new GetRevenueReportQuery(
                period ?? "monthly",
                startDate,
                endDate);
            var result = await bus.InvokeAsync<Result<RevenueReportDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReportsRead)
        .WithName("GetRevenueReport")
        .WithSummary("Get revenue report")
        .WithDescription("Returns a revenue report with daily breakdown, category breakdown, payment method breakdown, and period comparison.")
        .Produces<RevenueReportDto>(StatusCodes.Status200OK);

        // Get best-sellers report
        group.MapGet("/best-sellers", async (
            [FromQuery] DateTimeOffset? startDate,
            [FromQuery] DateTimeOffset? endDate,
            [FromQuery] int? topN,
            IMessageBus bus) =>
        {
            var query = new GetBestSellersReportQuery(
                startDate,
                endDate,
                topN ?? 10);
            var result = await bus.InvokeAsync<Result<BestSellersReportDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReportsRead)
        .WithName("GetBestSellersReport")
        .WithSummary("Get best-sellers report")
        .WithDescription("Returns a list of top selling products for the specified period sorted by units sold.")
        .Produces<BestSellersReportDto>(StatusCodes.Status200OK);

        // Get inventory report
        group.MapGet("/inventory", async (
            [FromQuery] int? lowStockThreshold,
            IMessageBus bus) =>
        {
            var query = new GetInventoryReportQuery(
                lowStockThreshold ?? 10);
            var result = await bus.InvokeAsync<Result<InventoryReportDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReportsRead)
        .WithName("GetInventoryReport")
        .WithSummary("Get inventory report")
        .WithDescription("Returns an inventory health report including low stock alerts, stock valuation, and turnover rate.")
        .Produces<InventoryReportDto>(StatusCodes.Status200OK);

        // Get customer report
        group.MapGet("/customers", async (
            [FromQuery] DateTimeOffset? startDate,
            [FromQuery] DateTimeOffset? endDate,
            IMessageBus bus) =>
        {
            var query = new GetCustomerReportQuery(
                startDate,
                endDate);
            var result = await bus.InvokeAsync<Result<CustomerReportDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.ReportsRead)
        .WithName("GetCustomerReport")
        .WithSummary("Get customer report")
        .WithDescription("Returns customer acquisition and retention metrics including new customers, returning customers, churn rate, and top spenders.")
        .Produces<CustomerReportDto>(StatusCodes.Status200OK);

        // Export report as file
        group.MapGet("/export", async (
            [FromQuery] ReportType reportType,
            [FromQuery] ExportFormat? format,
            [FromQuery] DateTimeOffset? startDate,
            [FromQuery] DateTimeOffset? endDate,
            IMessageBus bus) =>
        {
            var query = new ExportReportQuery(
                reportType,
                format ?? ExportFormat.CSV,
                startDate,
                endDate);
            var result = await bus.InvokeAsync<Result<ExportResultDto>>(query);

            if (result.IsFailure)
                return result.ToHttpResult();

            var export = result.Value;
            return Results.File(export.FileBytes, export.ContentType, export.FileName);
        })
        .RequireAuthorization(Permissions.ReportsRead)
        .WithName("ExportReport")
        .WithSummary("Export report as file")
        .WithDescription("Exports the specified report type as a CSV file download.")
        .Produces<byte[]>(StatusCodes.Status200OK, "text/csv");
    }
}
