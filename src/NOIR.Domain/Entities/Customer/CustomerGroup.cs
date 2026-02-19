namespace NOIR.Domain.Entities.Customer;

/// <summary>
/// Represents a customer group for segmentation.
/// Used to categorize and group customers for targeted marketing and management.
/// </summary>
public class CustomerGroup : TenantAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Cached member count for fast listing queries.
    /// </summary>
    public int MemberCount { get; private set; }

    // Navigation
    private readonly List<CustomerGroupMembership> _memberships = new();
    public IReadOnlyCollection<CustomerGroupMembership> Memberships => _memberships.AsReadOnly();

    // Private constructors for EF Core
    private CustomerGroup() : base() { }
    private CustomerGroup(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new customer group.
    /// </summary>
    public static CustomerGroup Create(string name, string? description, string? tenantId = null)
    {
        return new CustomerGroup(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Description = description,
            Slug = GenerateSlug(name),
            IsActive = true
        };
    }

    /// <summary>
    /// Updates group details.
    /// </summary>
    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        Slug = GenerateSlug(name);
    }

    /// <summary>
    /// Sets the active status.
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// Activates the group.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the group.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Increments the cached member count.
    /// </summary>
    public void IncrementMemberCount(int count = 1)
    {
        MemberCount += count;
    }

    /// <summary>
    /// Decrements the cached member count.
    /// </summary>
    public void DecrementMemberCount(int count = 1)
    {
        MemberCount = Math.Max(0, MemberCount - count);
    }

    /// <summary>
    /// Updates the cached member count.
    /// </summary>
    public void UpdateMemberCount(int count)
    {
        MemberCount = count;
    }

    /// <summary>
    /// Marks the group for deletion.
    /// </summary>
    public void MarkAsDeleted()
    {
        // Soft delete handled by interceptor
    }

    /// <summary>
    /// Generates a URL-friendly slug from a name.
    /// </summary>
    private static string GenerateSlug(string name)
    {
        var chars = name.ToLowerInvariant().Trim()
            .Select(c => char.IsLetterOrDigit(c) ? c : c is ' ' or '-' ? '-' : '\0')
            .Where(c => c != '\0');
        var slug = new string(chars.ToArray());
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
