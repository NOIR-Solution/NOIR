namespace NOIR.Application.Features.Customers.Commands.CreateCustomer;

/// <summary>
/// Command to create a new customer.
/// </summary>
public sealed record CreateCustomerCommand(
    string Email,
    string FirstName,
    string LastName,
    string? Phone = null,
    string? UserId = null,
    string? Tags = null,
    string? Notes = null) : IAuditableCommand<CustomerDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? AuditUserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => AuditUserId;
    public string? GetTargetDisplayName() => $"{FirstName} {LastName}";
    public string? GetActionDescription() => $"Created customer '{FirstName} {LastName}'";
}
