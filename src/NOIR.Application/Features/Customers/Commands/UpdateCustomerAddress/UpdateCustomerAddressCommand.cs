namespace NOIR.Application.Features.Customers.Commands.UpdateCustomerAddress;

/// <summary>
/// Command to update a customer address.
/// </summary>
public sealed record UpdateCustomerAddressCommand(
    Guid CustomerId,
    Guid AddressId,
    AddressType AddressType,
    string FullName,
    string Phone,
    string AddressLine1,
    string Province,
    string? AddressLine2 = null,
    string? Ward = null,
    string? District = null,
    string? PostalCode = null,
    bool IsDefault = false) : IAuditableCommand<CustomerAddressDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => AddressId;
    public string? GetTargetDisplayName() => FullName;
    public string? GetActionDescription() => $"Updated address for '{FullName}'";
}
