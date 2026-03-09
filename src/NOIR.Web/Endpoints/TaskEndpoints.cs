using NOIR.Application.Features.Pm.Commands.CreateTask;
using NOIR.Application.Features.Pm.Commands.UpdateTask;
using NOIR.Application.Features.Pm.Commands.MoveTask;
using NOIR.Application.Features.Pm.Commands.ChangeTaskStatus;
using NOIR.Application.Features.Pm.Commands.DeleteTask;
using NOIR.Application.Features.Pm.Commands.ArchiveTask;
using NOIR.Application.Features.Pm.Commands.RestoreTask;
using NOIR.Application.Features.Pm.Commands.PermanentDeleteTask;
using NOIR.Application.Features.Pm.Commands.EmptyProjectTrash;
using NOIR.Application.Features.Pm.Commands.AddTaskComment;
using NOIR.Application.Features.Pm.Commands.UpdateTaskComment;
using NOIR.Application.Features.Pm.Commands.DeleteTaskComment;
using NOIR.Application.Features.Pm.Commands.AddLabelToTask;
using NOIR.Application.Features.Pm.Commands.RemoveLabelFromTask;
using NOIR.Application.Features.Pm.Commands.ReorderTask;
using NOIR.Application.Features.Pm.Commands.AddSubtask;
using NOIR.Application.Features.Pm.Queries.GetTasks;
using NOIR.Application.Features.Pm.Queries.GetTaskById;
using NOIR.Application.Features.Pm.Queries.GetArchivedTasks;
using NOIR.Application.Features.Pm.Queries.SearchTasks;
using NOIR.Application.Features.Pm.DTOs;

