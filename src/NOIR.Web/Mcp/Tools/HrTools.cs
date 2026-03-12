using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Queries.GetDepartments;
using NOIR.Application.Features.Hr.Queries.GetEmployeeById;
using NOIR.Application.Features.Hr.Queries.GetEmployees;
using NOIR.Application.Features.Hr.Queries.GetHrReports;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for Human Resources management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Erp.Hr)]
public sealed class HrTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_hr_employees_list", ReadOnly = true, Idempotent = true)]
    [Description("List employees with pagination and filtering. Supports search, department, status, and employment type filters.")]
    public async Task<PagedResult<EmployeeListDto>> ListEmployees(
        [Description("Search by name, email, or employee code")] string? search = null,
        [Description("Filter by department ID (GUID)")] string? departmentId = null,
        [Description("Filter by status: Active, OnLeave, Suspended, Terminated")] string? status = null,
        [Description("Filter by type: FullTime, PartTime, Contract, Intern")] string? employmentType = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var deptId = departmentId is not null ? Guid.Parse(departmentId) : (Guid?)null;
        var empStatus = status is not null && Enum.TryParse<EmployeeStatus>(status, true, out var s) ? s : (EmployeeStatus?)null;
        var empType = employmentType is not null && Enum.TryParse<EmploymentType>(employmentType, true, out var t) ? t : (EmploymentType?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<EmployeeListDto>>>(
            new GetEmployeesQuery(search, deptId, empStatus, empType, page, pageSize), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_hr_employees_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full employee details by ID, including department, manager, tags, and employment history.")]
    public async Task<EmployeeDto> GetEmployee(
        [Description("The employee ID (GUID)")] string employeeId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<EmployeeDto>>(
            new GetEmployeeByIdQuery(Guid.Parse(employeeId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_hr_departments_list", ReadOnly = true, Idempotent = true)]
    [Description("Get all departments as a tree structure (parent-child hierarchy). Includes manager info and employee count.")]
    public async Task<List<DepartmentTreeNodeDto>> ListDepartments(
        [Description("Include inactive departments (default: false)")] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<List<DepartmentTreeNodeDto>>>(
            new GetDepartmentsQuery(includeInactive), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_hr_reports", ReadOnly = true, Idempotent = true)]
    [Description("Get HR analytics: headcount by department, employment type distribution, tag statistics, and status breakdown.")]
    public async Task<HrReportsDto> GetReports(CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<HrReportsDto>>(
            new GetHrReportsQuery(), ct);
        return result.Unwrap();
    }
}
