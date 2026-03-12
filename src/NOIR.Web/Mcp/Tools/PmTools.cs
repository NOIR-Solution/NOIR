using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Pm.Commands.CreateTask;
using NOIR.Application.Features.Pm.Commands.UpdateTask;
using NOIR.Application.Features.Pm.DTOs;
using NOIR.Application.Features.Pm.Queries.GetProjectById;
using NOIR.Application.Features.Pm.Queries.GetProjects;
using NOIR.Application.Features.Pm.Queries.GetTasks;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for Project Management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Erp.Pm)]
public sealed class PmTools(IMessageBus bus, ICurrentUser currentUser)
{
    [McpServerTool(Name = "noir_pm_projects_list", ReadOnly = true, Idempotent = true)]
    [Description("List projects with pagination and filtering. Supports search, status, and owner filters.")]
    public async Task<PagedResult<ProjectListDto>> ListProjects(
        [Description("Search by project name or code")] string? search = null,
        [Description("Filter by status: Planning, Active, OnHold, Completed, Cancelled")] string? status = null,
        [Description("Filter by owner user ID (GUID)")] string? ownerId = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var projStatus = status is not null && Enum.TryParse<ProjectStatus>(status, true, out var s) ? s : (ProjectStatus?)null;
        var oId = ownerId is not null ? Guid.Parse(ownerId) : (Guid?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<ProjectListDto>>>(
            new GetProjectsQuery(search, projStatus, oId, page, pageSize), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_pm_projects_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full project details by ID, including columns, task counts, members, and labels.")]
    public async Task<ProjectDto> GetProject(
        [Description("The project ID (GUID)")] string projectId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<ProjectDto>>(
            new GetProjectByIdQuery(Guid.Parse(projectId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_pm_tasks_list", ReadOnly = true, Idempotent = true)]
    [Description("List tasks within a project with filtering. Supports status, priority, assignee, and search filters.")]
    public async Task<PagedResult<TaskCardDto>> ListTasks(
        [Description("The project ID (GUID) — required")] string projectId,
        [Description("Filter by status: Todo, InProgress, InReview, Done, Cancelled")] string? status = null,
        [Description("Filter by priority: Low, Medium, High, Urgent")] string? priority = null,
        [Description("Filter by assignee user ID (GUID)")] string? assigneeId = null,
        [Description("Search by task title or description")] string? search = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var taskStatus = status is not null && Enum.TryParse<ProjectTaskStatus>(status, true, out var s) ? s : (ProjectTaskStatus?)null;
        var taskPriority = priority is not null && Enum.TryParse<TaskPriority>(priority, true, out var p) ? p : (TaskPriority?)null;
        var aId = assigneeId is not null ? Guid.Parse(assigneeId) : (Guid?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<TaskCardDto>>>(
            new GetTasksQuery(Guid.Parse(projectId), taskStatus, taskPriority, aId, search, page, pageSize), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_pm_tasks_create", Destructive = false)]
    [Description("Create a new task in a project. Optionally assign to a user, set priority, due date, and parent task for subtasks.")]
    public async Task<TaskDto> CreateTask(
        [Description("The project ID (GUID)")] string projectId,
        [Description("Task title")] string title,
        [Description("Task description (optional)")] string? description = null,
        [Description("Priority: Low, Medium, High, Urgent")] string? priority = null,
        [Description("Assignee user ID (GUID, optional)")] string? assigneeId = null,
        [Description("Due date (ISO 8601, optional)")] string? dueDate = null,
        [Description("Estimated hours (optional)")] decimal? estimatedHours = null,
        [Description("Parent task ID for subtasks (GUID, optional)")] string? parentTaskId = null,
        [Description("Column ID to place the task in (GUID, optional)")] string? columnId = null,
        CancellationToken ct = default)
    {
        var taskPriority = priority is not null && Enum.TryParse<TaskPriority>(priority, true, out var p) ? p : (TaskPriority?)null;
        var aId = assigneeId is not null ? Guid.Parse(assigneeId) : (Guid?)null;
        var due = dueDate is not null ? DateTimeOffset.Parse(dueDate) : (DateTimeOffset?)null;
        var pId = parentTaskId is not null ? Guid.Parse(parentTaskId) : (Guid?)null;
        var cId = columnId is not null ? Guid.Parse(columnId) : (Guid?)null;

        var command = new CreateTaskCommand(Guid.Parse(projectId), title, description, taskPriority, aId, due, estimatedHours, pId, cId)
        {
            AuditUserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<TaskDto>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_pm_tasks_update", Destructive = false)]
    [Description("Update an existing task. Only provided fields are updated — omitted fields remain unchanged.")]
    public async Task<TaskDto> UpdateTask(
        [Description("The task ID (GUID)")] string taskId,
        [Description("New title (optional)")] string? title = null,
        [Description("New description (optional)")] string? description = null,
        [Description("New priority: Low, Medium, High, Urgent (optional)")] string? priority = null,
        [Description("New assignee user ID (GUID, optional)")] string? assigneeId = null,
        [Description("New due date (ISO 8601, optional)")] string? dueDate = null,
        [Description("Estimated hours (optional)")] decimal? estimatedHours = null,
        [Description("Actual hours spent (optional)")] decimal? actualHours = null,
        CancellationToken ct = default)
    {
        var taskPriority = priority is not null && Enum.TryParse<TaskPriority>(priority, true, out var p) ? p : (TaskPriority?)null;
        var aId = assigneeId is not null ? Guid.Parse(assigneeId) : (Guid?)null;
        var due = dueDate is not null ? DateTimeOffset.Parse(dueDate) : (DateTimeOffset?)null;

        var command = new UpdateTaskCommand(Guid.Parse(taskId), title, description, taskPriority, aId, due, estimatedHours, actualHours)
        {
            AuditUserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<TaskDto>>(command, ct);
        return result.Unwrap();
    }
}
