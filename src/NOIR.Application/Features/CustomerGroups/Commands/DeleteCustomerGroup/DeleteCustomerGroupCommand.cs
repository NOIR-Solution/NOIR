namespace NOIR.Application.Features.CustomerGroups.Commands.DeleteCustomerGroup;

/// <summary>
/// Command to delete a customer group (soft delete).
/// </summary>
public sealed record DeleteCustomerGroupCommand(
    Guid Id,
    string? GroupName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => GroupName ?? Id.ToString();
    public string? GetActionDescription() => $"Deleted customer group '{GetTargetDisplayName()}'";
}
