namespace NOIR.Application.Features.Pm.DTOs;

/// <summary>
/// Full project details for detail view.
/// </summary>
public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    ProjectStatus Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? DueDate,
    Guid? OwnerId,
    string? OwnerName,
    decimal? Budget,
    string? Currency,
    string? Color,
    string? Icon,
    ProjectVisibility Visibility,
    List<ProjectMemberDto> Members,
    List<ProjectColumnDto> Columns,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string ProjectCode = "",
    string? OwnerAvatarUrl = null,
    int MemberCount = 0,
    int TaskCount = 0,
    int CompletedTaskCount = 0,
    decimal ProgressPercent = 0,
    List<TaskLabelDto>? Labels = null);

/// <summary>
/// Simplified project for list/table views.
/// </summary>
public sealed record ProjectListDto(
    Guid Id,
    string Name,
    string Slug,
    ProjectStatus Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? DueDate,
    string? OwnerName,
    int MemberCount,
    int TaskCount,
    int CompletedTaskCount,
    string? Color,
    string? Icon,
    ProjectVisibility Visibility,
    DateTimeOffset CreatedAt,
    string ProjectCode = "",
    Guid? OwnerId = null,
    string? OwnerAvatarUrl = null,
    decimal ProgressPercent = 0);

/// <summary>
/// Project member detail.
/// </summary>
public sealed record ProjectMemberDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string? AvatarUrl,
    ProjectMemberRole Role,
    DateTimeOffset JoinedAt,
    string? EmployeeCode = null,
    string? Position = null);

/// <summary>
/// Project column detail.
/// </summary>
public sealed record ProjectColumnDto(
    Guid Id,
    string Name,
    int SortOrder,
    string? Color,
    int? WipLimit,
    string? StatusMapping = null,
    int TaskCount = 0);

/// <summary>
/// Request body for creating a project.
/// </summary>
public sealed record CreateProjectRequest(
    string Name,
    string? Description = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    DateTimeOffset? DueDate = null,
    decimal? Budget = null,
    string? Currency = null,
    string? Color = null,
    string? Icon = null,
    ProjectVisibility? Visibility = null);

/// <summary>
/// Request body for updating a project.
/// </summary>
public sealed record UpdateProjectRequest(
    string Name,
    string? Description = null,
    ProjectStatus? Status = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    DateTimeOffset? DueDate = null,
    decimal? Budget = null,
    string? Currency = null,
    string? Color = null,
    string? Icon = null,
    ProjectVisibility? Visibility = null);

/// <summary>
/// Request body for adding a member to a project.
/// </summary>
public sealed record AddProjectMemberRequest(Guid EmployeeId, ProjectMemberRole Role);

/// <summary>
/// Request body for changing a member's role.
/// </summary>
public sealed record ChangeProjectMemberRoleRequest(ProjectMemberRole Role);

/// <summary>
/// Lightweight project search result for autocomplete.
/// </summary>
public sealed record ProjectSearchDto(
    Guid Id,
    string Name,
    string Slug,
    ProjectStatus Status,
    string? Color,
    string? Icon,
    string ProjectCode = "");
