namespace NOIR.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of report query service.
/// Uses direct DbContext access for efficient aggregation queries.
/// Follows the same pattern as DashboardQueryService.
///
/// NOTE: Queries avoid explicit .Join() and navigation-based .Where() on OrderItem → Order
/// because EF Core cannot translate these with global query filters (IsDeleted, TenantId).
/// Instead, we pre-filter valid Order IDs and use .Contains() or start from the Order side.
/// </summary>
public class ReportQueryService : IReportQueryService, IScopedService
{
    private readonly ApplicationDbContext _context;

    public ReportQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ─── Valid order statuses for revenue calculations ─────────────────────

    private static readonly OrderStatus[] ValidRevenueStatuses =
    [
        OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped,
        OrderStatus.Delivered, OrderStatus.Completed
    ];

    // ─── Revenue Report ───────────────────────────────────────────────────

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        string period,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var orders = _context.Set<Domain.Entities.Order.Order>();
        var payments = _context.PaymentTransactions;

        var validOrders = orders
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

        // DbContext is not thread-safe - run queries sequentially
        var totalRevenue = await validOrders
            .TagWith("Report_Revenue_Total")
            .SumAsync(o => (decimal?)o.GrandTotal ?? 0, cancellationToken);

        var totalOrders = await validOrders
            .TagWith("Report_Revenue_OrderCount")
            .CountAsync(cancellationToken);

        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        // Revenue by day - query Order directly (no navigation issues)
        var revenueByDay = await GetRevenueByDayAsync(validOrders, cancellationToken);

        // Revenue by category - use Order.Items SelectMany pattern
        var revenueByCategory = await GetRevenueByCategoryAsync(
            validOrders, cancellationToken);

        // Revenue by payment method - simple GroupBy on PaymentTransaction
        var revenueByPaymentMethod = await GetRevenueByPaymentMethodAsync(
            payments, startDate, endDate, cancellationToken);

        // Period comparison
        var periodDuration = endDate - startDate;
        var previousStart = startDate - periodDuration;
        var previousEnd = startDate;

