namespace NOIR.Domain.Entities.Hr;

/// <summary>
/// Employee in the organization. Links to ApplicationUser for portal access.
/// </summary>
public class Employee : TenantAggregateRoot<Guid>
{
    public string EmployeeCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? AvatarUrl { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string? Position { get; private set; }
    public Guid? ManagerId { get; private set; }
    public string? UserId { get; private set; }
    public DateTimeOffset JoinDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual Department? Department { get; private set; }
    public virtual Employee? Manager { get; private set; }
    public virtual ICollection<Employee> DirectReports { get; private set; } = new List<Employee>();
    public virtual ICollection<EmployeeTagAssignment> TagAssignments { get; private set; } = new List<EmployeeTagAssignment>();

    // Computed
    public string FullName => $"{FirstName} {LastName}";

    // Private constructor for EF Core
    private Employee() : base() { }

    private Employee(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new employee.
    /// </summary>
    public static Employee Create(
        string employeeCode,
        string firstName,
        string lastName,
        string email,
        Guid departmentId,
        DateTimeOffset joinDate,
        EmploymentType employmentType,
        string? tenantId,
        string? phone = null,
        string? position = null,
        Guid? managerId = null,
        string? userId = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var employee = new Employee(Guid.NewGuid(), tenantId)
        {
            EmployeeCode = employeeCode,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            DepartmentId = departmentId,
            JoinDate = joinDate,
            EmploymentType = employmentType,
            Status = EmployeeStatus.Active,
            Phone = phone?.Trim(),
            Position = position?.Trim(),
            ManagerId = managerId,
            UserId = userId,
            Notes = notes?.Trim()
        };

        employee.AddDomainEvent(new Events.Hr.EmployeeCreatedEvent(employee.Id));
        return employee;
    }

    /// <summary>
    /// Updates basic employee information.
    /// </summary>
    public void UpdateBasicInfo(
        string firstName,
        string lastName,
        string email,
        string? phone,
        string? avatarUrl,
        string? position,
        EmploymentType employmentType,
        string? notes)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        AvatarUrl = avatarUrl?.Trim();
        Position = position?.Trim();
        EmploymentType = employmentType;
        Notes = notes?.Trim();
        AddDomainEvent(new Events.Hr.EmployeeUpdatedEvent(Id));
    }

    /// <summary>
    /// Updates the employee's department assignment.
    /// </summary>
    public void UpdateDepartment(Guid departmentId)
    {
        if (DepartmentId != departmentId)
        {
            var oldDepartmentId = DepartmentId;
            DepartmentId = departmentId;
            AddDomainEvent(new Events.Hr.EmployeeDepartmentChangedEvent(Id, oldDepartmentId, departmentId));
        }
    }

    /// <summary>
    /// Updates the employee's manager.
    /// </summary>
    public void UpdateManager(Guid? managerId)
    {
        ManagerId = managerId;
        AddDomainEvent(new Events.Hr.EmployeeUpdatedEvent(Id));
    }

    /// <summary>
    /// Links this employee to a user account.
    /// </summary>
    public void LinkToUser(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        AddDomainEvent(new Events.Hr.EmployeeUpdatedEvent(Id));
    }

    /// <summary>
    /// Unlinks the employee from their user account.
    /// </summary>
    public void UnlinkUser()
    {
        UserId = null;
        AddDomainEvent(new Events.Hr.EmployeeUpdatedEvent(Id));
    }

    /// <summary>
    /// Deactivates the employee (resignation or termination).
    /// </summary>
    public void Deactivate(EmployeeStatus status)
    {
        if (status != EmployeeStatus.Resigned && status != EmployeeStatus.Terminated)
        {
            throw new InvalidOperationException("Deactivation status must be Resigned or Terminated.");
        }

        Status = status;
        EndDate = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.Hr.EmployeeDeactivatedEvent(Id, status));
    }

    /// <summary>
    /// Reactivates a previously deactivated employee.
    /// </summary>
    public void Reactivate()
    {
        Status = EmployeeStatus.Active;
        EndDate = null;
        AddDomainEvent(new Events.Hr.EmployeeUpdatedEvent(Id));
    }
}
