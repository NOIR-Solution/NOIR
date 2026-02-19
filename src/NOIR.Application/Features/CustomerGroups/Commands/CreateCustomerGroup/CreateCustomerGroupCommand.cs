namespace NOIR.Application.Features.CustomerGroups.Commands.CreateCustomerGroup;

/// <summary>
/// Command to create a new customer group.
/// </summary>
public sealed record CreateCustomerGroupCommand(
    string Name,
    string? Description) : IAuditableCommand<CustomerGroupDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => null;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created customer group '{Name}'";
}