namespace NOIR.Web.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pm/tasks")
            .WithTags("PM - Tasks")
            .RequireFeature(ModuleNames.Erp.Pm)
            .RequireAuthorization();

        // ── Tasks ───────────────────────────────────────────────────

        group.MapGet("/", async (
            [FromQuery] Guid projectId,
            [FromQuery] ProjectTaskStatus? status,
            [FromQuery] TaskPriority? priority,
            [FromQuery] Guid? assigneeId,
            [FromQuery] string? search,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetTasksQuery(projectId, status, priority, assigneeId, search, page ?? 1, pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<TaskCardDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksRead)
        .WithName("GetTasks")
        .WithSummary("Get paginated list of tasks")
        .Produces<PagedResult<TaskCardDto>>(StatusCodes.Status200OK);

        group.MapGet("/search", async (
            [FromQuery] Guid projectId,
            [FromQuery] string q,
            [FromQuery] int? take,
            IMessageBus bus) =>
        {
            var query = new SearchTasksQuery(projectId, q, take ?? 10);
            var result = await bus.InvokeAsync<Result<List<TaskSearchDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksRead)
        .WithName("SearchTasks")
        .WithSummary("Search tasks within a project for autocomplete")
        .Produces<List<TaskSearchDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetTaskByIdQuery(id);
            var result = await bus.InvokeAsync<Result<TaskDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksRead)
        .WithName("GetTaskById")
        .WithSummary("Get task by ID")
        .Produces<TaskDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
            CreateTaskRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateTaskCommand(
                request.ProjectId, request.Title, request.Description, request.Priority,
                request.AssigneeId, request.DueDate, request.EstimatedHours,
                request.ParentTaskId, request.ColumnId)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksCreate)
        .WithName("CreateTask")
        .WithSummary("Create a new task")
        .Produces<TaskDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTaskRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateTaskCommand(
                id, request.Title, request.Description, request.Priority,
                request.AssigneeId, request.DueDate, request.EstimatedHours, request.ActualHours)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("UpdateTask")
        .WithSummary("Update a task")
        .Produces<TaskDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/move", async (
            Guid id,
            MoveTaskRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new MoveTaskCommand(id, request.ColumnId, request.SortOrder)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("MoveTask")
        .WithSummary("Move a task to a different column on the Kanban board")
        .Produces<TaskDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/status", async (
            Guid id,
            ChangeTaskStatusRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ChangeTaskStatusCommand(id, request.Status)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("ChangeTaskStatus")
        .WithSummary("Change task status")
        .Produces<TaskDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/reorder", async (
            Guid id,
            ReorderTaskRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ReorderTaskCommand(id, request.SortOrder)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("ReorderTask")
        .WithSummary("Reorder a task within its column")
        .Produces<TaskDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/subtasks", async (
            Guid id,
            AddSubtaskRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddSubtaskCommand(id, request.Title, request.Description,
                request.Priority ?? TaskPriority.Medium, request.AssigneeId)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksCreate)
        .WithName("AddSubtask")
        .WithSummary("Add a subtask to an existing task")
        .Produces<TaskDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteTaskCommand(id) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TaskDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksDelete)
        .WithName("DeleteTask")
        .WithSummary("Delete a task (soft delete)")
        .Produces<TaskDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ArchiveTaskCommand(id) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<Guid>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("ArchiveTask")
        .WithSummary("Archive a task (moves to trash bin)")
        .Produces<Guid>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/restore", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RestoreTaskCommand(id) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<Guid>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("RestoreTask")
        .WithSummary("Restore a task from the archive")
        .Produces<Guid>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/permanent", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new PermanentDeleteTaskCommand(id) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<Guid>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksDelete)
        .WithName("PermanentDeleteTask")
        .WithSummary("Permanently delete an archived task")
        .Produces<Guid>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/archived", async (
            [FromQuery] Guid projectId,
            IMessageBus bus) =>
        {
            var query = new GetArchivedTasksQuery(projectId);
            var result = await bus.InvokeAsync<Result<List<ArchivedTaskCardDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksRead)
        .WithName("GetArchivedTasks")
        .WithSummary("Get all archived (trashed) tasks for a project")
        .Produces<List<ArchivedTaskCardDto>>(StatusCodes.Status200OK);

        group.MapDelete("/archived", async (
            [FromQuery] Guid projectId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new EmptyProjectTrashCommand(projectId) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<int>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksDelete)
        .WithName("EmptyProjectTrash")
        .WithSummary("Permanently delete all archived tasks for a project")
        .Produces<int>(StatusCodes.Status200OK);

        // ── Comments ────────────────────────────────────────────────

        group.MapPost("/{id:guid}/comments", async (
            Guid id,
            AddTaskCommentRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddTaskCommentCommand(id, request.Content)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskCommentDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("AddTaskComment")
        .WithSummary("Add a comment to a task")
        .Produces<TaskCommentDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/comments/{commentId:guid}", async (
            Guid id,
            Guid commentId,
            UpdateTaskCommentRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateTaskCommentCommand(id, commentId, request.Content)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskCommentDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("UpdateTaskComment")
        .WithSummary("Update a task comment")
        .Produces<TaskCommentDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/comments/{commentId:guid}", async (
            Guid id,
            Guid commentId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteTaskCommentCommand(id, commentId) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TaskCommentDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksDelete)
        .WithName("DeleteTaskComment")
        .WithSummary("Delete a task comment")
        .Produces<TaskCommentDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ── Task Labels ─────────────────────────────────────────────

        group.MapPost("/{id:guid}/labels/{labelId:guid}", async (
            Guid id,
            Guid labelId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddLabelToTaskCommand(id, labelId) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TaskLabelBriefDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("AddLabelToTask")
        .WithSummary("Add a label to a task")
        .Produces<TaskLabelBriefDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/labels/{labelId:guid}", async (
            Guid id,
            Guid labelId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RemoveLabelFromTaskCommand(id, labelId) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TaskLabelBriefDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksUpdate)
        .WithName("RemoveLabelFromTask")
        .WithSummary("Remove a label from a task")
        .Produces<TaskLabelBriefDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
