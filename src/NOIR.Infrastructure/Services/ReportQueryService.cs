namespace NOIR.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of report query service.
/// Uses direct DbContext access for efficient aggregation queries.
/// Follows the same pattern as DashboardQueryService.
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
        var orderItems = _context.Set<OrderItem>();
        var payments = _context.PaymentTransactions;
        var categories = _context.ProductCategories;

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

        // Revenue by day
        var revenueByDay = await GetRevenueByDayAsync(validOrders, cancellationToken);

        // Revenue by category
        var revenueByCategory = await GetRevenueByCategoryAsync(
            orderItems, categories, startDate, endDate, cancellationToken);

        // Revenue by payment method
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
        var dailyData = await validOrders
            .TagWith("Report_Revenue_ByDay")
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.GrandTotal),
                OrderCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return dailyData
            .Select(x => new DailyRevenueDto(
                DateOnly.FromDateTime(x.Date), x.Revenue, x.OrderCount))
            .ToList();
    }

    private static async Task<IReadOnlyList<CategoryRevenueDto>> GetRevenueByCategoryAsync(
        DbSet<OrderItem> orderItems,
        DbSet<ProductCategory> categories,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken ct)
    {
        var categoryRevenue = await orderItems
            .TagWith("Report_Revenue_ByCategory")
            .Where(oi => ValidRevenueStatuses.Contains(oi.Order!.Status))
            .Where(oi => oi.Order!.CreatedAt >= startDate && oi.Order!.CreatedAt <= endDate)
            .Join(
                categories.SelectMany(c => c.Products, (c, p) => new { c.Id, CategoryName = c.Name, p }),
                oi => oi.ProductId,
                cp => cp.p.Id,
                (oi, cp) => new { cp.Id, cp.CategoryName, Revenue = oi.UnitPrice * oi.Quantity, oi.OrderId })
            .GroupBy(x => new { CategoryId = x.Id, x.CategoryName })
            .Select(g => new CategoryRevenueDto(
                g.Key.CategoryId,
                g.Key.CategoryName,
                g.Sum(x => x.Revenue),
                g.Select(x => x.OrderId).Distinct().Count()))
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(ct);

        return categoryRevenue;
    }

    private async Task<IReadOnlyList<PaymentMethodRevenueDto>> GetRevenueByPaymentMethodAsync(
        DbSet<PaymentTransaction> payments,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken ct)
    {
        var paidStatuses = new[] { PaymentStatus.Paid, PaymentStatus.CodCollected };

        var paymentData = await payments
            .TagWith("Report_Revenue_ByPaymentMethod")
            .Where(p => paidStatuses.Contains(p.Status))
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new
            {
                Method = g.Key,
                Revenue = g.Sum(p => p.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(ct);

        return paymentData
            .Select(x => new PaymentMethodRevenueDto(
                x.Method.ToString(), x.Revenue, x.Count))
            .ToList();
    }

    // ─── Best Sellers Report ──────────────────────────────────────────────

    public async Task<BestSellersReportDto> GetBestSellersAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var orderItems = _context.Set<OrderItem>();

        var bestSellers = await orderItems
            .TagWith("Report_BestSellers")
            .Where(oi => ValidRevenueStatuses.Contains(oi.Order!.Status))
            .Where(oi => oi.Order!.CreatedAt >= startDate && oi.Order!.CreatedAt <= endDate)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.ImageUrl })
            .Select(g => new BestSellerDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.ImageUrl,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.UnitPrice * oi.Quantity)))
            .OrderByDescending(x => x.UnitsSold)
            .Take(topN)
            .ToListAsync(cancellationToken);

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
        var orderItems = _context.Set<OrderItem>();

        // Low stock products
        var lowStock = await productVariants
            .TagWith("Report_Inventory_LowStock")
            .Where(pv => pv.StockQuantity <= lowStockThreshold && pv.StockQuantity >= 0)
            .Join(products,
                pv => pv.ProductId,
                p => p.Id,
                (pv, p) => new LowStockDto(
                    p.Id,
                    p.Name,
                    pv.Sku,
                    pv.StockQuantity,
                    lowStockThreshold))
            .OrderBy(x => x.CurrentStock)
            .Take(50)
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
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var unitsSold = await orderItems
            .TagWith("Report_Inventory_UnitsSold30Days")
            .Where(oi => ValidRevenueStatuses.Contains(oi.Order!.Status))
            .Where(oi => oi.Order!.CreatedAt >= thirtyDaysAgo)
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

        // New customers: unique CustomerId with first order in this period
        var allCustomerFirstOrders = orders
            .TagWith("Report_Customer_FirstOrders")
            .Where(o => o.CustomerId != null)
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, FirstOrder = g.Min(o => o.CreatedAt) });

        var newCustomers = await allCustomerFirstOrders
            .Where(x => x.FirstOrder >= startDate && x.FirstOrder <= endDate)
            .CountAsync(cancellationToken);

        // Returning customers: ordered before the period AND during the period
        var customersInPeriod = orders
            .Where(o => o.CustomerId != null)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .Select(o => o.CustomerId)
            .Distinct();

        var customersBeforePeriod = orders
            .Where(o => o.CustomerId != null)
            .Where(o => o.CreatedAt < startDate)
            .Select(o => o.CustomerId)
            .Distinct();

        var returningCustomers = await customersInPeriod
            .TagWith("Report_Customer_Returning")
            .Where(c => customersBeforePeriod.Contains(c))
            .CountAsync(cancellationToken);

        // Churn rate: customers who ordered in previous period but not in current period
        var periodDuration = endDate - startDate;
        var previousStart = startDate - periodDuration;
        var previousEnd = startDate;

        var previousPeriodCustomers = orders
            .Where(o => o.CustomerId != null)
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < previousEnd)
            .Select(o => o.CustomerId)
            .Distinct();

        var previousPeriodCount = await previousPeriodCustomers
            .TagWith("Report_Customer_PreviousPeriodCount")
            .CountAsync(cancellationToken);

        var churnedCount = await previousPeriodCustomers
            .TagWith("Report_Customer_Churned")
            .Where(c => !customersInPeriod.Contains(c))
            .CountAsync(cancellationToken);

        var churnRate = previousPeriodCount > 0
            ? Math.Round((decimal)churnedCount / previousPeriodCount * 100, 2)
            : 0m;

        // Monthly acquisition
        var acquisitionByMonth = await allCustomerFirstOrders
            .TagWith("Report_Customer_MonthlyAcquisition")
            .Where(x => x.FirstOrder >= startDate && x.FirstOrder <= endDate)
            .GroupBy(x => new { x.FirstOrder.Year, x.FirstOrder.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        // Get revenue for each month's new customers
        var monthlyAcquisition = new List<MonthlyAcquisitionDto>();
        foreach (var m in acquisitionByMonth)
        {
            var monthLabel = $"{m.Year}-{m.Month:D2}";
            var monthStart = new DateTimeOffset(m.Year, m.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var monthEnd = monthStart.AddMonths(1);

            var monthRevenue = await orders
                .TagWith("Report_Customer_MonthlyRevenue")
                .Where(o => ValidRevenueStatuses.Contains(o.Status))
                .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd)
                .SumAsync(o => (decimal?)o.GrandTotal ?? 0, cancellationToken);

            monthlyAcquisition.Add(new MonthlyAcquisitionDto(
                monthLabel, m.Count, monthRevenue));
        }

        // Top customers by spending
        var topCustomers = await orders
            .TagWith("Report_Customer_TopSpenders")
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .Where(o => o.CustomerId != null)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .GroupBy(o => new { o.CustomerId, o.CustomerName })
            .Select(g => new TopCustomerDto(
                g.Key.CustomerId,
                g.Key.CustomerName ?? "Unknown",
                g.Sum(o => o.GrandTotal),
                g.Count()))
            .OrderByDescending(x => x.TotalSpent)
            .Take(10)
            .ToListAsync(cancellationToken);

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
