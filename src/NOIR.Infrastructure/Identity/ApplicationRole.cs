namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Application role entity extending ASP.NET Core Identity.
/// Supports role hierarchy, tenant scoping, and audit tracking.
/// </summary>
public class ApplicationRole : IdentityRole, IAuditableEntity
{
    /// <summary>
    /// Parent role ID for inheritance hierarchy.
    /// Child roles inherit all permissions from parent roles.
    /// </summary>
    public string? ParentRoleId { get; set; }

    /// <summary>
    /// Navigation property to parent role.
    /// </summary>
    public virtual ApplicationRole? ParentRole { get; set; }

    /// <summary>
    /// Navigation property to child roles.
    /// </summary>
    public virtual ICollection<ApplicationRole> ChildRoles { get; set; } = [];

    /// <summary>
    /// Tenant ID for tenant-scoped roles.
    /// Null means this is a system-wide role.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Description of the role's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system role that cannot be deleted.
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Whether this is a platform-level role for cross-tenant administration.
    /// Platform roles are completely hidden from tenant-level role management.
    /// </summary>
    public bool IsPlatformRole { get; set; }

    /// <summary>
    /// Order for display purposes in UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Icon name for UI display (e.g., "shield", "users").
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Color for UI display (e.g., "blue", "red", "green").
    /// </summary>
    public string? Color { get; set; }

    #region IAuditableEntity Implementation

    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    #endregion

    /// <summary>
    /// Creates a new role with the specified name.
    /// </summary>
    public static ApplicationRole Create(
        string name,
        string? description = null,
        string? parentRoleId = null,
        Guid? tenantId = null,
        bool isSystemRole = false,
        bool isPlatformRole = false,
        int sortOrder = 0,
        string? iconName = null,
        string? color = null)
    {
        return new ApplicationRole
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = description,
            ParentRoleId = parentRoleId,
            TenantId = tenantId,
            IsSystemRole = isSystemRole,
            IsPlatformRole = isPlatformRole,
            SortOrder = sortOrder,
            IconName = iconName,
            Color = color,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates the role details.
    /// </summary>
    public void Update(
        string name,
        string? description,
        string? parentRoleId,
        int sortOrder,
        string? iconName,
        string? color)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        ParentRoleId = parentRoleId;
        SortOrder = sortOrder;
        IconName = iconName;
        Color = color;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
