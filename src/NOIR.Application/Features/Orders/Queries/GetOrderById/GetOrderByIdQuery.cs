namespace NOIR.Application.Features.Orders.Queries.GetOrderById;

/// <summary>
/// Query to get an order by ID.
/// </summary>
public sealed record GetOrderByIdQuery(Guid OrderId);
