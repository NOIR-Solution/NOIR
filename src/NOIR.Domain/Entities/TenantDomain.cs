namespace NOIR.Domain.Entities;

/// <summary>
/// Represents a domain mapping for a tenant.
/// Enables tenants to use custom domains or subdomains.
/// Platform-level entity - NOT scoped to any tenant (does not implement ITenantEntity).
/// </summary>
public class TenantDomain : Entity<Guid>, IAuditableEntity
{
    /// <summary>
    /// The tenant this domain belongs to (FK to Tenants).
    /// </summary>
    public string TenantId { get; private set; } = default!;

    /// <summary>
    /// The domain name (e.g., "crm.acme.com" or "acme.noir.app").
    /// </summary>
    public string Domain { get; private set; } = default!;

    /// <summary>
    /// Whether this is the primary domain for the tenant.
    /// Used for generating URLs and redirects.
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Whether this is a custom domain (true) or a subdomain of the platform (false).
    /// Custom domains require DNS verification.
    /// </summary>
    public bool IsCustomDomain { get; private set; }

    /// <summary>
    /// Whether the domain has been verified (DNS records confirmed).
    /// Only applicable for custom domains.
    /// </summary>
    public bool IsVerified { get; private set; }

    /// <summary>
    /// When the domain was verified.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <summary>
    /// DNS verification token for custom domain setup.
    /// </summary>
    public string? VerificationToken { get; private set; }

    #region IAuditableEntity Implementation
    // CreatedAt and ModifiedAt are inherited from Entity<Guid>

    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    #endregion

    // Private constructor for EF Core
    private TenantDomain() : base() { }

    /// <summary>
    /// Creates a subdomain for a tenant (e.g., "acme.noir.app").
    /// Subdomains are automatically verified.
    /// </summary>
    public static TenantDomain CreateSubdomain(
        string tenantId,
        string subdomain,
        string platformDomain,
        bool isPrimary = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain, nameof(subdomain));
        ArgumentException.ThrowIfNullOrWhiteSpace(platformDomain, nameof(platformDomain));

        var domain = $"{subdomain.ToLowerInvariant().Trim()}.{platformDomain.ToLowerInvariant().Trim()}";

        return new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Domain = domain,
            IsPrimary = isPrimary,
            IsCustomDomain = false,
            IsVerified = true, // Subdomains are auto-verified
            VerifiedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a custom domain for a tenant (e.g., "crm.acme.com").
    /// Custom domains require DNS verification.
    /// </summary>
    public static TenantDomain CreateCustomDomain(
        string tenantId,
        string domain,
        bool isPrimary = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(domain, nameof(domain));

        return new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Domain = domain.ToLowerInvariant().Trim(),
            IsPrimary = isPrimary,
            IsCustomDomain = true,
            IsVerified = false,
            VerificationToken = GenerateVerificationToken()
        };
    }

    /// <summary>
    /// Marks the domain as verified.
    /// </summary>
    public void MarkAsVerified()
    {
        IsVerified = true;
        VerifiedAt = DateTimeOffset.UtcNow;
        VerificationToken = null; // Clear token after verification
    }

    /// <summary>
    /// Sets this domain as the primary domain for the tenant.
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Removes primary status from this domain.
    /// </summary>
    public void ClearPrimary()
    {
        IsPrimary = false;
    }

    /// <summary>
    /// Regenerates the verification token for retry.
    /// </summary>
    public void RegenerateVerificationToken()
    {
        if (IsCustomDomain && !IsVerified)
        {
            VerificationToken = GenerateVerificationToken();
        }
    }

    private static string GenerateVerificationToken()
    {
        return $"noir-verify-{Guid.NewGuid():N}";
    }
}
