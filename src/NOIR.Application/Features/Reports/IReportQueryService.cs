namespace NOIR.Application.Features.Reports;

/// <summary>
/// Service for aggregating report data from the database.
/// Implemented in Infrastructure for direct DbContext access for efficient aggregation.
/// </summary>
public interface IReportQueryService
{
    /// <summary>
    /// Generates a revenue report for the specified period.
    /// </summary>
    Task<DTOs.RevenueReportDto> GetRevenueReportAsync(
        string period,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a best-sellers report for the specified period.
    /// </summary>
    Task<DTOs.BestSellersReportDto> GetBestSellersAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int topN,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an inventory health report.
    /// </summary>
    Task<DTOs.InventoryReportDto> GetInventoryReportAsync(
        int lowStockThreshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a customer acquisition and retention report.
    /// </summary>
    Task<DTOs.CustomerReportDto> GetCustomerReportAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a report as CSV bytes with metadata.
    /// </summary>
    Task<DTOs.ExportResultDto> ExportReportAsync(
        DTOs.ReportType reportType,
        DTOs.ExportFormat format,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        CancellationToken cancellationToken = default);
}
