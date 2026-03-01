namespace NOIR.Domain.Entities.Hr;

/// <summary>
/// Junction entity linking Employee to EmployeeTag. Tenant-scoped.
/// Uses soft delete so we can track assignment history.
/// </summary>
public class EmployeeTagAssignment : TenantEntity<Guid>
{
    public Guid EmployeeId { get; private set; }
    public Guid EmployeeTagId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    // Navigation
    public virtual Employee? Employee { get; private set; }
    public virtual EmployeeTag? EmployeeTag { get; private set; }

    private EmployeeTagAssignment() : base() { }

    public static EmployeeTagAssignment Create(Guid employeeId, Guid employeeTagId, string? tenantId)
    {
        return new EmployeeTagAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeTagId = employeeTagId,
            AssignedAt = DateTimeOffset.UtcNow
        };
    }
}
