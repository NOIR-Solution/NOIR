namespace NOIR.Application.Features.Orders.Specifications;

/// <summary>
/// Specification to get an order by ID with items loaded.
/// </summary>
public sealed class OrderByIdSpec : Specification<Order>
{
    public OrderByIdSpec(Guid orderId)
    {
        Query.Where(o => o.Id == orderId)
            .Include(o => o.Items)
            .TagWith("OrderById");
    }
}

/// <summary>
/// Specification to get an order by ID for update (with tracking).
/// </summary>
public sealed class OrderByIdForUpdateSpec : Specification<Order>
{
    public OrderByIdForUpdateSpec(Guid orderId)
    {
        Query.Where(o => o.Id == orderId)
            .Include(o => o.Items)
            .AsTracking()
            .TagWith("OrderByIdForUpdate");
    }
}

/// <summary>
/// Specification to get an order by order number.
/// </summary>
public sealed class OrderByNumberSpec : Specification<Order>
{
    public OrderByNumberSpec(string orderNumber, string? tenantId = null)
    {
        Query.Where(o => o.OrderNumber == orderNumber);

        if (!string.IsNullOrEmpty(tenantId))
        {
            Query.Where(o => o.TenantId == tenantId);
        }

        Query.Include(o => o.Items)
            .TagWith("OrderByNumber");
    }
}

/// <summary>
/// Specification to get orders by customer ID.
/// </summary>
public sealed class OrdersByCustomerIdSpec : Specification<Order>
{
    public OrdersByCustomerIdSpec(Guid customerId, int? skip = null, int? take = null)
    {
        Query.Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .TagWith("OrdersByCustomerId");

        if (skip.HasValue)
            Query.Skip(skip.Value);

        if (take.HasValue)
            Query.Take(take.Value);
    }
}

/// <summary>
/// Specification to get orders by status.
/// </summary>
public sealed class OrdersByStatusSpec : Specification<Order>
{
    public OrdersByStatusSpec(OrderStatus status, int? skip = null, int? take = null)
    {
        Query.Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .TagWith("OrdersByStatus");

        if (skip.HasValue)
            Query.Skip(skip.Value);

        if (take.HasValue)
            Query.Take(take.Value);
    }
}

/// <summary>
/// Specification to get all orders with pagination.
/// </summary>
public sealed class OrdersListSpec : Specification<Order>
{
    public OrdersListSpec(
        int skip = 0,
        int take = 20,
        OrderStatus? status = null,
        string? customerEmail = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? orderBy = null,
        bool isDescending = true)
    {
        Query.TagWith("OrdersList");

        if (status.HasValue)
            Query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrEmpty(customerEmail))
            Query.Where(o => o.CustomerEmail.Contains(customerEmail));

        if (fromDate.HasValue)
            Query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            Query.Where(o => o.CreatedAt <= toDate.Value);

        // Sorting
        switch (orderBy?.ToLowerInvariant())
        {
            case "ordernumber":
                if (isDescending) Query.OrderByDescending(o => o.OrderNumber);
                else Query.OrderBy(o => o.OrderNumber);
                break;
            case "status":
                if (isDescending) Query.OrderByDescending(o => o.Status);
                else Query.OrderBy(o => o.Status);
                break;
            case "grandtotal":
                if (isDescending) Query.OrderByDescending(o => o.GrandTotal);
                else Query.OrderBy(o => o.GrandTotal);
                break;
            case "customer":
            case "customeremail":
                if (isDescending) Query.OrderByDescending(o => o.CustomerEmail);
                else Query.OrderBy(o => o.CustomerEmail);
                break;
            case "itemcount":
                if (isDescending) Query.OrderByDescending(o => o.Items.Count);
                else Query.OrderBy(o => o.Items.Count);
                break;
            case "createdat":
                if (isDescending) Query.OrderByDescending(o => o.CreatedAt);
                else Query.OrderBy(o => o.CreatedAt);
                break;
            case "createdby":
            case "creator":
                if (isDescending) Query.OrderByDescending(o => o.CreatedBy);
                else Query.OrderBy(o => o.CreatedBy);
                break;
            case "modifiedby":
            case "editor":
                if (isDescending) Query.OrderByDescending(o => o.ModifiedBy);
                else Query.OrderBy(o => o.ModifiedBy);
                break;
            default:
                Query.OrderByDescending(o => o.CreatedAt);
                break;
        }

        Query.Include(o => o.Items)
            .Skip(skip).Take(take);
    }
}

/// <summary>
/// Specification to count orders matching criteria.
/// </summary>
public sealed class OrdersCountSpec : Specification<Order>
{
    public OrdersCountSpec(
        OrderStatus? status = null,
        string? customerEmail = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        Query.TagWith("OrdersCount");

        if (status.HasValue)
            Query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrEmpty(customerEmail))
            Query.Where(o => o.CustomerEmail.Contains(customerEmail));

        if (fromDate.HasValue)
            Query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            Query.Where(o => o.CreatedAt <= toDate.Value);
    }
}

/// <summary>
/// Specification to get the latest order number for today (for sequence generation).
/// </summary>
public sealed class LatestOrderNumberTodaySpec : Specification<Order>
{
    public LatestOrderNumberTodaySpec(string datePrefix, string? tenantId = null)
    {
        Query.Where(o => o.OrderNumber.StartsWith(datePrefix));

        if (!string.IsNullOrEmpty(tenantId))
        {
            Query.Where(o => o.TenantId == tenantId);
        }

        Query.OrderByDescending(o => o.OrderNumber)
            .TagWith("LatestOrderNumberToday");
    }
}

/// <summary>
/// Specification to count orders for a specific customer.
/// </summary>
public sealed class OrdersByCustomerIdCountSpec : Specification<Order>
{
    public OrdersByCustomerIdCountSpec(Guid customerId)
    {
        Query.Where(o => o.CustomerId == customerId)
            .TagWith("OrdersByCustomerIdCount");
    }
}

/// <summary>
/// Specification to load orders for export with optional filters.
/// </summary>
public sealed class OrdersForExportSpec : Specification<Order>
{
    public OrdersForExportSpec(
        OrderStatus? status = null,
        string? customerEmail = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        if (status.HasValue)
            Query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrEmpty(customerEmail))
            Query.Where(o => o.CustomerEmail.Contains(customerEmail));

        if (fromDate.HasValue)
            Query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            Query.Where(o => o.CreatedAt <= toDate.Value);

        Query.OrderByDescending(o => o.CreatedAt)
            .TagWith("OrdersForExport");
    }
}

/// <summary>
/// Specification to find orders by a list of IDs for bulk update (with tracking).
/// </summary>
public sealed class OrdersByIdsForUpdateSpec : Specification<Order>
{
    public OrdersByIdsForUpdateSpec(List<Guid> ids)
    {
        Query.Where(o => ids.Contains(o.Id)).AsTracking().TagWith("OrdersByIdsForUpdate");
    }
}

/// <summary>
/// Specification to search orders by order number or customer email.
/// </summary>
public sealed class OrderSearchSpec : Specification<Order>
{
    public OrderSearchSpec(string search, int take = 5)
    {
        Query.Where(o =>
                o.OrderNumber.Contains(search) ||
                o.CustomerEmail.Contains(search))
            .OrderByDescending(o => o.CreatedAt)
            .Take(take)
            .TagWith("OrderSearch");
    }
}

