namespace NOIR.Application.Features.Hr.DTOs;

/// <summary>
/// Full employee details for detail view.
/// </summary>
public sealed record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? AvatarUrl,
    Guid DepartmentId,
    string DepartmentName,
    string? Position,
    Guid? ManagerId,
    string? ManagerName,
    string? UserId,
    bool HasUserAccount,
    DateTimeOffset JoinDate,
    DateTimeOffset? EndDate,
    EmployeeStatus Status,
    EmploymentType EmploymentType,
    string? Notes,
    IReadOnlyList<DirectReportDto> DirectReports,
    IReadOnlyList<TagBriefDto> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);

/// <summary>
/// Simplified employee for list/table views.
/// </summary>
public sealed record EmployeeListDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl,
    string DepartmentName,
    string? Position,
    string? ManagerName,
    EmployeeStatus Status,
    EmploymentType EmploymentType);

/// <summary>
/// Lightweight employee for autocomplete search.
/// </summary>
public sealed record EmployeeSearchDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string? AvatarUrl,
    string? Position,
    string DepartmentName);

/// <summary>
/// Direct report summary nested in EmployeeDto.
/// </summary>
public sealed record DirectReportDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string? AvatarUrl,
    string? Position,
    EmployeeStatus Status);

/// <summary>
/// Full department details.
/// </summary>
public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    Guid? ManagerId,
    string? ManagerName,
    Guid? ParentDepartmentId,
    string? ParentDepartmentName,
    int SortOrder,
    bool IsActive,
    int EmployeeCount,
    IReadOnlyList<DepartmentTreeNodeDto> SubDepartments);

/// <summary>
/// Department node for tree structure.
/// </summary>
public sealed record DepartmentTreeNodeDto(
    Guid Id,
    string Name,
    string Code,
    string? ManagerName,
    int EmployeeCount,
    bool IsActive,
    IReadOnlyList<DepartmentTreeNodeDto> Children);

/// <summary>
/// Org chart node (can represent either a Department or an Employee).
/// </summary>
public sealed record OrgChartNodeDto(
    Guid Id,
    OrgChartNodeType Type,
    string Name,
    string? Subtitle,
    string? AvatarUrl,
    int? EmployeeCount,
    EmployeeStatus? Status,
    IReadOnlyList<OrgChartNodeDto> Children);

/// <summary>
/// Type of node in the org chart.
/// </summary>
public enum OrgChartNodeType
{
    Department = 0,
    Employee = 1
}

/// <summary>
/// Request body for creating an employee.
/// </summary>
public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId,
    DateTimeOffset JoinDate,
    EmploymentType EmploymentType,
    string? Phone = null,
    string? AvatarUrl = null,
    string? Position = null,
    Guid? ManagerId = null,
    string? UserId = null,
    string? Notes = null);

/// <summary>
/// Request body for updating an employee.
/// </summary>
public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId,
    EmploymentType EmploymentType,
    string? Phone = null,
    string? AvatarUrl = null,
    string? Position = null,
    Guid? ManagerId = null,
    string? Notes = null);

/// <summary>
/// Request body for creating a department.
/// </summary>
public sealed record CreateDepartmentRequest(
    string Name,
    string Code,
    string? Description = null,
    Guid? ParentDepartmentId = null,
    Guid? ManagerId = null);

/// <summary>
/// Request body for updating a department.
/// </summary>
public sealed record UpdateDepartmentRequest(
    string Name,
    string Code,
    string? Description = null,
    Guid? ManagerId = null,
    Guid? ParentDepartmentId = null);

/// <summary>
/// Request body for reordering departments.
/// </summary>
public sealed record ReorderDepartmentsRequest(
    List<ReorderItem> Items);

/// <summary>
/// Single item in a reorder operation.
/// </summary>
public sealed record ReorderItem(
    Guid Id,
    int SortOrder);

// === Tag DTOs ===

/// <summary>
/// Full employee tag details.
/// </summary>
public sealed record EmployeeTagDto(
    Guid Id,
    string Name,
    EmployeeTagCategory Category,
    string Color,
    string? Description,
    int SortOrder,
    bool IsActive,
    int EmployeeCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);

/// <summary>
/// Lightweight tag for employee list views.
/// </summary>
public sealed record TagBriefDto(
    Guid Id,
    string Name,
    EmployeeTagCategory Category,
    string Color);

/// <summary>
/// Request body for creating a tag.
/// </summary>
public sealed record CreateEmployeeTagRequest(
    string Name,
    EmployeeTagCategory Category,
    string? Color = null,
    string? Description = null,
    int SortOrder = 0);

/// <summary>
/// Request body for updating a tag.
/// </summary>
public sealed record UpdateEmployeeTagRequest(
    string Name,
    EmployeeTagCategory Category,
    string? Color = null,
    string? Description = null,
    int SortOrder = 0);

/// <summary>
/// Request body for assigning tags to an employee.
/// </summary>
public sealed record AssignTagsRequest(
    List<Guid> TagIds);

/// <summary>
/// Request body for removing tags from an employee.
/// </summary>
public sealed record RemoveTagsRequest(
    List<Guid> TagIds);

// === Report DTOs ===

/// <summary>
/// HR reports aggregate data.
/// </summary>
public sealed record HrReportsDto(
    IReadOnlyList<DepartmentHeadcountDto> HeadcountByDepartment,
    IReadOnlyList<TagDistributionDto> TagDistribution,
    IReadOnlyList<EmploymentTypeBreakdownDto> EmploymentTypeBreakdown,
    IReadOnlyList<StatusBreakdownDto> StatusBreakdown,
    int TotalActiveEmployees,
    int TotalDepartments);

/// <summary>
/// Department headcount for reports.
/// </summary>
public sealed record DepartmentHeadcountDto(Guid DepartmentId, string DepartmentName, int Count);

/// <summary>
/// Tag distribution for reports.
/// </summary>
public sealed record TagDistributionDto(Guid TagId, string TagName, EmployeeTagCategory Category, string Color, int Count);

/// <summary>
/// Employment type breakdown for reports.
/// </summary>
public sealed record EmploymentTypeBreakdownDto(EmploymentType Type, int Count);

/// <summary>
/// Employee status breakdown for reports.
/// </summary>
public sealed record StatusBreakdownDto(EmployeeStatus Status, int Count);

// === Bulk Operation Request DTOs ===

/// <summary>
/// Request body for bulk assigning tags to employees.
/// </summary>
public sealed record BulkAssignTagsRequest(List<Guid> EmployeeIds, List<Guid> TagIds);

/// <summary>
/// Request body for bulk changing employee department.
/// </summary>
public sealed record BulkChangeDepartmentRequest(List<Guid> EmployeeIds, Guid NewDepartmentId);

// === Import DTOs ===

/// <summary>
/// Result of a CSV import operation.
/// </summary>
public sealed record ImportResultDto(
    int TotalRows,
    int SuccessCount,
    int FailedCount,
    List<ImportErrorDto> Errors);

/// <summary>
/// Error details for a failed import row.
/// </summary>
public sealed record ImportErrorDto(int RowNumber, string Message);
