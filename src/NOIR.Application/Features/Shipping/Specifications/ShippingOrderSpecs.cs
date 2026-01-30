namespace NOIR.Application.Features.Shipping.Specifications;

/// <summary>
/// Get shipping order by ID (read-only).
/// </summary>
public sealed class ShippingOrderByIdSpec : Specification<ShippingOrder>
{
    public ShippingOrderByIdSpec(Guid id)
    {
        Query.Where(o => o.Id == id)
             .Include(o => o.TrackingEvents!)
             .Include(o => o.Provider!)
             .TagWith("ShippingOrderById");
    }
}

/// <summary>
/// Get shipping order by ID for update (with tracking).
/// </summary>
public sealed class ShippingOrderByIdForUpdateSpec : Specification<ShippingOrder>
{
    public ShippingOrderByIdForUpdateSpec(Guid id)
    {
        Query.Where(o => o.Id == id)
             .AsTracking()
             .TagWith("ShippingOrderByIdForUpdate");
    }
}

/// <summary>
/// Get shipping order by tracking number.
/// </summary>
public sealed class ShippingOrderByTrackingNumberSpec : Specification<ShippingOrder>
{
    public ShippingOrderByTrackingNumberSpec(string trackingNumber)
    {
        Query.Where(o => o.TrackingNumber == trackingNumber)
             .Include(o => o.TrackingEvents!)
             .Include(o => o.Provider!)
             .TagWith("ShippingOrderByTrackingNumber");
    }
}

/// <summary>
/// Get shipping order by tracking number for update.
/// </summary>
public sealed class ShippingOrderByTrackingNumberForUpdateSpec : Specification<ShippingOrder>
{
    public ShippingOrderByTrackingNumberForUpdateSpec(string trackingNumber)
    {
        Query.Where(o => o.TrackingNumber == trackingNumber)
             .Include(o => o.TrackingEvents)
             .AsTracking()
             .TagWith("ShippingOrderByTrackingNumberForUpdate");
    }
}

/// <summary>
/// Get shipping order by NOIR order ID.
/// </summary>
public sealed class ShippingOrderByOrderIdSpec : Specification<ShippingOrder>
{
    public ShippingOrderByOrderIdSpec(Guid orderId)
    {
        Query.Where(o => o.OrderId == orderId)
             .Include(o => o.TrackingEvents!)
             .Include(o => o.Provider!)
             .TagWith("ShippingOrderByOrderId");
    }
}

/// <summary>
/// Get shipping orders by status (paginated).
/// </summary>
public sealed class ShippingOrdersByStatusSpec : Specification<ShippingOrder>
{
    public ShippingOrdersByStatusSpec(ShippingStatus? status = null, int page = 1, int pageSize = 20)
    {
        if (status.HasValue)
        {
            Query.Where(o => o.Status == status.Value);
        }

        Query.OrderByDescending(o => o.CreatedAt)
             .Skip((page - 1) * pageSize)
             .Take(pageSize)
             .TagWith("ShippingOrdersByStatus");
    }
}

/// <summary>
/// Get pending shipping orders (awaiting pickup or in transit).
/// </summary>
public sealed class PendingShippingOrdersSpec : Specification<ShippingOrder>
{
    public PendingShippingOrdersSpec()
    {
        Query.Where(o =>
                o.Status == ShippingStatus.AwaitingPickup ||
                o.Status == ShippingStatus.PickedUp ||
                o.Status == ShippingStatus.InTransit ||
                o.Status == ShippingStatus.OutForDelivery)
             .OrderBy(o => o.EstimatedDeliveryDate ?? DateTimeOffset.MaxValue)
             .TagWith("PendingShippingOrders");
    }
}

/// <summary>
/// Get all shipping orders (admin view, paginated).
/// </summary>
public sealed class ShippingOrdersSpec : Specification<ShippingOrder>
{
    public ShippingOrdersSpec(
        ShippingStatus? status = null,
        ShippingProviderCode? providerCode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        if (status.HasValue)
        {
            Query.Where(o => o.Status == status.Value);
        }

        if (providerCode.HasValue)
        {
            Query.Where(o => o.ProviderCode == providerCode.Value);
        }

        if (fromDate.HasValue)
        {
            Query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            Query.Where(o => o.CreatedAt <= toDate.Value);
        }

        Query.OrderByDescending(o => o.CreatedAt)
             .Skip((page - 1) * pageSize)
             .Take(pageSize)
             .Include(o => o.Provider!)
             .TagWith("GetShippingOrders");
    }
}
