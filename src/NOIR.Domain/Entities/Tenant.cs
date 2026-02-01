using System.Diagnostics.CodeAnalysis;

namespace NOIR.Domain.Entities;

/// <summary>
/// Represents a tenant in the multi-tenant system.
/// Platform-level entity - NOT scoped to any tenant (does not implement ITenantEntity).
/// Inherits from TenantInfo class for Finbuckle EFCoreStore compatibility.
/// </summary>
/// <remarks>
/// As of Finbuckle.MultiTenant 10.0.2, TenantInfo has required init-only properties.
/// This class uses factory methods for creation and updates to work within those constraints.
/// For updates, use CreateUpdated() to generate a new instance with modified values.
/// </remarks>
public class Tenant : TenantInfo, IAuditableEntity
{
    /// <summary>
    /// Private constructor for EF Core materialization.
    /// EF Core can set required/init-only properties through this constructor.
    /// </summary>
    [SetsRequiredMembers]
    private Tenant()
    {
        Id = string.Empty;
        Identifier = string.Empty;
    }

    /// <summary>
    /// Primary constructor for creating Tenant instances.
    /// </summary>
    [SetsRequiredMembers]
    public Tenant(string id, string identifier, string? name = null)
    {
        Id = id;
        Identifier = identifier;
        Name = name;
    }

    #region Status

    /// <summary>
    /// Whether the tenant is active. Inactive tenants cannot log in.
    /// Use CreateActivated/CreateDeactivated/CreateUpdated methods to change status.
    /// </summary>
    public bool IsActive { get; init; } = true;

    #endregion

    #region Tenant Details

    /// <summary>
    /// Custom domain for this tenant (e.g., "acme.platform.com" or "app.acme.com").
    /// Used for automatic tenant resolution from request host.
    /// If null, tenant can only be accessed via identifier or explicit selection.
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// Description of the tenant for administrative purposes.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Internal notes about this tenant (not visible to tenant users).
    /// </summary>
    public string? Note { get; init; }

    #endregion

    #region Timestamps (from Entity pattern)

    /// <summary>
    /// Timestamp when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    #endregion

    #region IAuditableEntity Implementation

    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    #endregion


    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="identifier">Unique identifier/slug (will be lowercased).</param>
    /// <param name="name">Display name.</param>
    /// <param name="domain">Optional custom domain for tenant resolution.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="note">Optional internal notes.</param>
    /// <param name="isActive">Whether the tenant is active (default: true).</param>
    public static Tenant Create(
        string identifier,
        string name,
        string? domain = null,
        string? description = null,
        string? note = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var id = Guid.NewGuid().ToString();
        return new Tenant(id, identifier.ToLowerInvariant().Trim(), name.Trim())
        {
            Domain = domain?.ToLowerInvariant().Trim(),
            Description = description?.Trim(),
            Note = note?.Trim(),
            IsActive = isActive
        };
    }

    /// <summary>
    /// Gets the Id as a Guid for convenience.
    /// </summary>
    public Guid GetGuidId() => Guid.Parse(Id);

    /// <summary>
    /// Creates a new Tenant instance with updated details.
    /// TenantInfo base class has init-only properties, so updates require creating a new instance.
    /// Finbuckle's EFCoreStore handles replacing the tracked entity appropriately.
    /// </summary>
    public Tenant CreateUpdated(
        string identifier,
        string name,
        string? domain,
        string? description,
        string? note,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return new Tenant(Id, identifier.ToLowerInvariant().Trim(), name.Trim())
        {
            Domain = domain?.ToLowerInvariant().Trim(),
            Description = description?.Trim(),
            Note = note?.Trim(),
            IsActive = isActive,
            ModifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy,
            ModifiedBy = ModifiedBy,
            IsDeleted = IsDeleted,
            DeletedAt = DeletedAt,
            DeletedBy = DeletedBy
        };
    }

    /// <summary>
    /// Creates an activated copy of this tenant.
    /// </summary>
    public Tenant CreateActivated()
    {
        return new Tenant(Id, Identifier, Name)
        {
            Domain = Domain,
            Description = Description,
            Note = Note,
            IsActive = true,
            ModifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy,
            ModifiedBy = ModifiedBy,
            IsDeleted = IsDeleted,
            DeletedAt = DeletedAt,
            DeletedBy = DeletedBy
        };
    }

    /// <summary>
    /// Creates a deactivated copy of this tenant.
    /// </summary>
    public Tenant CreateDeactivated()
    {
        return new Tenant(Id, Identifier, Name)
        {
            Domain = Domain,
            Description = Description,
            Note = Note,
            IsActive = false,
            ModifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy,
            ModifiedBy = ModifiedBy,
            IsDeleted = IsDeleted,
            DeletedAt = DeletedAt,
            DeletedBy = DeletedBy
        };
    }

    /// <summary>
    /// Creates a soft-deleted copy of this tenant.
    /// </summary>
    /// <param name="deletedBy">Optional user ID who performed the deletion.</param>
    public Tenant CreateDeleted(string? deletedBy = null)
    {
        return new Tenant(Id, Identifier, Name)
        {
            Domain = Domain,
            Description = Description,
            Note = Note,
            IsActive = IsActive,
            ModifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy,
            ModifiedBy = ModifiedBy,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
            DeletedBy = deletedBy ?? DeletedBy
        };
    }

    /// <summary>
    /// Creates a restored copy of this soft-deleted tenant.
    /// Clears the soft-delete flags and activates the tenant.
    /// </summary>
    public Tenant CreateRestored()
    {
        return new Tenant(Id, Identifier, Name)
        {
            Domain = Domain,
            Description = Description,
            Note = Note,
            IsActive = true,
            ModifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy,
            ModifiedBy = ModifiedBy,
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null
        };
    }
}
