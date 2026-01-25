namespace NOIR.Application.Features.Payments.Queries.GetPaymentTransactions;

/// <summary>
/// Query to get a paginated list of payment transactions.
/// </summary>
public sealed record GetPaymentTransactionsQuery(
    string? Search = null,
    PaymentStatus? Status = null,
    PaymentMethod? PaymentMethod = null,
    string? Provider = null,
    int Page = 1,
    int PageSize = 20);
