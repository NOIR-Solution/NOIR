namespace NOIR.Application.Features.Shipping.Queries.GetShippingOrder;

/// <summary>
/// Query to get a shipping order by tracking number.
/// </summary>
public sealed record GetShippingOrderQuery(string TrackingNumber);

/// <summary>
/// Query to get a shipping order by ID.
/// </summary>
public sealed record GetShippingOrderByIdQuery(Guid Id);

/// <summary>
/// Query to get a shipping order by NOIR order ID.
/// </summary>
public sealed record GetShippingOrderByOrderIdQuery(Guid OrderId);
