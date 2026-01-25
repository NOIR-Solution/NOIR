namespace NOIR.Application.Features.Payments.Queries.GetRefunds;

/// <summary>
/// Query to get refunds for a payment transaction.
/// </summary>
public sealed record GetRefundsQuery(Guid PaymentTransactionId);
