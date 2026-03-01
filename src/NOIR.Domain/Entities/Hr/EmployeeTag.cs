namespace NOIR.Domain.Entities.Hr;

/// <summary>
/// A tag definition that can be assigned to employees. Grouped by category.
/// </summary>
public class EmployeeTag : TenantAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public EmployeeTagCategory Category { get; private set; }
    public string Color { get; private set; } = "#6366f1";
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public virtual ICollection<EmployeeTagAssignment> TagAssignments { get; private set; } = new List<EmployeeTagAssignment>();

    // Computed
    public int EmployeeCount => TagAssignments?.Count(a => !a.IsDeleted) ?? 0;

    private EmployeeTag() : base() { }
    private EmployeeTag(Guid id, string? tenantId) : base(id, tenantId) { }

    public static EmployeeTag Create(
        string name, EmployeeTagCategory category, string? tenantId,
        string? color = null, string? description = null, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var tag = new EmployeeTag(Guid.NewGuid(), tenantId)
        {
            Name = name.Trim(),
            Category = category,
            Color = color?.Trim() ?? "#6366f1",
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };
        return tag;
    }

    public void Update(string name, EmployeeTagCategory category, string? color, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Category = category;
        Color = color?.Trim() ?? Color;
        Description = description?.Trim();
        SortOrder = sortOrder;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
