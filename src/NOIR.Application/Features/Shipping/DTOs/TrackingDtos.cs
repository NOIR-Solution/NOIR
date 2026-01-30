namespace NOIR.Application.Features.Shipping.DTOs;

/// <summary>
/// A single tracking event in the shipment history.
/// </summary>
public record TrackingEventDto(
    string EventType,
    ShippingStatus Status,
    string Description,
    string? Location,
    DateTimeOffset EventDate);

/// <summary>
/// Complete tracking information for a shipment.
/// </summary>
public record TrackingInfoDto(
    string TrackingNumber,
    ShippingProviderCode ProviderCode,
    string ProviderName,
    ShippingStatus CurrentStatus,
    string StatusDescription,
    string? CurrentLocation,
    DateTimeOffset? EstimatedDeliveryDate,
    DateTimeOffset? ActualDeliveryDate,
    List<TrackingEventDto> Events,
    string? TrackingUrl);

/// <summary>
/// Webhook payload received from shipping providers.
/// </summary>
public record ShippingWebhookPayload(
    string ProviderCode,
    string TrackingNumber,
    string EventType,
    string Status,
    string Description,
    string? Location,
    DateTimeOffset EventDate,
    string RawPayload);
