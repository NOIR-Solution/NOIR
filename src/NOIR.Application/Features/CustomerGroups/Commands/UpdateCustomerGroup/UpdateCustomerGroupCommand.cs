namespace NOIR.Application.Features.CustomerGroups.Commands.UpdateCustomerGroup;

/// <summary>
/// Command to update an existing customer group.
/// </summary>
public sealed record UpdateCustomerGroupCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive) : IAuditableCommand<CustomerGroupDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => Id;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated customer group '{Name}'";
}