        var previousRevenue = await orders
            .TagWith("Report_Revenue_PreviousPeriod")
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < previousEnd)
            .SumAsync(o => (decimal?)o.GrandTotal ?? 0, cancellationToken);

        var previousOrderCount = await orders
            .TagWith("Report_Revenue_PreviousPeriodCount")
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < previousEnd)
            .CountAsync(cancellationToken);

        var revenueChange = previousRevenue > 0
            ? ((totalRevenue - previousRevenue) / previousRevenue) * 100
            : totalRevenue > 0 ? 100m : 0m;

        var orderCountChange = previousOrderCount > 0
            ? ((decimal)(totalOrders - previousOrderCount) / previousOrderCount) * 100
            : totalOrders > 0 ? 100m : 0m;

        return new RevenueReportDto(
            period,
            startDate,
            endDate,
            totalRevenue,
            totalOrders,
            avgOrderValue,
            revenueByDay,
            revenueByCategory,
            revenueByPaymentMethod,
            new RevenueComparisonDto(
                Math.Round(revenueChange, 2),
                Math.Round(orderCountChange, 2)));
    }

    private static async Task<IReadOnlyList<DailyRevenueDto>> GetRevenueByDayAsync(
        IQueryable<Domain.Entities.Order.Order> validOrders,
        CancellationToken ct)
    {
        // Materialize daily data first, then project to DTO in memory
        // This avoids EF Core translation issues with DateTimeOffset.Date in GroupBy
        var dailyData = await validOrders
            .TagWith("Report_Revenue_ByDay")
            .Select(o => new { o.CreatedAt, o.GrandTotal })
            .ToListAsync(ct);

        return dailyData
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt.Date))
            .Select(g => new DailyRevenueDto(g.Key, g.Sum(o => o.GrandTotal), g.Count()))
            .OrderBy(x => x.Date)
            .ToList();
    }

    private async Task<IReadOnlyList<CategoryRevenueDto>> GetRevenueByCategoryAsync(
        IQueryable<Domain.Entities.Order.Order> validOrders,
        CancellationToken ct)
    {
        // Start from valid orders and SelectMany to Items, then join to Product for CategoryId.
        // This avoids the OrderItem → Order navigation translation issue.
        var products = _context.Products;
        var categories = _context.ProductCategories;

        // Materialize order items from valid orders
        var orderItemData = await validOrders
            .TagWith("Report_Revenue_ByCategory")
            .SelectMany(o => o.Items, (o, oi) => new { oi.ProductId, oi.UnitPrice, oi.Quantity, oi.OrderId })
            .ToListAsync(ct);

        // Get product-to-category mapping
        var productCategories = await products
            .Where(p => p.CategoryId != null)
            .Select(p => new { p.Id, p.CategoryId })
            .ToListAsync(ct);

        var categoryNames = await categories
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        // Aggregate in memory
        var productCategoryMap = productCategories.ToDictionary(p => p.Id, p => p.CategoryId!.Value);
        var categoryNameMap = categoryNames.ToDictionary(c => c.Id, c => c.Name);

        return orderItemData
            .Where(oi => productCategoryMap.ContainsKey(oi.ProductId))
            .GroupBy(oi => productCategoryMap[oi.ProductId])
            .Select(g => new CategoryRevenueDto(
                g.Key,
                categoryNameMap.GetValueOrDefault(g.Key, "Unknown"),
                g.Sum(x => x.UnitPrice * x.Quantity),
                g.Select(x => x.OrderId).Distinct().Count()))
            .OrderByDescending(x => x.Revenue)
            .ToList();
    }

    private async Task<IReadOnlyList<PaymentMethodRevenueDto>> GetRevenueByPaymentMethodAsync(
        DbSet<PaymentTransaction> payments,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken ct)
    {
        var paidStatuses = new[] { PaymentStatus.Paid, PaymentStatus.CodCollected };

        // Materialize then group in memory to avoid enum GroupBy translation issues
        var paymentData = await payments
            .TagWith("Report_Revenue_ByPaymentMethod")
            .Where(p => paidStatuses.Contains(p.Status))
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .Select(p => new { p.PaymentMethod, p.Amount })
            .ToListAsync(ct);

        return paymentData
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new PaymentMethodRevenueDto(
                g.Key.ToString(), g.Sum(p => p.Amount), g.Count()))
            .OrderByDescending(x => x.Revenue)
            .ToList();
    }

    // ─── Best Sellers Report ──────────────────────────────────────────────

    public async Task<BestSellersReportDto> GetBestSellersAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var orders = _context.Set<Domain.Entities.Order.Order>();

        // Start from Order side to avoid OrderItem → Order navigation issues
        var itemData = await orders
            .TagWith("Report_BestSellers")
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .SelectMany(o => o.Items, (o, oi) => new { oi.ProductId, oi.ProductName, oi.ImageUrl, oi.Quantity, oi.UnitPrice })
            .ToListAsync(cancellationToken);

        var bestSellers = itemData
            .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.ImageUrl })
            .Select(g => new BestSellerDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.ImageUrl,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.UnitPrice * oi.Quantity)))
            .OrderByDescending(x => x.UnitsSold)
            .Take(topN)
            .ToList();

        return new BestSellersReportDto(
            bestSellers,
            "custom",
            startDate,
            endDate);
    }

    // ─── Inventory Report ─────────────────────────────────────────────────

    public async Task<InventoryReportDto> GetInventoryReportAsync(
        int lowStockThreshold,
        CancellationToken cancellationToken = default)
    {
        var products = _context.Products;
        var productVariants = _context.ProductVariants;
        var orders = _context.Set<Domain.Entities.Order.Order>();

        // Low stock products - OrderBy before Select to avoid DTO constructor in translation
        var lowStock = await productVariants
            .TagWith("Report_Inventory_LowStock")
            .Where(pv => pv.StockQuantity <= lowStockThreshold && pv.StockQuantity >= 0)
            .OrderBy(pv => pv.StockQuantity)
            .Take(50)
            .Select(pv => new LowStockDto(
                pv.Product.Id,
                pv.Product.Name,
                pv.Sku,
                pv.StockQuantity,
                lowStockThreshold))
            .ToListAsync(cancellationToken);

        // Total counts
        var totalProducts = await products
            .TagWith("Report_Inventory_TotalProducts")
            .CountAsync(cancellationToken);

        var totalVariants = await productVariants
            .TagWith("Report_Inventory_TotalVariants")
            .CountAsync(cancellationToken);

        // Total stock value (sum of price * quantity across all variants)
        var totalStockValue = await productVariants
            .TagWith("Report_Inventory_StockValue")
            .SumAsync(pv => (decimal?)pv.Price * pv.StockQuantity ?? 0, cancellationToken);

        // Turnover rate = units sold in last 30 days / average inventory
        // Start from Order side to avoid OrderItem → Order navigation issues
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var unitsSold = await orders
            .TagWith("Report_Inventory_UnitsSold30Days")
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .Where(o => o.CreatedAt >= thirtyDaysAgo)
            .SelectMany(o => o.Items)
            .SumAsync(oi => (int?)oi.Quantity ?? 0, cancellationToken);

        var totalCurrentStock = await productVariants
            .TagWith("Report_Inventory_TotalCurrentStock")
            .SumAsync(pv => (int?)pv.StockQuantity ?? 0, cancellationToken);

        var turnoverRate = totalCurrentStock > 0
            ? Math.Round((decimal)unitsSold / totalCurrentStock, 2)
            : 0m;

        return new InventoryReportDto(
            lowStock,
            totalProducts,
            totalVariants,
            totalStockValue,
            turnoverRate);
    }

    // ─── Customer Report ──────────────────────────────────────────────────

    public async Task<CustomerReportDto> GetCustomerReportAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var orders = _context.Set<Domain.Entities.Order.Order>();

        // Materialize order data for customer analysis to avoid complex GroupBy translation
        // Only load from previous period start onward to avoid full table scan
        var periodDuration = endDate - startDate;
        var dataFloor = startDate - periodDuration;

        var orderData = await orders
            .TagWith("Report_Customer_OrderData")
            .Where(o => o.CustomerId != null)
            .Where(o => o.CreatedAt >= dataFloor)
            .Select(o => new { o.CustomerId, o.CustomerName, o.CreatedAt, o.Status, o.GrandTotal })
            .ToListAsync(cancellationToken);

        // New customers: unique CustomerId with first order in this period
        var firstOrderByCustomer = orderData
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, FirstOrder = g.Min(o => o.CreatedAt) })
            .ToList();

        var newCustomers = firstOrderByCustomer
            .Count(x => x.FirstOrder >= startDate && x.FirstOrder <= endDate);

        // Returning customers: ordered before the period AND during the period
        var customersInPeriod = orderData
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .Select(o => o.CustomerId)
            .Distinct()
            .ToHashSet();

        var customersBeforePeriod = orderData
            .Where(o => o.CreatedAt < startDate)
            .Select(o => o.CustomerId)
            .Distinct()
            .ToHashSet();

        var returningCustomers = customersInPeriod.Count(c => customersBeforePeriod.Contains(c));

        // Churn rate: customers who ordered in previous period but not in current period
        var previousStart = startDate - periodDuration;
        var previousEnd = startDate;

        var previousPeriodCustomers = orderData
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < previousEnd)
            .Select(o => o.CustomerId)
            .Distinct()
            .ToHashSet();

        var churnedCount = previousPeriodCustomers.Count(c => !customersInPeriod.Contains(c));

        var churnRate = previousPeriodCustomers.Count > 0
            ? Math.Round((decimal)churnedCount / previousPeriodCustomers.Count * 100, 2)
            : 0m;

        // Monthly acquisition
        var newCustomersByMonth = firstOrderByCustomer
            .Where(x => x.FirstOrder >= startDate && x.FirstOrder <= endDate)
            .GroupBy(x => new { x.FirstOrder.Year, x.FirstOrder.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .ToList();

        var validOrderData = orderData
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .ToList();

        var monthlyAcquisition = newCustomersByMonth
            .Select(g =>
            {
                var monthLabel = $"{g.Key.Year}-{g.Key.Month:D2}";
                var monthStart = new DateTimeOffset(g.Key.Year, g.Key.Month, 1, 0, 0, 0, TimeSpan.Zero);
                var monthEnd = monthStart.AddMonths(1);

                var monthRevenue = validOrderData
                    .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd)
                    .Sum(o => o.GrandTotal);

                return new MonthlyAcquisitionDto(monthLabel, g.Count(), monthRevenue);
            })
            .ToList();

        // Top customers by spending
        var topCustomers = validOrderData
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .GroupBy(o => new { o.CustomerId, o.CustomerName })
            .Select(g => new TopCustomerDto(
                g.Key.CustomerId,
                g.Key.CustomerName ?? "Unknown",
                g.Sum(o => o.GrandTotal),
                g.Count()))
            .OrderByDescending(x => x.TotalSpent)
            .Take(10)
            .ToList();

        return new CustomerReportDto(
            newCustomers,
            returningCustomers,
            churnRate,
            monthlyAcquisition,
            topCustomers);
    }

    // ─── Export ───────────────────────────────────────────────────────────

    public async Task<ExportResultDto> ExportReportAsync(
        ReportType reportType,
        ExportFormat format,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var effectiveStart = startDate ?? now.AddDays(-30);
        var effectiveEnd = endDate ?? now;

        var csvContent = reportType switch
        {
            ReportType.Revenue =>
                await ExportRevenueAsync(effectiveStart, effectiveEnd, cancellationToken),
            ReportType.BestSellers =>
                await ExportBestSellersAsync(effectiveStart, effectiveEnd, cancellationToken),
            ReportType.Inventory =>
                await ExportInventoryAsync(cancellationToken),
            ReportType.CustomerAcquisition =>
                await ExportCustomersAsync(effectiveStart, effectiveEnd, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(reportType))
        };

        var fileBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvContent)).ToArray();
        var timestamp = now.ToString("yyyyMMdd-HHmmss");
        var fileName = $"{reportType}-report-{timestamp}.csv";

        return new ExportResultDto(
            fileBytes,
            "text/csv",
            fileName);
    }

    private async Task<string> ExportRevenueAsync(
        DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct)
    {
        var report = await GetRevenueReportAsync("daily", startDate, endDate, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Date,Revenue,OrderCount");
        foreach (var day in report.RevenueByDay)
        {
            sb.AppendLine($"{day.Date:yyyy-MM-dd},{day.Revenue},{day.OrderCount}");
        }

        sb.AppendLine();
        sb.AppendLine("Category,Revenue,OrderCount");
        foreach (var cat in report.RevenueByCategory)
        {
            sb.AppendLine($"\"{EscapeCsv(cat.CategoryName)}\",{cat.Revenue},{cat.OrderCount}");
        }

        sb.AppendLine();
        sb.AppendLine("PaymentMethod,Revenue,Count");
        foreach (var pm in report.RevenueByPaymentMethod)
        {
            sb.AppendLine($"{pm.Method},{pm.Revenue},{pm.Count}");
        }

        return sb.ToString();
    }

    private async Task<string> ExportBestSellersAsync(
        DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct)
    {
        var report = await GetBestSellersAsync(startDate, endDate, 50, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Rank,ProductId,ProductName,UnitsSold,Revenue");
        var rank = 1;
        foreach (var p in report.Products)
        {
            sb.AppendLine($"{rank},\"{p.ProductId}\",\"{EscapeCsv(p.ProductName)}\",{p.UnitsSold},{p.Revenue}");
            rank++;
        }

        return sb.ToString();
    }

    private async Task<string> ExportInventoryAsync(CancellationToken ct)
    {
        var report = await GetInventoryReportAsync(int.MaxValue, ct);

        var sb = new StringBuilder();
        sb.AppendLine("ProductId,ProductName,VariantSku,CurrentStock,ReorderLevel");
        foreach (var item in report.LowStockProducts)
        {
            sb.AppendLine($"\"{item.ProductId}\",\"{EscapeCsv(item.Name)}\",\"{EscapeCsv(item.VariantSku)}\",{item.CurrentStock},{item.ReorderLevel}");
        }

        sb.AppendLine();
        sb.AppendLine($"TotalProducts,{report.TotalProducts}");
        sb.AppendLine($"TotalVariants,{report.TotalVariants}");
        sb.AppendLine($"TotalStockValue,{report.TotalStockValue}");
        sb.AppendLine($"TurnoverRate,{report.TurnoverRate}");

        return sb.ToString();
    }

    private async Task<string> ExportCustomersAsync(
        DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct)
    {
        var report = await GetCustomerReportAsync(startDate, endDate, ct);

        var sb = new StringBuilder();
        sb.AppendLine($"NewCustomers,{report.NewCustomers}");
        sb.AppendLine($"ReturningCustomers,{report.ReturningCustomers}");
        sb.AppendLine($"ChurnRate,{report.ChurnRate}%");
        sb.AppendLine();

        sb.AppendLine("Month,NewCustomers,Revenue");
        foreach (var m in report.AcquisitionByMonth)
        {
            sb.AppendLine($"{m.Month},{m.NewCustomers},{m.Revenue}");
        }

        sb.AppendLine();
        sb.AppendLine("CustomerId,Name,TotalSpent,OrderCount");
        foreach (var c in report.TopCustomers)
        {
            sb.AppendLine($"\"{c.CustomerId}\",\"{EscapeCsv(c.Name)}\",{c.TotalSpent},{c.OrderCount}");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"");
    }
}
