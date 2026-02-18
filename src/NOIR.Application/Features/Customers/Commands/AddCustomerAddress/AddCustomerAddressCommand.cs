namespace NOIR.Application.Features.Customers.Commands.AddCustomerAddress;

/// <summary>
/// Command to add an address to a customer.
/// </summary>
public sealed record AddCustomerAddressCommand(
    Guid CustomerId,
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

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => CustomerId;
    public string? GetTargetDisplayName() => FullName;
    public string? GetActionDescription() => $"Added address for '{FullName}'";
}
