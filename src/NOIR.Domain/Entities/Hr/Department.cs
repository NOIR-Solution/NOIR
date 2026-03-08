namespace NOIR.Domain.Entities.Hr;

/// <summary>
/// Department in the organizational hierarchy.
/// </summary>
public class Department : TenantAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ManagerId { get; private set; }
    public Guid? ParentDepartmentId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public virtual Employee? Manager { get; private set; }
    public virtual Department? ParentDepartment { get; private set; }
    public virtual ICollection<Department> SubDepartments { get; private set; } = new List<Department>();
    public virtual ICollection<Employee> Employees { get; private set; } = new List<Employee>();

    // Private constructor for EF Core
    private Department() : base() { }

    private Department(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new department.
    /// </summary>
    public static Department Create(
        string name,
        string code,
        string? tenantId,
        string? description = null,
        Guid? parentDepartmentId = null,
        Guid? managerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var department = new Department(Guid.NewGuid(), tenantId)
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Description = description?.Trim(),
            ParentDepartmentId = parentDepartmentId,
            ManagerId = managerId,
            SortOrder = 0,
            IsActive = true
        };

        department.AddDomainEvent(new Events.Hr.DepartmentCreatedEvent(department.Id));
        return department;
    }

    /// <summary>
    /// Updates department details.
    /// </summary>
    public void Update(
        string name,
        string code,
        string? description,
        Guid? managerId,
        Guid? parentDepartmentId)
    {
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = description?.Trim();
        ManagerId = managerId;
        ParentDepartmentId = parentDepartmentId;
        AddDomainEvent(new Events.Hr.DepartmentUpdatedEvent(Id));
    }

    /// <summary>
    /// Sets the display sort order.
    /// </summary>
    public void SetSortOrder(int order)
    {
        SortOrder = order;
    }

    /// <summary>
    /// Moves this department to a different parent (or root if null).
    /// Used by drag-and-drop reordering. No-op if parent hasn't changed.
    /// </summary>
    public void SetParentDepartment(Guid? parentDepartmentId)
    {
        if (ParentDepartmentId == parentDepartmentId) return;
        ParentDepartmentId = parentDepartmentId;
        AddDomainEvent(new Events.Hr.DepartmentUpdatedEvent(Id));
    }

    /// <summary>
    /// Deactivates the department.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        AddDomainEvent(new Events.Hr.DepartmentUpdatedEvent(Id));
    }

    /// <summary>
    /// Activates the department.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        AddDomainEvent(new Events.Hr.DepartmentUpdatedEvent(Id));
    }
}
