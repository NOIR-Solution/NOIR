namespace NOIR.Domain.Entities;

/// <summary>
/// Pre-defined permission sets for quick role creation.
/// Templates can be system-wide or tenant-specific.
/// </summary>
public class PermissionTemplate : Entity<Guid>, IAuditableEntity
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
    /// Tenant ID for tenant-specific templates.
    /// Null means this is a system template.
    /// </summary>
    public Guid? TenantId { get; private set; }

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

    #region IAuditableEntity Implementation

    public string? CreatedBy { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    #endregion

    private PermissionTemplate() { }

    /// <summary>
    /// Creates a new permission template.
    /// </summary>
    public static PermissionTemplate Create(
        string name,
        string? description = null,
        Guid? tenantId = null,
        bool isSystem = false,
        string? iconName = null,
        string? color = null,
        int sortOrder = 0)
    {
        return new PermissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            TenantId = tenantId,
            IsSystem = isSystem,
            IconName = iconName,
            Color = color,
            SortOrder = sortOrder
        };
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
public class PermissionTemplateItem : Entity<Guid>
{
    public Guid TemplateId { get; private set; }
    public Guid PermissionId { get; private set; }

    public PermissionTemplate Template { get; private set; } = default!;
    public Permission Permission { get; private set; } = default!;

    private PermissionTemplateItem() { }

    public static PermissionTemplateItem Create(Guid templateId, Guid permissionId)
    {
        return new PermissionTemplateItem
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            PermissionId = permissionId
        };
    }
}
