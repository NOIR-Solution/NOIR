namespace NOIR.Application.Features.Customers.Commands.DeleteCustomerAddress;

/// <summary>
/// Command to delete a customer address.
/// </summary>
public sealed record DeleteCustomerAddressCommand(
    Guid CustomerId,
    Guid AddressId) : IAuditableCommand<CustomerAddressDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => AddressId;
    public string? GetTargetDisplayName() => "CustomerAddress";
    public string? GetActionDescription() => "Deleted customer address";
}
