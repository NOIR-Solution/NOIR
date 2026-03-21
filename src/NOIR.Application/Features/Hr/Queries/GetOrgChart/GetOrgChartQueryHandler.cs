namespace NOIR.Application.Features.Hr.Queries.GetOrgChart;

public class GetOrgChartQueryHandler
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<Employee, Guid> _employeeRepository;

    public GetOrgChartQueryHandler(
        IRepository<Department, Guid> departmentRepository,
        IRepository<Employee, Guid> employeeRepository)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<List<Features.Hr.DTOs.OrgChartNodeDto>>> Handle(
        GetOrgChartQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Load all active departments
        var allDepartments = (await _departmentRepository.ListAsync(
                new Specifications.AllDepartmentsSpec(), cancellationToken))
            .Where(d => d.IsActive)
            .ToList();

        // 2. Determine target department IDs (subtree if filtered)
        var targetDeptIds = new HashSet<Guid>();
        if (query.DepartmentId.HasValue)
        {
            CollectSubtree(query.DepartmentId.Value, allDepartments, targetDeptIds);
        }
        else
        {
            foreach (var dept in allDepartments)
                targetDeptIds.Add(dept.Id);
        }

        // 3. Load employees for target departments
        var employees = await _employeeRepository.ListAsync(
            new Specifications.OrgChartEmployeesSpec(targetDeptIds), cancellationToken);

        var employeeIdSet = employees.Select(e => e.Id).ToHashSet();

        // 4. Build flat node list
        var nodes = new List<Features.Hr.DTOs.OrgChartNodeDto>();

        // Department nodes
        foreach (var dept in allDepartments.Where(d => targetDeptIds.Contains(d.Id)))
        {
            // If filtered and parent is outside the target set, make it a root
            var parentId = dept.ParentDepartmentId;
            if (parentId.HasValue && !targetDeptIds.Contains(parentId.Value))
                parentId = null;

            var empCount = employees.Count(e => e.DepartmentId == dept.Id);
            var managerSubtitle = dept.Manager != null
                ? $"Manager: {dept.Manager.FirstName} {dept.Manager.LastName}"
                : null;

            nodes.Add(new Features.Hr.DTOs.OrgChartNodeDto(
                dept.Id,
                Features.Hr.DTOs.OrgChartNodeType.Department,
                dept.Name,
                managerSubtitle,
                null,
                empCount,
                null,
                parentId,
                null));
        }

        // Employee nodes
        foreach (var emp in employees)
        {
            // Only include managerId edge if the manager is also in the result set
            var managerId = emp.ManagerId.HasValue && employeeIdSet.Contains(emp.ManagerId.Value)
                ? emp.ManagerId
                : null;

            nodes.Add(new Features.Hr.DTOs.OrgChartNodeDto(
                emp.Id,
                Features.Hr.DTOs.OrgChartNodeType.Employee,
                emp.FullName,
                emp.Position,
                emp.AvatarUrl,
                null,
                emp.Status,
                emp.DepartmentId,
                managerId));
        }

        return Result.Success(nodes);
    }

    /// <summary>
    /// Recursively collects department IDs for the given root and all its descendants.
    /// </summary>
    private static void CollectSubtree(
        Guid rootId,
        List<Department> allDepartments,
        HashSet<Guid> result)
    {
        if (!result.Add(rootId)) return;

        foreach (var child in allDepartments.Where(d => d.ParentDepartmentId == rootId))
        {
            CollectSubtree(child.Id, allDepartments, result);
        }
    }
}
