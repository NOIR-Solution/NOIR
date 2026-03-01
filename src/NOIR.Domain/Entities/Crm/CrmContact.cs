namespace NOIR.Domain.Entities.Crm;

/// <summary>
/// A contact in the CRM system. May be linked to an existing Customer.
/// </summary>
public class CrmContact : TenantAggregateRoot<Guid>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? JobTitle { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? OwnerId { get; private set; }
    public ContactSource Source { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual CrmCompany? Company { get; private set; }
    public virtual Employee? Owner { get; private set; }
    public virtual Customer.Customer? Customer { get; private set; }
    public virtual ICollection<Lead> Leads { get; private set; } = new List<Lead>();

    // Computed
    public string FullName => $"{FirstName} {LastName}";

    // Private constructor for EF Core
    private CrmContact() : base() { }

    private CrmContact(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new CRM contact.
    /// </summary>
    public static CrmContact Create(
        string firstName,
        string lastName,
        string email,
        ContactSource source,
        string? tenantId,
        string? phone = null,
        string? jobTitle = null,
        Guid? companyId = null,
        Guid? ownerId = null,
        Guid? customerId = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var contact = new CrmContact(Guid.NewGuid(), tenantId)
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Source = source,
            Phone = phone?.Trim(),
            JobTitle = jobTitle?.Trim(),
            CompanyId = companyId,
            OwnerId = ownerId,
            CustomerId = customerId,
            Notes = notes?.Trim()
        };

        contact.AddDomainEvent(new Events.Crm.ContactCreatedEvent(contact.Id));
        return contact;
    }

    /// <summary>
    /// Updates contact information.
    /// </summary>
    public void Update(
        string firstName,
        string lastName,
        string email,
        ContactSource source,
        string? phone,
        string? jobTitle,
        Guid? companyId,
        Guid? ownerId,
        Guid? customerId,
        string? notes)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Source = source;
        Phone = phone?.Trim();
        JobTitle = jobTitle?.Trim();
        CompanyId = companyId;
        OwnerId = ownerId;
        CustomerId = customerId;
        Notes = notes?.Trim();
    }
}
