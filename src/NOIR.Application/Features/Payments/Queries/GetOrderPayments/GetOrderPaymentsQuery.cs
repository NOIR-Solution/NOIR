namespace NOIR.Application.Features.Payments.Queries.GetOrderPayments;

/// <summary>
/// Query to get all payment transactions for an order.
/// </summary>
public sealed record GetOrderPaymentsQuery(Guid OrderId);
