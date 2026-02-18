namespace NOIR.Domain.Entities.Customer;

/// <summary>
/// Represents a customer's address (shipping, billing, or both).
/// </summary>
public class CustomerAddress : TenantEntity<Guid>
{
    private CustomerAddress() : base() { }
    private CustomerAddress(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Parent customer ID.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Type of address.
    /// </summary>
    public AddressType AddressType { get; private set; }

    /// <summary>
    /// Full name of the recipient.
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// Phone number for the address.
    /// </summary>
    public string Phone { get; private set; } = string.Empty;

    /// <summary>
    /// Primary address line.
    /// </summary>
    public string AddressLine1 { get; private set; } = string.Empty;

    /// <summary>
    /// Secondary address line (apartment, suite, etc.).
    /// </summary>
    public string? AddressLine2 { get; private set; }

    /// <summary>
    /// Ward/commune.
    /// </summary>
    public string? Ward { get; private set; }

    /// <summary>
    /// District.
    /// </summary>
    public string? District { get; private set; }

    /// <summary>
    /// Province/city.
    /// </summary>
    public string Province { get; private set; } = string.Empty;

    /// <summary>
    /// Postal code.
    /// </summary>
    public string? PostalCode { get; private set; }

    /// <summary>
    /// Whether this is the default address for its type.
    /// </summary>
    public bool IsDefault { get; private set; }

    // Navigation
    public virtual Customer Customer { get; private set; } = null!;

    /// <summary>
    /// Creates a new customer address.
    /// </summary>
    public static CustomerAddress Create(
        Guid customerId,
        AddressType addressType,
        string fullName,
        string phone,
        string addressLine1,
        string province,
        string? addressLine2 = null,
        string? ward = null,
        string? district = null,
        string? postalCode = null,
        bool isDefault = false,
        string? tenantId = null)
    {
        return new CustomerAddress(Guid.NewGuid(), tenantId)
        {
            CustomerId = customerId,
            AddressType = addressType,
            FullName = fullName,
            Phone = phone,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            Ward = ward,
            District = district,
            Province = province,
            PostalCode = postalCode,
            IsDefault = isDefault
        };
    }

    /// <summary>
    /// Updates the address details.
    /// </summary>
    public void Update(
        AddressType addressType,
        string fullName,
        string phone,
        string addressLine1,
        string province,
        string? addressLine2 = null,
        string? ward = null,
        string? district = null,
        string? postalCode = null,
        bool isDefault = false)
    {
        AddressType = addressType;
        FullName = fullName;
        Phone = phone;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        Ward = ward;
        District = district;
        Province = province;
        PostalCode = postalCode;
        IsDefault = isDefault;
    }

    /// <summary>
    /// Sets this address as the default.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
    }

    /// <summary>
    /// Removes the default status.
    /// </summary>
    public void RemoveDefault()
    {
        IsDefault = false;
    }
}
