namespace NOIR.Application.Features.Payments.Commands.UpdateGateway;

/// <summary>
/// Command to update a payment gateway configuration.
/// </summary>
public sealed record UpdateGatewayCommand(
    Guid GatewayId,
    string? DisplayName,
    GatewayEnvironment? Environment,
    Dictionary<string, string>? Credentials,
    List<PaymentMethod>? SupportedMethods,
    int? SortOrder,
    bool? IsActive) : IAuditableCommand<PaymentGatewayDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => DisplayName ?? $"Gateway {GatewayId}";
    public string? GetActionDescription() => $"Updated payment gateway configuration";
}
