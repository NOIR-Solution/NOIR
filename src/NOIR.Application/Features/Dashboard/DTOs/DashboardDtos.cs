namespace NOIR.Application.Features.Dashboard.DTOs;

/// <summary>
/// Complete dashboard metrics response.
/// </summary>
public sealed record DashboardMetricsDto(
    RevenueMetricsDto Revenue,
    OrderStatusCountsDto OrderCounts,
    IReadOnlyList<TopSellingProductDto> TopSellingProducts,
    IReadOnlyList<LowStockProductDto> LowStockProducts,
    IReadOnlyList<RecentOrderDto> RecentOrders,
    IReadOnlyList<SalesOverTimeDto> SalesOverTime,
    ProductStatusDistributionDto ProductDistribution);

/// <summary>
/// Revenue metrics with period comparisons.
/// </summary>
public sealed record RevenueMetricsDto(
    decimal TotalRevenue,
    decimal RevenueThisMonth,
    decimal RevenueLastMonth,
    decimal RevenueToday,
    int TotalOrders,
    int OrdersThisMonth,
    int OrdersToday,
    decimal AverageOrderValue);

/// <summary>
/// Order counts grouped by status.
/// </summary>
public sealed record OrderStatusCountsDto(
    int Pending,
    int Confirmed,
    int Processing,
    int Shipped,
    int Delivered,
    int Completed,
    int Cancelled,
    int Refunded,
    int Returned);

/// <summary>
/// Top selling product with sales count and revenue.
/// </summary>
public sealed record TopSellingProductDto(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    int TotalQuantitySold,
    decimal TotalRevenue);

/// <summary>
/// Product variant with low stock level.
/// </summary>
public sealed record LowStockProductDto(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string? Sku,
    int StockQuantity,
    int LowStockThreshold);

/// <summary>
/// Recent order summary for dashboard display.
/// </summary>
public sealed record RecentOrderDto(
    Guid OrderId,
    string OrderNumber,
    string? CustomerEmail,
    decimal GrandTotal,
    OrderStatus Status,
    DateTimeOffset CreatedAt);

/// <summary>
/// Sales data point for chart display.
/// </summary>
public sealed record SalesOverTimeDto(
    DateOnly Date,
    decimal Revenue,
    int OrderCount);

/// <summary>
/// Product status distribution for chart display.
/// </summary>
public sealed record ProductStatusDistributionDto(
    int Draft,
    int Active,
    int Archived);
