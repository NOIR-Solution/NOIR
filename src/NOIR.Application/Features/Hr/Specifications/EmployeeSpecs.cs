namespace NOIR.Application.Features.Hr.Specifications;

/// <summary>
/// Get employee by ID with tracking for mutations.
/// </summary>
public sealed class EmployeeByIdSpec : Specification<Employee>
{
    public EmployeeByIdSpec(Guid id)
    {
        Query.Where(e => e.Id == id)
             .Include(e => e.Department!)
             .Include(e => e.Manager!)
             .AsTracking()
             .TagWith("EmployeeById");
    }
}

/// <summary>
/// Get employee by ID read-only (for queries).
/// </summary>
public sealed class EmployeeByIdReadOnlySpec : Specification<Employee>
{
    public EmployeeByIdReadOnlySpec(Guid id)
    {
        Query.Where(e => e.Id == id)
             .Include(e => e.Department!)
             .Include(e => e.Manager!)
             .Include(e => e.DirectReports)
             .TagWith("EmployeeByIdReadOnly");
    }
}

/// <summary>
/// Paginated, filterable employee list.
/// </summary>
public sealed class EmployeesFilterSpec : Specification<Employee>
{
    public EmployeesFilterSpec(
        string? search = null,
        Guid? departmentId = null,
        EmployeeStatus? status = null,
        EmploymentType? employmentType = null,
        int? skip = null,
        int? take = null,
        string? orderBy = null,
        bool isDescending = true)
    {
        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(e =>
                e.FirstName.Contains(search) ||
                e.LastName.Contains(search) ||
                e.Email.Contains(search) ||
                e.EmployeeCode.Contains(search) ||
                (e.Position != null && e.Position.Contains(search)));
        }

        if (departmentId.HasValue)
        {
            Query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (status.HasValue)
        {
            Query.Where(e => e.Status == status.Value);
        }

        if (employmentType.HasValue)
        {
            Query.Where(e => e.EmploymentType == employmentType.Value);
        }

        Query.Include(e => e.Department!)
             .Include(e => e.Manager!);

        // Sorting
        switch (orderBy?.ToLowerInvariant())
        {
            case "employeecode":
                if (isDescending) Query.OrderByDescending(e => e.EmployeeCode);
                else Query.OrderBy(e => e.EmployeeCode);
                break;
            case "name":
                if (isDescending)
                    Query.OrderByDescending(e => e.LastName).ThenByDescending(e => e.FirstName);
                else
                    Query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName);
                break;
            case "position":
                if (isDescending) Query.OrderByDescending(e => e.Position ?? string.Empty);
                else Query.OrderBy(e => e.Position ?? string.Empty);
                break;
            case "department":
                if (isDescending) Query.OrderByDescending(e => e.Department != null ? e.Department.Name : string.Empty);
                else Query.OrderBy(e => e.Department != null ? e.Department.Name : string.Empty);
                break;
            case "status":
                if (isDescending) Query.OrderByDescending(e => e.Status);
                else Query.OrderBy(e => e.Status);
                break;
            case "employmenttype":
                if (isDescending) Query.OrderByDescending(e => e.EmploymentType);
                else Query.OrderBy(e => e.EmploymentType);
                break;
            case "hiredate":
                if (isDescending) Query.OrderByDescending(e => e.JoinDate);
                else Query.OrderBy(e => e.JoinDate);
                break;
            case "email":
                if (isDescending) Query.OrderByDescending(e => e.Email);
                else Query.OrderBy(e => e.Email);
                break;
            case "createdby":
            case "creator":
                if (isDescending) Query.OrderByDescending(e => e.CreatedBy);
                else Query.OrderBy(e => e.CreatedBy);
                break;
            case "modifiedby":
            case "editor":
                if (isDescending) Query.OrderByDescending(e => e.ModifiedBy);
                else Query.OrderBy(e => e.ModifiedBy);
                break;
            case "createdat":
            default:
                Query.OrderByDescending(e => e.CreatedAt);
                break;
        }

        if (skip.HasValue) Query.Skip(skip.Value);
        if (take.HasValue) Query.Take(take.Value);

        Query.TagWith("GetEmployees");
    }
}

/// <summary>
/// Count employees matching filters (without pagination).
/// </summary>
public sealed class EmployeesCountSpec : Specification<Employee>
{
    public EmployeesCountSpec(
        string? search = null,
        Guid? departmentId = null,
        EmployeeStatus? status = null,
        EmploymentType? employmentType = null)
    {
        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(e =>
                e.FirstName.Contains(search) ||
                e.LastName.Contains(search) ||
                e.Email.Contains(search) ||
                e.EmployeeCode.Contains(search) ||
                (e.Position != null && e.Position.Contains(search)));
        }

