using NOIR.Application.Features.Dashboard;
using NOIR.Application.Features.Dashboard.DTOs;
using NOIR.Infrastructure.Persistence;

namespace NOIR.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of dashboard query service.
/// Uses direct DbContext access for efficient aggregation queries.
/// </summary>
public class DashboardQueryService : IDashboardQueryService, IScopedService
{
    private static readonly OrderStatus[] ValidRevenueStatuses =
    [
        OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped,
        OrderStatus.Delivered, OrderStatus.Completed
    ];

    private readonly ApplicationDbContext _context;

    public DashboardQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync(
        int topProductsCount,
        int lowStockThreshold,
        int recentOrdersCount,
        int salesOverTimeDays,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var startOfToday = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var salesStartDate = now.AddDays(-salesOverTimeDays);

        // Use Set<T>() to access entities not exposed on IApplicationDbContext
        var orders = _context.Set<Domain.Entities.Order.Order>();
        var products = _context.Products;
        var productVariants = _context.ProductVariants;

        // Run aggregation queries sequentially - DbContext is not thread-safe
        // and does not support multiple concurrent operations on the same instance.
        var revenue = await GetRevenueMetricsAsync(orders, startOfMonth, startOfLastMonth, startOfToday, cancellationToken);
        var orderCounts = await GetOrderStatusCountsAsync(orders, cancellationToken);
        var topSelling = await GetTopSellingProductsAsync(orders, topProductsCount, cancellationToken);
        var lowStock = await GetLowStockProductsAsync(productVariants, lowStockThreshold, cancellationToken);
        var recentOrders = await GetRecentOrdersAsync(orders, recentOrdersCount, cancellationToken);
        var salesOverTime = await GetSalesOverTimeAsync(orders, salesStartDate, cancellationToken);
        var productDistribution = await GetProductStatusDistributionAsync(products, cancellationToken);

        return new DashboardMetricsDto(
            revenue,
            orderCounts,
            topSelling,
            lowStock,
            recentOrders,
            salesOverTime,
            productDistribution);
    }

    private static async Task<RevenueMetricsDto> GetRevenueMetricsAsync(
        DbSet<Domain.Entities.Order.Order> orders,
        DateTimeOffset startOfMonth,
        DateTimeOffset startOfLastMonth,
        DateTimeOffset startOfToday,
        CancellationToken ct)
    {
        var validOrders = orders.Where(o => ValidRevenueStatuses.Contains(o.Status));

        var totalRevenue = await validOrders
            .TagWith("Dashboard_TotalRevenue")
            .SumAsync(o => o.GrandTotal, ct);

        var revenueThisMonth = await validOrders
            .TagWith("Dashboard_RevenueThisMonth")
            .Where(o => o.CreatedAt >= startOfMonth)
            .SumAsync(o => o.GrandTotal, ct);

        var revenueLastMonth = await validOrders
            .TagWith("Dashboard_RevenueLastMonth")
            .Where(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfMonth)
            .SumAsync(o => o.GrandTotal, ct);

        var revenueToday = await validOrders
            .TagWith("Dashboard_RevenueToday")
            .Where(o => o.CreatedAt >= startOfToday)
            .SumAsync(o => o.GrandTotal, ct);

        var totalOrders = await orders
            .TagWith("Dashboard_TotalOrders")
            .CountAsync(ct);

        var ordersThisMonth = await orders
            .TagWith("Dashboard_OrdersThisMonth")
            .Where(o => o.CreatedAt >= startOfMonth)
            .CountAsync(ct);

        var ordersToday = await orders
            .TagWith("Dashboard_OrdersToday")
            .Where(o => o.CreatedAt >= startOfToday)
            .CountAsync(ct);

        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        return new RevenueMetricsDto(
            totalRevenue, revenueThisMonth, revenueLastMonth, revenueToday,
            totalOrders, ordersThisMonth, ordersToday, avgOrderValue);
    }

