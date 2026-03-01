namespace NOIR.Domain.Entities.Crm;

/// <summary>
/// A company/organization in the CRM system.
/// </summary>
public class CrmCompany : TenantAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Domain { get; private set; }
    public string? Industry { get; private set; }
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }
    public Guid? OwnerId { get; private set; }
    public string? TaxId { get; private set; }
    public int? EmployeeCount { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual Employee? Owner { get; private set; }
    public virtual ICollection<CrmContact> Contacts { get; private set; } = new List<CrmContact>();

    // Private constructor for EF Core
    private CrmCompany() : base() { }

    private CrmCompany(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new CRM company.
    /// </summary>
    public static CrmCompany Create(
        string name,
        string? tenantId,
        string? domain = null,
        string? industry = null,
        string? address = null,
        string? phone = null,
        string? website = null,
        Guid? ownerId = null,
        string? taxId = null,
        int? employeeCount = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var company = new CrmCompany(Guid.NewGuid(), tenantId)
        {
            Name = name.Trim(),
            Domain = domain?.Trim().ToLowerInvariant(),
            Industry = industry?.Trim(),
            Address = address?.Trim(),
            Phone = phone?.Trim(),
            Website = website?.Trim(),
            OwnerId = ownerId,
            TaxId = taxId?.Trim(),
            EmployeeCount = employeeCount,
            Notes = notes?.Trim()
        };

        company.AddDomainEvent(new Events.Crm.CompanyCreatedEvent(company.Id));
        return company;
    }

    /// <summary>
    /// Updates company information.
    /// </summary>
    public void Update(
        string name,
        string? domain,
        string? industry,
        string? address,
        string? phone,
        string? website,
        Guid? ownerId,
        string? taxId,
        int? employeeCount,
        string? notes)
    {
        Name = name.Trim();
        Domain = domain?.Trim().ToLowerInvariant();
        Industry = industry?.Trim();
        Address = address?.Trim();
        Phone = phone?.Trim();
        Website = website?.Trim();
        OwnerId = ownerId;
        TaxId = taxId?.Trim();
        EmployeeCount = employeeCount;
        Notes = notes?.Trim();
    }
}
