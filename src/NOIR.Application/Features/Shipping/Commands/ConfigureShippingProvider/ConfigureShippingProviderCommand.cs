namespace NOIR.Application.Features.Shipping.Commands.ConfigureShippingProvider;

/// <summary>
/// Command to configure a shipping provider.
/// </summary>
public sealed record ConfigureShippingProviderCommand(
    ShippingProviderCode ProviderCode,
    string DisplayName,
    GatewayEnvironment Environment,
    Dictionary<string, string> Credentials,
    List<ShippingServiceType> SupportedServices,
    int SortOrder,
    bool IsActive,
    bool SupportsCod = true,
    bool SupportsInsurance = false,
    string? ApiBaseUrl = null,
    string? TrackingUrlTemplate = null) : IAuditableCommand<ShippingProviderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => DisplayName;
    public string? GetActionDescription() => $"Configured shipping provider '{DisplayName}' ({ProviderCode})";
}

/// <summary>
/// Request DTO for configuring a shipping provider via API.
/// </summary>
public sealed record ConfigureShippingProviderRequest(
    ShippingProviderCode ProviderCode,
    string DisplayName,
    GatewayEnvironment Environment,
    Dictionary<string, string> Credentials,
    List<ShippingServiceType> SupportedServices,
    int SortOrder,
    bool IsActive,
    bool SupportsCod = true,
    bool SupportsInsurance = false,
    string? ApiBaseUrl = null,
    string? TrackingUrlTemplate = null);