    private static async Task<OrderStatusCountsDto> GetOrderStatusCountsAsync(
        DbSet<Domain.Entities.Order.Order> orders,
        CancellationToken ct)
    {
        var counts = await orders
            .TagWith("Dashboard_OrderStatusCounts")
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dict = counts.ToDictionary(x => x.Status, x => x.Count);

        return new OrderStatusCountsDto(
            dict.GetValueOrDefault(OrderStatus.Pending),
            dict.GetValueOrDefault(OrderStatus.Confirmed),
            dict.GetValueOrDefault(OrderStatus.Processing),
            dict.GetValueOrDefault(OrderStatus.Shipped),
            dict.GetValueOrDefault(OrderStatus.Delivered),
            dict.GetValueOrDefault(OrderStatus.Completed),
            dict.GetValueOrDefault(OrderStatus.Cancelled),
            dict.GetValueOrDefault(OrderStatus.Refunded),
            dict.GetValueOrDefault(OrderStatus.Returned));
    }

    private static async Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(
        DbSet<Domain.Entities.Order.Order> orders,
        int count,
        CancellationToken ct)
    {
        // Start from Order side to avoid OrderItem → Order navigation + global query filter issues
        var itemData = await orders
            .TagWith("Dashboard_TopSellingProducts")
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .SelectMany(o => o.Items, (o, oi) => new { oi.ProductId, oi.ProductName, oi.ImageUrl, oi.Quantity, oi.UnitPrice })
            .ToListAsync(ct);

        return itemData
            .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.ImageUrl })
            .Select(g => new TopSellingProductDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.ImageUrl,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.UnitPrice * oi.Quantity)))
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(count)
            .ToList();
    }

    private static async Task<IReadOnlyList<LowStockProductDto>> GetLowStockProductsAsync(
        DbSet<ProductVariant> productVariants,
        int threshold,
        CancellationToken ct)
    {
        // OrderBy before Select to avoid DTO constructor in SQL translation
        // Use navigation property instead of explicit Join to work with global query filters
        var lowStock = await productVariants
            .TagWith("Dashboard_LowStockProducts")
            .Where(pv => pv.StockQuantity <= threshold && pv.StockQuantity >= 0)
            .OrderBy(pv => pv.StockQuantity)
            .Take(20)
            .Select(pv => new LowStockProductDto(
                pv.Product.Id,
                pv.Id,
                pv.Product.Name,
                pv.Name,
                pv.Sku,
                pv.StockQuantity,
                threshold))
            .ToListAsync(ct);

        return lowStock;
    }

    private static async Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(
        DbSet<Domain.Entities.Order.Order> orders,
        int count,
        CancellationToken ct)
    {
        var recentOrders = await orders
            .TagWith("Dashboard_RecentOrders")
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .Select(o => new RecentOrderDto(
                o.Id,
                o.OrderNumber,
                o.CustomerEmail,
                o.GrandTotal,
                o.Status,
                o.CreatedAt))
            .ToListAsync(ct);

        return recentOrders;
    }

    private static async Task<IReadOnlyList<SalesOverTimeDto>> GetSalesOverTimeAsync(
        DbSet<Domain.Entities.Order.Order> orders,
        DateTimeOffset salesStartDate,
        CancellationToken ct)
    {
        // Materialize first, then group in memory to avoid GroupBy(o.CreatedAt.Date) translation issues
        var rawData = await orders
            .TagWith("Dashboard_SalesOverTime")
            .Where(o => o.CreatedAt >= salesStartDate && ValidRevenueStatuses.Contains(o.Status))
            .Select(o => new { o.CreatedAt, o.GrandTotal })
            .ToListAsync(ct);

        return rawData
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt.Date))
            .Select(g => new SalesOverTimeDto(g.Key, g.Sum(o => o.GrandTotal), g.Count()))
            .OrderBy(x => x.Date)
            .ToList();
    }

    private static async Task<ProductStatusDistributionDto> GetProductStatusDistributionAsync(
        DbSet<Product> products,
        CancellationToken ct)
    {
        var counts = await products
            .TagWith("Dashboard_ProductStatusDistribution")
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dict = counts.ToDictionary(x => x.Status, x => x.Count);

        return new ProductStatusDistributionDto(
            dict.GetValueOrDefault(ProductStatus.Draft),
            dict.GetValueOrDefault(ProductStatus.Active),
            dict.GetValueOrDefault(ProductStatus.Archived));
    }
}
