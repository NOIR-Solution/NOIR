namespace NOIR.Application.Features.Customers.Commands.RedeemLoyaltyPoints;

/// <summary>
/// Command to redeem loyalty points from a customer.
/// </summary>
public sealed record RedeemLoyaltyPointsCommand(
    Guid CustomerId,
    int Points,
    string? Reason = null) : IAuditableCommand<CustomerDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => CustomerId;
    public string? GetTargetDisplayName() => "Customer";
    public string? GetActionDescription() => $"Redeemed {Points} loyalty points";
}
