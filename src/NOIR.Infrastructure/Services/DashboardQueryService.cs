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
        var orderItems = _context.Set<OrderItem>();
        var products = _context.Products;
        var productVariants = _context.ProductVariants;

        // Run aggregation queries sequentially - DbContext is not thread-safe
        // and does not support multiple concurrent operations on the same instance.
        var revenue = await GetRevenueMetricsAsync(orders, startOfMonth, startOfLastMonth, startOfToday, cancellationToken);
        var orderCounts = await GetOrderStatusCountsAsync(orders, cancellationToken);
        var topSelling = await GetTopSellingProductsAsync(orderItems, orders, topProductsCount, cancellationToken);
        var lowStock = await GetLowStockProductsAsync(products, productVariants, lowStockThreshold, cancellationToken);
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
        // Exclude cancelled and refunded orders from revenue
        var validStatuses = new[]
        {
            OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped,
            OrderStatus.Delivered, OrderStatus.Completed
        };

        var validOrders = orders.Where(o => validStatuses.Contains(o.Status));

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
        DbSet<OrderItem> orderItems,
        DbSet<Domain.Entities.Order.Order> orders,
        int count,
        CancellationToken ct)
    {
        // Only count items from non-cancelled/refunded orders
        var validStatuses = new[]
        {
            OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped,
            OrderStatus.Delivered, OrderStatus.Completed
        };

        var topProducts = await orderItems
            .TagWith("Dashboard_TopSellingProducts")
            .Where(oi => validStatuses.Contains(oi.Order!.Status))
            .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.ImageUrl })
            .Select(g => new TopSellingProductDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.ImageUrl,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.UnitPrice * oi.Quantity)))
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(count)
            .ToListAsync(ct);

        return topProducts;
    }

    private static async Task<IReadOnlyList<LowStockProductDto>> GetLowStockProductsAsync(
        DbSet<Product> products,
        DbSet<ProductVariant> productVariants,
        int threshold,
        CancellationToken ct)
    {
        var lowStock = await productVariants
            .TagWith("Dashboard_LowStockProducts")
            .Where(pv => pv.StockQuantity <= threshold && pv.StockQuantity >= 0)
            .Join(products,
                pv => pv.ProductId,
                p => p.Id,
                (pv, p) => new LowStockProductDto(
                    p.Id,
                    pv.Id,
                    p.Name,
                    pv.Name,
                    pv.Sku,
                    pv.StockQuantity,
                    threshold))
            .OrderBy(x => x.StockQuantity)
            .Take(20)
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
        var validStatuses = new[]
        {
            OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped,
            OrderStatus.Delivered, OrderStatus.Completed
        };

        var salesData = await orders
            .TagWith("Dashboard_SalesOverTime")
            .Where(o => o.CreatedAt >= salesStartDate && validStatuses.Contains(o.Status))
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.GrandTotal),
                OrderCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return salesData.Select(x =>
            new SalesOverTimeDto(DateOnly.FromDateTime(x.Date), x.Revenue, x.OrderCount))
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
