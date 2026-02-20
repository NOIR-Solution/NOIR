namespace NOIR.Application.Features.Payments.Queries.GetPaymentTimeline;

/// <summary>
/// Query to get payment event timeline.
/// </summary>
public sealed record GetPaymentTimelineQuery(Guid PaymentTransactionId);
