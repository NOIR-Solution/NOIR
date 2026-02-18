namespace NOIR.Application.Features.Customers.Commands.DeleteCustomer;

/// <summary>
/// Command to soft-delete a customer.
/// </summary>
public sealed record DeleteCustomerCommand(Guid Id) : IAuditableCommand<CustomerDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Customer";
    public string? GetActionDescription() => "Deleted customer";
}
