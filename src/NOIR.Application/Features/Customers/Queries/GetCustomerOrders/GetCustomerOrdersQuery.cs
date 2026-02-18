namespace NOIR.Application.Features.Customers.Queries.GetCustomerOrders;

/// <summary>
/// Query to get order history for a specific customer.
/// </summary>
public sealed record GetCustomerOrdersQuery(
    Guid CustomerId,
    int Page = 1,
    int PageSize = 20);
