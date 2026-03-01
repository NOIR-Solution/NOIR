using NOIR.Application.Features.Pm.Commands.CreateProject;
using NOIR.Application.Features.Pm.Commands.UpdateProject;
using NOIR.Application.Features.Pm.Commands.ArchiveProject;
using NOIR.Application.Features.Pm.Commands.ChangeProjectStatus;
using NOIR.Application.Features.Pm.Commands.DeleteProject;
using NOIR.Application.Features.Pm.Commands.AddProjectMember;
using NOIR.Application.Features.Pm.Commands.RemoveProjectMember;
using NOIR.Application.Features.Pm.Commands.ChangeProjectMemberRole;
using NOIR.Application.Features.Pm.Commands.CreateTaskLabel;
using NOIR.Application.Features.Pm.Commands.UpdateTaskLabel;
using NOIR.Application.Features.Pm.Commands.DeleteTaskLabel;
using NOIR.Application.Features.Pm.Commands.CreateColumn;
using NOIR.Application.Features.Pm.Commands.UpdateColumn;
using NOIR.Application.Features.Pm.Commands.ReorderColumns;
using NOIR.Application.Features.Pm.Commands.DeleteColumn;
using NOIR.Application.Features.Pm.Queries.GetProjects;
using NOIR.Application.Features.Pm.Queries.SearchProjects;
using NOIR.Application.Features.Pm.Queries.GetProjectById;
using NOIR.Application.Features.Pm.Queries.GetProjectMembers;
using NOIR.Application.Features.Pm.Queries.GetProjectLabels;
using NOIR.Application.Features.Pm.Queries.GetKanbanBoard;
using NOIR.Application.Features.Pm.DTOs;

