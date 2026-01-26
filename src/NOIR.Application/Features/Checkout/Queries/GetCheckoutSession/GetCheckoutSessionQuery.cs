namespace NOIR.Application.Features.Checkout.Queries.GetCheckoutSession;

/// <summary>
/// Query to get a checkout session by ID.
/// </summary>
public sealed record GetCheckoutSessionQuery(Guid SessionId);
