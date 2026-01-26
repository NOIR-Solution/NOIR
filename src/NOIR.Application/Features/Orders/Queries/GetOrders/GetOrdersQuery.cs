namespace NOIR.Application.Features.Orders.Queries.GetOrders;

/// <summary>
/// Query to get orders with pagination and filtering.
/// </summary>
public sealed record GetOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    string? CustomerEmail = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
