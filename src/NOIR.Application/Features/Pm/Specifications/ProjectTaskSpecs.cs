namespace NOIR.Application.Features.Pm.Specifications;

/// <summary>
/// Get task by ID with all related data for detail view (read-only).
/// </summary>
public sealed class TaskByIdSpec : Specification<ProjectTask>
{
    public TaskByIdSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .Include(t => t.Project!)
             .Include(t => t.Column!)
             .Include(t => t.Assignee!)
             .Include(t => t.Reporter!)
             .Include(t => t.ParentTask!)
             .Include(t => t.SubTasks)
             .Include("SubTasks.Column")
             .Include("Comments.Author")
             .Include("TaskLabels.Label")
             .AsSplitQuery()
             .TagWith("TaskById");
    }
}

/// <summary>
/// Get task by ID with tracking for mutations.
/// </summary>
public sealed class TaskByIdForUpdateSpec : Specification<ProjectTask>
{
    public TaskByIdForUpdateSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .AsTracking()
             .TagWith("TaskByIdForUpdate");
    }
}

/// <summary>
/// Paginated, filterable task list by project.
/// </summary>
public sealed class TasksByProjectSpec : Specification<ProjectTask>
{
    public TasksByProjectSpec(
        Guid projectId,
        ProjectTaskStatus? status = null,
        TaskPriority? priority = null,
        Guid? assigneeId = null,
        string? search = null,
        int? skip = null,
        int? take = null)
    {
        Query.Where(t => t.ProjectId == projectId);

        if (status.HasValue)
            Query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            Query.Where(t => t.Priority == priority.Value);

        if (assigneeId.HasValue)
            Query.Where(t => t.AssigneeId == assigneeId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(t => t.Title.Contains(search) || t.TaskNumber.Contains(search));
        }

        Query.Include(t => t.Assignee!)
             .Include(t => t.Column!)
             .Include("TaskLabels.Label")
             .OrderByDescending(t => t.CreatedAt);

        if (skip.HasValue) Query.Skip(skip.Value);
        if (take.HasValue) Query.Take(take.Value);

        Query.TagWith("TasksByProject");
    }
}

/// <summary>
/// Count tasks matching filters (without pagination).
/// </summary>
public sealed class TasksByProjectCountSpec : Specification<ProjectTask>
{
    public TasksByProjectCountSpec(
        Guid projectId,
        ProjectTaskStatus? status = null,
        TaskPriority? priority = null,
        Guid? assigneeId = null,
        string? search = null)
    {
        Query.Where(t => t.ProjectId == projectId);

        if (status.HasValue)
            Query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            Query.Where(t => t.Priority == priority.Value);

        if (assigneeId.HasValue)
            Query.Where(t => t.AssigneeId == assigneeId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(t => t.Title.Contains(search) || t.TaskNumber.Contains(search));
        }

        Query.TagWith("TasksByProjectCount");
    }
}

/// <summary>
/// Get tasks by column for Kanban board, ordered by SortOrder.
/// </summary>
public sealed class TasksByColumnSpec : Specification<ProjectTask>
{
    public TasksByColumnSpec(Guid columnId)
    {
        Query.Where(t => t.ColumnId == columnId)
             .Include(t => t.Assignee!)
             .Include(t => t.SubTasks)
             .Include(t => t.Comments)
             .Include("TaskLabels.Label")
             .OrderBy(t => t.SortOrder)
             .TagWith("TasksByColumn");
    }
}

/// <summary>
/// Get all tasks for a project for Kanban board (all columns). Excludes archived tasks.
/// </summary>
public sealed class TasksForKanbanSpec : Specification<ProjectTask>
{
    public TasksForKanbanSpec(Guid projectId)
    {
        Query.Where(t => t.ProjectId == projectId && !t.IsArchived)
             .Include(t => t.Assignee!)
             .Include(t => t.Reporter!)
             .Include(t => t.SubTasks)
             .Include(t => t.Comments)
             .Include(t => t.ParentTask!)
             .Include("TaskLabels.Label")
             .OrderBy(t => t.SortOrder)
             .AsSplitQuery()
             .TagWith("TasksForKanban");
    }
}

/// <summary>
/// Get all archived tasks for a project (trash bin view).
/// </summary>
public sealed class ArchivedTasksByProjectSpec : Specification<ProjectTask>
{
    public ArchivedTasksByProjectSpec(Guid projectId)
    {
        Query.Where(t => t.ProjectId == projectId && t.IsArchived)
             .Include(t => t.Assignee!)
             .Include(t => t.SubTasks)
             .Include(t => t.Comments)
             .Include(t => t.ParentTask!)
             .Include("TaskLabels.Label")
             .OrderByDescending(t => t.ArchivedAt)
             .AsSplitQuery()
             .AsTracking()
             .TagWith("ArchivedTasksByProject");
    }
}

/// <summary>
/// Get archived tasks for a project (read-only, for listing).
/// </summary>
public sealed class ArchivedTasksByProjectReadSpec : Specification<ProjectTask>
{
    public ArchivedTasksByProjectReadSpec(Guid projectId)
    {
        Query.Where(t => t.ProjectId == projectId && t.IsArchived)
             .Include(t => t.Assignee!)
             .Include(t => t.SubTasks)
             .Include(t => t.Comments)
             .Include("TaskLabels.Label")
             .OrderByDescending(t => t.ArchivedAt)
             .AsSplitQuery()
             .TagWith("ArchivedTasksByProjectRead");
    }
}
