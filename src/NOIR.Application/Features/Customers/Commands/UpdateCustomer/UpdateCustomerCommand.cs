namespace NOIR.Application.Features.Customers.Commands.UpdateCustomer;

/// <summary>
/// Command to update an existing customer's profile.
/// </summary>
public sealed record UpdateCustomerCommand(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone = null,
    string? Tags = null,
    string? Notes = null) : IAuditableCommand<CustomerDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => $"{FirstName} {LastName}";
    public string? GetActionDescription() => $"Updated customer '{FirstName} {LastName}'";
}
