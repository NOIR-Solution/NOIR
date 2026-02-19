namespace NOIR.Domain.Entities.Customer;

/// <summary>
/// Junction entity for Customer-CustomerGroup many-to-many relationship.
/// </summary>
public class CustomerGroupMembership : TenantEntity<Guid>
{
    public Guid CustomerGroupId { get; private set; }
    public CustomerGroup CustomerGroup { get; private set; } = null!;

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;

    // Private constructors for EF Core
    private CustomerGroupMembership() : base() { }
    private CustomerGroupMembership(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new membership.
    /// </summary>
    public static CustomerGroupMembership Create(Guid customerGroupId, Guid customerId, string? tenantId = null)
    {
        return new CustomerGroupMembership(Guid.NewGuid(), tenantId)
        {
            CustomerGroupId = customerGroupId,
            CustomerId = customerId
        };
    }
}
