namespace NOIR.Application.Features.Payments.Commands.ConfigureGateway;

/// <summary>
/// Command to configure a payment gateway.
/// </summary>
public sealed record ConfigureGatewayCommand(
    string Provider,
    string DisplayName,
    GatewayEnvironment Environment,
    Dictionary<string, string> Credentials,
    List<PaymentMethod> SupportedMethods,
    int SortOrder,
    bool IsActive) : IAuditableCommand<PaymentGatewayDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => DisplayName;
    public string? GetActionDescription() => $"Configured payment gateway '{DisplayName}' ({Provider})";
}
