namespace NOIR.Application.Features.Shipping.Commands.UpdateShippingProvider;

/// <summary>
/// Command to update an existing shipping provider.
/// </summary>
public sealed record UpdateShippingProviderCommand(
    Guid ProviderId,
    string? DisplayName = null,
    GatewayEnvironment? Environment = null,
    Dictionary<string, string>? Credentials = null,
    List<ShippingServiceType>? SupportedServices = null,
    int? SortOrder = null,
    bool? IsActive = null,
    bool? SupportsCod = null,
    bool? SupportsInsurance = null,
    string? ApiBaseUrl = null,
    string? TrackingUrlTemplate = null,
    int? MinWeightGrams = null,
    int? MaxWeightGrams = null,
    decimal? MinCodAmount = null,
    decimal? MaxCodAmount = null) : IAuditableCommand<ShippingProviderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ProviderId;
    public string? GetTargetDisplayName() => DisplayName;
    public string? GetActionDescription() => $"Updated shipping provider '{DisplayName}'";
}

/// <summary>
/// Request DTO for updating a shipping provider via API.
/// </summary>
public sealed record UpdateShippingProviderRequest(
    string? DisplayName = null,
    GatewayEnvironment? Environment = null,
    Dictionary<string, string>? Credentials = null,
    List<ShippingServiceType>? SupportedServices = null,
    int? SortOrder = null,
    bool? IsActive = null,
    bool? SupportsCod = null,
    bool? SupportsInsurance = null,
    string? ApiBaseUrl = null,
    string? TrackingUrlTemplate = null,
    int? MinWeightGrams = null,
    int? MaxWeightGrams = null,
    decimal? MinCodAmount = null,
    decimal? MaxCodAmount = null);
