namespace NOIR.Application.Features.Shipping.Queries.GetShippingTracking;

/// <summary>
/// Query to get tracking information for a shipment.
/// </summary>
public sealed record GetShippingTrackingQuery(string TrackingNumber);
