namespace NOIR.Application.Features.Payments.Queries.GetPaymentTransaction;

/// <summary>
/// Query to get a payment transaction by ID.
/// </summary>
public sealed record GetPaymentTransactionQuery(Guid Id);
