namespace NOIR.Application.Features.Payments.Queries.GetPaymentGateway;

/// <summary>
/// Query to get a payment gateway by ID.
/// </summary>
public sealed record GetPaymentGatewayQuery(Guid Id);
