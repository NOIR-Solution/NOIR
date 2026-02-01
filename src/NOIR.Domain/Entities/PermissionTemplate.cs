namespace NOIR.Domain.Entities;

/// <summary>
/// Pre-defined permission sets for quick role creation.
/// Platform templates (TenantId = null) serve as defaults for all tenants.
/// Tenant templates (TenantId = value) are tenant-specific overrides.
/// </summary>
public class PermissionTemplate : PlatformTenantEntity<Guid>
{
    /// <summary>
    /// Display name for the template.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Description of what this template is for.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this is a system template that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Icon name for UI display.
    /// </summary>
    public string? IconName { get; private set; }

    /// <summary>
    /// Color for UI display.
    /// </summary>
    public string? Color { get; private set; }

    /// <summary>
    /// Order for display purposes.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// The permission items in this template.
    /// </summary>
    public ICollection<PermissionTemplateItem> Items { get; private set; } = [];

    // Note: TenantId, IsPlatformDefault, IsTenantOverride, and IAuditableEntity properties
    // are inherited from PlatformTenantEntity<Guid> base class

    private PermissionTemplate() { }

    /// <summary>
    /// Creates a platform-level default template (TenantId = null).
    /// Platform templates are shared across all tenants.
    /// </summary>
    public static PermissionTemplate CreatePlatformDefault(
        string name,
        string? description = null,
        bool isSystem = false,
        string? iconName = null,
        string? color = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return new PermissionTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = null, // Platform default
            Name = name,
            Description = description,
            IsSystem = isSystem,
            IconName = iconName,
            Color = color,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Creates a tenant-specific template override.
    /// Used when a tenant creates a custom permission template.
    /// </summary>
    public static PermissionTemplate CreateTenantOverride(
        string tenantId,
        string name,
        string? description = null,
        bool isSystem = false,
        string? iconName = null,
        string? color = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return new PermissionTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            IsSystem = isSystem,
            IconName = iconName,
            Color = color,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Creates a new permission template (legacy method for backward compatibility).
    /// Use CreatePlatformDefault() or CreateTenantOverride() for clearer semantics.
    /// </summary>
    [Obsolete("Use CreatePlatformDefault() or CreateTenantOverride() for clearer semantics.")]
    public static PermissionTemplate Create(
        string name,
        string? description = null,
        string? tenantId = null,
        bool isSystem = false,
        string? iconName = null,
        string? color = null,
        int sortOrder = 0)
    {
        return tenantId == null
            ? CreatePlatformDefault(name, description, isSystem, iconName, color, sortOrder)
            : CreateTenantOverride(tenantId, name, description, isSystem, iconName, color, sortOrder);
    }

    /// <summary>
    /// Updates the template details.
    /// </summary>
    public void Update(
        string name,
        string? description,
        string? iconName,
        string? color,
        int sortOrder)
    {
        Name = name;
        Description = description;
        IconName = iconName;
        Color = color;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Adds a permission to the template.
    /// </summary>
    public void AddPermission(Guid permissionId)
    {
        if (Items.Any(i => i.PermissionId == permissionId))
            return;

        Items.Add(PermissionTemplateItem.Create(Id, permissionId));
    }

    /// <summary>
    /// Removes a permission from the template.
    /// </summary>
    public void RemovePermission(Guid permissionId)
    {
        var item = Items.FirstOrDefault(i => i.PermissionId == permissionId);
        if (item != null)
        {
            Items.Remove(item);
        }
    }

    /// <summary>
    /// Sets the permissions for this template.
    /// </summary>
    public void SetPermissions(IEnumerable<Guid> permissionIds)
    {
        Items.Clear();
        foreach (var permissionId in permissionIds)
        {
            Items.Add(PermissionTemplateItem.Create(Id, permissionId));
        }
    }
}

/// <summary>
/// Join entity for Permission Template - Permission many-to-many relationship.
/// </summary>
public class PermissionTemplateItem : Entity<Guid>, IAuditableEntity
{
    #region IAuditableEntity Implementation

    public string? CreatedBy { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    #endregion

    public Guid TemplateId { get; private set; }
    public Guid PermissionId { get; private set; }

    public PermissionTemplate Template { get; private set; } = default!;
    public Permission Permission { get; private set; } = default!;

    private PermissionTemplateItem() { }

    public static PermissionTemplateItem Create(Guid templateId, Guid permissionId)
    {
        if (templateId == Guid.Empty)
            throw new ArgumentException("TemplateId cannot be empty.", nameof(templateId));
        if (permissionId == Guid.Empty)
            throw new ArgumentException("PermissionId cannot be empty.", nameof(permissionId));

        return new PermissionTemplateItem
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            PermissionId = permissionId
        };
    }
}
