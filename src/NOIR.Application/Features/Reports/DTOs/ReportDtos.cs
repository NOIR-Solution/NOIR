namespace NOIR.Application.Features.Reports.DTOs;

// ─── Revenue Report ───────────────────────────────────────────────────────

/// <summary>
/// Complete revenue report with period comparisons and breakdowns.
/// </summary>
public sealed record RevenueReportDto(
    string Period,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    IReadOnlyList<DailyRevenueDto> RevenueByDay,
    IReadOnlyList<CategoryRevenueDto> RevenueByCategory,
    IReadOnlyList<PaymentMethodRevenueDto> RevenueByPaymentMethod,
    RevenueComparisonDto ComparedToPreviousPeriod);

/// <summary>
/// Revenue data for a single day.
/// </summary>
public sealed record DailyRevenueDto(
    DateOnly Date,
    decimal Revenue,
    int OrderCount);

/// <summary>
/// Revenue data grouped by product category.
/// </summary>
public sealed record CategoryRevenueDto(
    Guid? CategoryId,
    string CategoryName,
    decimal Revenue,
    int OrderCount);

/// <summary>
/// Revenue data grouped by payment method/provider.
/// </summary>
public sealed record PaymentMethodRevenueDto(
    string Method,
    decimal Revenue,
    int Count);

/// <summary>
/// Comparison metrics against the previous period.
/// </summary>
public sealed record RevenueComparisonDto(
    decimal RevenueChange,
    decimal OrderCountChange);

// ─── Best Sellers Report ──────────────────────────────────────────────────

/// <summary>
/// Best selling products report for a given period.
/// </summary>
public sealed record BestSellersReportDto(
    IReadOnlyList<BestSellerDto> Products,
    string Period,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate);

/// <summary>
/// A best-selling product with sales metrics.
/// </summary>
public sealed record BestSellerDto(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    int UnitsSold,
    decimal Revenue);

// ─── Inventory Report ─────────────────────────────────────────────────────

/// <summary>
/// Inventory health report with low stock alerts and stock valuation.
/// </summary>
public sealed record InventoryReportDto(
    IReadOnlyList<LowStockDto> LowStockProducts,
    int TotalProducts,
    int TotalVariants,
    decimal TotalStockValue,
    decimal TurnoverRate);

/// <summary>
/// A product variant with low stock level.
/// </summary>
public sealed record LowStockDto(
    Guid ProductId,
    string Name,
    string? VariantSku,
    int CurrentStock,
    int ReorderLevel);

// ─── Customer Report ──────────────────────────────────────────────────────

/// <summary>
/// Customer acquisition and retention report.
/// </summary>
public sealed record CustomerReportDto(
    int NewCustomers,
    int ReturningCustomers,
    decimal ChurnRate,
    IReadOnlyList<MonthlyAcquisitionDto> AcquisitionByMonth,
    IReadOnlyList<TopCustomerDto> TopCustomers);

/// <summary>
/// Monthly customer acquisition metrics.
/// </summary>
public sealed record MonthlyAcquisitionDto(
    string Month,
    int NewCustomers,
    decimal Revenue);

/// <summary>
/// A top customer with spending metrics.
/// </summary>
public sealed record TopCustomerDto(
    Guid? CustomerId,
    string Name,
    decimal TotalSpent,
    int OrderCount);

// ─── Export ───────────────────────────────────────────────────────────────

/// <summary>
/// Supported report types for export.
/// </summary>
public enum ReportType
{
    Revenue,
    BestSellers,
    Inventory,
    CustomerAcquisition
}

/// <summary>
/// Supported export file formats.
/// </summary>
public enum ExportFormat
{
    CSV,
    Excel
}

/// <summary>
/// Export request parameters.
/// </summary>
public sealed record ExportRequestDto(
    ReportType ReportType,
    ExportFormat Format,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate);

/// <summary>
/// Export result containing file bytes and metadata.
/// </summary>
public sealed record ExportResultDto(
    byte[] FileBytes,
    string ContentType,
    string FileName);
