namespace NOIR.Domain.Entities;

/// <summary>
/// Permission entity for flexible, database-backed authorization.
/// Format: "resource:action:scope" (e.g., "orders:read:own", "users:delete:all")
/// </summary>
public class Permission : Entity<Guid>
{
    /// <summary>
    /// Resource this permission applies to (e.g., "orders", "users", "reports").
    /// </summary>
    public string Resource { get; private set; } = default!;

    /// <summary>
    /// Action allowed on the resource (e.g., "create", "read", "update", "delete").
    /// </summary>
    public string Action { get; private set; } = default!;

    /// <summary>
    /// Scope of the permission (e.g., "own", "team", "all").
    /// Null means no scope restriction.
    /// </summary>
    public string? Scope { get; private set; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>
    /// Description of what this permission allows.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Category for grouping in UI (e.g., "User Management", "Orders").
    /// </summary>
    public string? Category { get; private set; }

    /// <summary>
    /// Whether this is a system permission (cannot be deleted).
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Order for display purposes.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// The full permission name (resource:action:scope).
    /// </summary>
    public string Name => string.IsNullOrEmpty(Scope)
        ? $"{Resource}:{Action}"
        : $"{Resource}:{Action}:{Scope}";

    /// <summary>
    /// Roles that have this permission.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    // Private constructor for EF Core
    private Permission() { }

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    public static Permission Create(
        string resource,
        string action,
        string displayName,
        string? scope = null,
        string? description = null,
        string? category = null,
        bool isSystem = false,
        int sortOrder = 0)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Resource = resource.ToLowerInvariant(),
            Action = action.ToLowerInvariant(),
            Scope = scope?.ToLowerInvariant(),
            DisplayName = displayName,
            Description = description,
            Category = category,
            IsSystem = isSystem,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Updates the permission details.
    /// </summary>
    public void Update(string displayName, string? description, string? category, int sortOrder)
    {
        DisplayName = displayName;
        Description = description;
        Category = category;
        SortOrder = sortOrder;
    }
}

/// <summary>
/// Join entity for Role-Permission many-to-many relationship.
/// Extends Entity for consistent audit tracking (CreatedAt, ModifiedAt).
/// Uses composite key of (RoleId, PermissionId) stored in Id as tuple.
/// </summary>
public class RolePermission : Entity<Guid>, IAuditableEntity
{
    public string RoleId { get; private set; } = default!;
    public Guid PermissionId { get; private set; }

    public Permission Permission { get; private set; } = default!;

    #region IAuditableEntity Implementation

    public string? CreatedBy { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    #endregion

    // Private constructor for EF Core
    private RolePermission() : base() { }

    public static RolePermission Create(string roleId, Guid permissionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        if (permissionId == Guid.Empty)
            throw new ArgumentException("PermissionId cannot be empty.", nameof(permissionId));

        return new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
}
