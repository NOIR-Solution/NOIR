namespace NOIR.Application.Features.Shipping.Queries.GetShippingProviderById;

/// <summary>
/// Query to get a shipping provider by ID.
/// </summary>
public sealed record GetShippingProviderByIdQuery(Guid ProviderId);
