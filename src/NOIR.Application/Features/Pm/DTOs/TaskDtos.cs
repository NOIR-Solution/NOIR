namespace NOIR.Application.Features.Pm.DTOs;

/// <summary>
/// Full task details for detail view.
/// </summary>
public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    string TaskNumber,
    string Title,
    string? Description,
    ProjectTaskStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid? ReporterId,
    string? ReporterName,
    DateTimeOffset? DueDate,
    decimal? EstimatedHours,
    decimal? ActualHours,
    Guid? ParentTaskId,
    string? ParentTaskNumber,
    Guid? ColumnId,
    string? ColumnName,
    DateTimeOffset? CompletedAt,
    List<TaskLabelBriefDto> Labels,
    List<SubtaskDto> Subtasks,
    List<TaskCommentDto> Comments,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string? ProjectName = null,
    string? AssigneeAvatarUrl = null);

/// <summary>
/// Kanban card for board view.
/// </summary>
public sealed record TaskCardDto(
    Guid Id,
    string TaskNumber,
    string Title,
    ProjectTaskStatus Status,
    TaskPriority Priority,
    string? AssigneeName,
    string? AssigneeAvatarUrl,
    DateTimeOffset? DueDate,
    int CommentCount,
    int SubtaskCount,
    int CompletedSubtaskCount,
    List<TaskLabelBriefDto> Labels,
    double SortOrder);

/// <summary>
/// Subtask brief info nested in TaskDto.
/// </summary>
public sealed record SubtaskDto(
    Guid Id,
    string TaskNumber,
    string Title,
    ProjectTaskStatus Status,
    TaskPriority Priority,
    string? AssigneeName);

/// <summary>
/// Task comment detail.
/// </summary>
public sealed record TaskCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    string Content,
    bool IsEdited,
    DateTimeOffset CreatedAt);

/// <summary>
/// Full task label detail.
/// </summary>
public sealed record TaskLabelDto(Guid Id, string Name, string Color);

/// <summary>
/// Brief label info for task cards and detail views.
/// </summary>
public sealed record TaskLabelBriefDto(Guid Id, string Name, string Color);

/// <summary>
/// Kanban board view with columns and task cards.
/// </summary>
public sealed record KanbanBoardDto(
    Guid ProjectId,
    string ProjectName,
    List<KanbanColumnDto> Columns);

/// <summary>
/// Kanban column with its task cards.
/// </summary>
public sealed record KanbanColumnDto(
    Guid Id,
    string Name,
    int SortOrder,
    string? Color,
    int? WipLimit,
    List<TaskCardDto> Tasks);

/// <summary>
/// Request body for creating a task.
/// </summary>
public sealed record CreateTaskRequest(
    Guid ProjectId,
    string Title,
    string? Description = null,
    TaskPriority? Priority = null,
    Guid? AssigneeId = null,
    DateTimeOffset? DueDate = null,
    decimal? EstimatedHours = null,
    Guid? ParentTaskId = null,
    Guid? ColumnId = null);

/// <summary>
/// Request body for updating a task.
/// </summary>
public sealed record UpdateTaskRequest(
    string? Title = null,
    string? Description = null,
    TaskPriority? Priority = null,
    Guid? AssigneeId = null,
    DateTimeOffset? DueDate = null,
    decimal? EstimatedHours = null,
    decimal? ActualHours = null);

/// <summary>
/// Request body for moving a task on the Kanban board.
/// </summary>
public sealed record MoveTaskRequest(Guid ColumnId, double SortOrder);

/// <summary>
/// Request body for changing task status.
/// </summary>
public sealed record ChangeTaskStatusRequest(ProjectTaskStatus Status);

/// <summary>
/// Request body for adding a comment to a task.
/// </summary>
public sealed record AddTaskCommentRequest(string Content);

/// <summary>
/// Request body for creating a task label.
/// </summary>
public sealed record CreateTaskLabelRequest(string Name, string Color);

/// <summary>
/// Request body for creating a column.
/// </summary>
public sealed record CreateColumnRequest(string Name, string? Color = null, int? WipLimit = null);

/// <summary>
/// Request body for updating a column.
/// </summary>
public sealed record UpdateColumnRequest(string Name, string? Color = null, int? WipLimit = null);

/// <summary>
/// Request body for reordering columns.
/// </summary>
public sealed record ReorderColumnsRequest(List<Guid> ColumnIds);

/// <summary>
/// Request body for deleting a column (requires target column for task migration).
/// </summary>
public sealed record DeleteColumnRequest(Guid MoveToColumnId);

/// <summary>
/// Lightweight task search result for autocomplete.
/// </summary>
public sealed record TaskSearchDto(
    Guid Id,
    string TaskNumber,
    string Title,
    ProjectTaskStatus Status,
    TaskPriority Priority);

/// <summary>
/// Request body for updating a task label.
/// </summary>
public sealed record UpdateTaskLabelRequest(string Name, string Color);

/// <summary>
/// Request body for updating a task comment.
/// </summary>
public sealed record UpdateTaskCommentRequest(string Content);

/// <summary>
/// Request body for reordering a task within its column.
/// </summary>
public sealed record ReorderTaskRequest(double SortOrder);

/// <summary>
/// Request body for adding a subtask to a task.
/// </summary>
public sealed record AddSubtaskRequest(
    string Title,
    string? Description = null,
    TaskPriority? Priority = null,
    Guid? AssigneeId = null);

/// <summary>
/// Request body for changing project status.
/// </summary>
public sealed record ChangeProjectStatusRequest(ProjectStatus Status);
