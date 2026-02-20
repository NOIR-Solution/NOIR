namespace NOIR.Application.Features.Payments.Queries.GetPaymentDetails;

/// <summary>
/// Query to get comprehensive payment details with logs, webhooks, and refunds.
/// </summary>
public sealed record GetPaymentDetailsQuery(Guid PaymentTransactionId);