namespace NOIR.Web.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pm/projects")
            .WithTags("PM - Projects")
            .RequireFeature(ModuleNames.Erp.Pm)
            .RequireAuthorization();

        // ── Projects ────────────────────────────────────────────────

        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] ProjectStatus? status,
            [FromQuery] Guid? ownerId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetProjectsQuery(search, status, ownerId, page ?? 1, pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<ProjectListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsRead)
        .WithName("GetProjects")
        .WithSummary("Get paginated list of projects")
        .Produces<PagedResult<ProjectListDto>>(StatusCodes.Status200OK);

        group.MapGet("/search", async (
            [FromQuery] string q,
            [FromQuery] int? take,
            IMessageBus bus) =>
        {
            var query = new SearchProjectsQuery(q, take ?? 10);
            var result = await bus.InvokeAsync<Result<List<ProjectSearchDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsRead)
        .WithName("SearchProjects")
        .WithSummary("Search projects for autocomplete")
        .Produces<List<ProjectSearchDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetProjectByIdQuery(id);
            var result = await bus.InvokeAsync<Result<ProjectDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsRead)
        .WithName("GetProjectById")
        .WithSummary("Get project by ID")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
            CreateProjectRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateProjectCommand(
                request.Name, request.Description, request.StartDate, request.EndDate,
                request.DueDate, request.Budget, request.Currency, request.Color,
                request.Icon, request.Visibility)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProjectDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsCreate)
        .WithName("CreateProject")
        .WithSummary("Create a new project")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProjectRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateProjectCommand(
                id, request.Name, request.Description, request.Status,
                request.StartDate, request.EndDate, request.DueDate,
                request.Budget, request.Currency, request.Color, request.Icon,
                request.Visibility)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProjectDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("UpdateProject")
        .WithSummary("Update a project")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ArchiveProjectCommand(id) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ProjectDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("ArchiveProject")
        .WithSummary("Archive a project")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/status", async (
            Guid id,
            ChangeProjectStatusRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ChangeProjectStatusCommand(id, request.Status)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProjectDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("ChangeProjectStatus")
        .WithSummary("Change project status with state machine validation")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteProjectCommand(id) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ProjectDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsDelete)
        .WithName("DeleteProject")
        .WithSummary("Delete a project")
        .Produces<ProjectDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ── Members ─────────────────────────────────────────────────

        group.MapGet("/{id:guid}/members", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetProjectMembersQuery(id);
            var result = await bus.InvokeAsync<Result<List<ProjectMemberDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsRead)
        .WithName("GetProjectMembers")
        .WithSummary("Get all members of a project")
        .Produces<List<ProjectMemberDto>>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/members", async (
            Guid id,
            AddProjectMemberRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddProjectMemberCommand(id, request.EmployeeId, request.Role)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProjectMemberDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmMembersManage)
        .WithName("AddProjectMember")
        .WithSummary("Add a member to a project")
        .Produces<ProjectMemberDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/members/{memberId:guid}", async (
            Guid id,
            Guid memberId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RemoveProjectMemberCommand(id, memberId) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<ProjectMemberDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmMembersManage)
        .WithName("RemoveProjectMember")
        .WithSummary("Remove a member from a project")
        .Produces<ProjectMemberDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/members/{memberId:guid}/role", async (
            Guid id,
            Guid memberId,
            ChangeProjectMemberRoleRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ChangeProjectMemberRoleCommand(id, memberId, request.Role)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProjectMemberDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmMembersManage)
        .WithName("ChangeProjectMemberRole")
        .WithSummary("Change a member's role within a project")
        .Produces<ProjectMemberDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ── Labels ──────────────────────────────────────────────────

        group.MapGet("/{id:guid}/labels", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetProjectLabelsQuery(id);
            var result = await bus.InvokeAsync<Result<List<TaskLabelDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsRead)
        .WithName("GetProjectLabels")
        .WithSummary("Get all labels for a project")
        .Produces<List<TaskLabelDto>>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/labels", async (
            Guid id,
            CreateTaskLabelRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateTaskLabelCommand(id, request.Name, request.Color)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskLabelDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("CreateTaskLabel")
        .WithSummary("Create a new task label")
        .Produces<TaskLabelDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/labels/{labelId:guid}", async (
            Guid id,
            Guid labelId,
            UpdateTaskLabelRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateTaskLabelCommand(id, labelId, request.Name, request.Color)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<TaskLabelDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("UpdateTaskLabel")
        .WithSummary("Update a task label")
        .Produces<TaskLabelDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/labels/{labelId:guid}", async (
            Guid id,
            Guid labelId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteTaskLabelCommand(id, labelId) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TaskLabelDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("DeleteTaskLabel")
        .WithSummary("Delete a task label")
        .Produces<TaskLabelDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ── Kanban Board ────────────────────────────────────────────

        group.MapGet("/{id:guid}/board", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetKanbanBoardQuery(id);
            var result = await bus.InvokeAsync<Result<KanbanBoardDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmTasksRead)
        .WithName("GetKanbanBoard")
        .WithSummary("Get Kanban board for a project")
        .Produces<KanbanBoardDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ── Columns ─────────────────────────────────────────────────

        group.MapPost("/{id:guid}/columns", async (
            Guid id,
            CreateColumnRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateColumnCommand(id, request.Name, request.Color, request.WipLimit)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProjectColumnDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("CreateProjectColumn")
        .WithSummary("Create a new column")
        .Produces<ProjectColumnDto>(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}/columns/{columnId:guid}", async (
            Guid id,
            Guid columnId,
            UpdateColumnRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateColumnCommand(id, columnId, request.Name, request.Color, request.WipLimit)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProjectColumnDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("UpdateProjectColumn")
        .WithSummary("Update a column")
        .Produces<ProjectColumnDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/columns/reorder", async (
            Guid id,
            ReorderColumnsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ReorderColumnsCommand(id, request.ColumnIds)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<List<ProjectColumnDto>>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("ReorderProjectColumns")
        .WithSummary("Reorder project columns")
        .Produces<List<ProjectColumnDto>>(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}/columns/{columnId:guid}", async (
            Guid id,
            Guid columnId,
            [FromQuery] Guid moveToColumnId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteColumnCommand(id, columnId, moveToColumnId)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ProjectColumnDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PmProjectsUpdate)
        .WithName("DeleteProjectColumn")
        .WithSummary("Delete a column and move tasks to another column")
        .Produces<ProjectColumnDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