        if (departmentId.HasValue)
        {
            Query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (status.HasValue)
        {
            Query.Where(e => e.Status == status.Value);
        }

        if (employmentType.HasValue)
        {
            Query.Where(e => e.EmploymentType == employmentType.Value);
        }

        Query.TagWith("CountEmployees");
    }
}

/// <summary>
/// Check if a department has employees (for delete validation).
/// </summary>
public sealed class EmployeesByDepartmentSpec : Specification<Employee>
{
    public EmployeesByDepartmentSpec(Guid departmentId)
    {
        Query.Where(e => e.DepartmentId == departmentId)
             .TagWith("EmployeesByDepartment");
    }
}

/// <summary>
/// Check email uniqueness per tenant.
/// </summary>
public sealed class EmployeeByEmailSpec : Specification<Employee>
{
    public EmployeeByEmailSpec(string email, string? tenantId, Guid? excludeId = null)
    {
        Query.Where(e => e.Email == email.ToLowerInvariant())
             .Where(e => tenantId == null || e.TenantId == tenantId)
             .Where(e => excludeId == null || e.Id != excludeId)
             .TagWith("EmployeeByEmail");
    }
}

/// <summary>
/// Check user link uniqueness per tenant.
/// </summary>
public sealed class EmployeeByUserIdSpec : Specification<Employee>
{
    public EmployeeByUserIdSpec(string userId, Guid? excludeId = null)
    {
        Query.Where(e => e.UserId == userId)
             .Where(e => excludeId == null || e.Id != excludeId)
             .TagWith("EmployeeByUserId");
    }
}

/// <summary>
/// Get direct reports of a manager (for deactivation cascade).
/// </summary>
public sealed class EmployeesByManagerIdSpec : Specification<Employee>
{
    public EmployeesByManagerIdSpec(Guid managerId)
    {
        Query.Where(e => e.ManagerId == managerId)
             .AsTracking()
             .TagWith("EmployeesByManagerId");
    }
}

/// <summary>
/// Get employees by IDs with tracking for bulk mutations.
/// </summary>
public sealed class EmployeesByIdsForUpdateSpec : Specification<Employee>
{
    public EmployeesByIdsForUpdateSpec(List<Guid> ids)
    {
        Query.Where(e => ids.Contains(e.Id))
             .AsTracking()
             .TagWith("EmployeesByIdsForUpdate");
    }
}

/// <summary>
/// Get employees for export with department and tag data.
/// </summary>
public sealed class EmployeesForExportSpec : Specification<Employee>
{
    public EmployeesForExportSpec(
        Guid? departmentId = null,
        EmployeeStatus? status = null,
        EmploymentType? employmentType = null)
    {
        if (departmentId.HasValue)
            Query.Where(e => e.DepartmentId == departmentId.Value);

        if (status.HasValue)
            Query.Where(e => e.Status == status.Value);

        if (employmentType.HasValue)
            Query.Where(e => e.EmploymentType == employmentType.Value);

        Query.Include(e => e.Department!)
             .Include(e => e.Manager!)
             .OrderBy(e => e.EmployeeCode)
             .TagWith("EmployeesForExport");
    }
}

/// <summary>
/// Employees for org chart — all employees in the given departments (no status filter).
/// </summary>
public sealed class OrgChartEmployeesSpec : Specification<Employee>
{
    public OrgChartEmployeesSpec(ICollection<Guid> departmentIds)
    {
        Query.Where(e => departmentIds.Contains(e.DepartmentId))
             .TagWith("OrgChartEmployees");
    }
}

/// <summary>
/// Lightweight search for autocomplete.
/// </summary>
public sealed class EmployeeSearchSpec : Specification<Employee>
{
    public EmployeeSearchSpec(string searchText, int take = 10)
    {
        Query.Where(e =>
                e.FirstName.Contains(searchText) ||
                e.LastName.Contains(searchText) ||
                e.Email.Contains(searchText) ||
                e.EmployeeCode.Contains(searchText))
             .Where(e => e.Status == EmployeeStatus.Active)
             .Include(e => e.Department!)
             .Take(take)
             .OrderBy(e => e.LastName)
             .ThenBy(e => e.FirstName)
             .TagWith("SearchEmployees");
    }
}
