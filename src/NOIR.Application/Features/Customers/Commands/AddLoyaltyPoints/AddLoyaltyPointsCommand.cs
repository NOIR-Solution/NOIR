namespace NOIR.Application.Features.Customers.Commands.AddLoyaltyPoints;

/// <summary>
/// Command to add loyalty points to a customer.
/// </summary>
public sealed record AddLoyaltyPointsCommand(
    Guid CustomerId,
    int Points,
    string? Reason = null) : IAuditableCommand<CustomerDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => CustomerId;
    public string? GetTargetDisplayName() => "Customer";
    public string? GetActionDescription() => $"Added {Points} loyalty points";
}
