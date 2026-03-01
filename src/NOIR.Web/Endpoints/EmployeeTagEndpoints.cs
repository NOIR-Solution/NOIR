using NOIR.Application.Features.Hr.Commands.CreateTag;
using NOIR.Application.Features.Hr.Commands.UpdateTag;
using NOIR.Application.Features.Hr.Commands.DeleteTag;
using NOIR.Application.Features.Hr.Commands.AssignTagsToEmployee;
using NOIR.Application.Features.Hr.Commands.RemoveTagsFromEmployee;
using NOIR.Application.Features.Hr.Queries.GetTags;
using NOIR.Application.Features.Hr.Queries.GetEmployeesByTag;
using NOIR.Application.Features.Hr.DTOs;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Employee Tag API endpoints.
/// Provides CRUD operations for HR employee tags and tag assignments.
/// </summary>
public static class EmployeeTagEndpoints
{
    public static void MapEmployeeTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/hr/tags")
            .WithTags("HR - Tags")
            .RequireFeature(ModuleNames.Erp.Hr)
            .RequireAuthorization();

        // Get all tags (with optional filters)
        group.MapGet("/", async (
            [FromQuery] EmployeeTagCategory? category,
            [FromQuery] bool? isActive,
            IMessageBus bus) =>
        {
            var query = new GetTagsQuery(category, isActive);
            var result = await bus.InvokeAsync<Result<List<EmployeeTagDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrTagsRead)
        .WithName("GetEmployeeTags")
        .WithSummary("Get all employee tags")
        .WithDescription("Returns all employee tags with optional filtering by category and active status.")
        .Produces<List<EmployeeTagDto>>(StatusCodes.Status200OK);

        // Get single tag by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var spec = new EmployeeTagByIdSpec(id);
            // Use GetTags with a direct lookup — reuse the handler pattern
            var query = new GetTagsQuery();
            var result = await bus.InvokeAsync<Result<List<EmployeeTagDto>>>(query);
            if (result.IsFailure) return result.ToHttpResult();

            var tag = result.Value.FirstOrDefault(t => t.Id == id);
            if (tag is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: $"Employee tag with ID '{id}' not found.");
            }
            return Results.Ok(tag);
        })
        .RequireAuthorization(Permissions.HrTagsRead)
        .WithName("GetEmployeeTagById")
        .WithSummary("Get employee tag by ID")
        .WithDescription("Returns a single employee tag with employee count.")
        .Produces<EmployeeTagDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create tag
        group.MapPost("/", async (
            CreateEmployeeTagRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateTagCommand(
                request.Name,
                request.Category,
                request.Color,
                request.Description,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<EmployeeTagDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrTagsManage)
        .WithName("CreateEmployeeTag")
        .WithSummary("Create a new employee tag")
        .WithDescription("Creates a new tag definition that can be assigned to employees.")
        .Produces<EmployeeTagDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update tag
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateEmployeeTagRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateTagCommand(
                id,
                request.Name,
                request.Category,
                request.Color,
                request.Description,
                request.SortOrder)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<EmployeeTagDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrTagsManage)
        .WithName("UpdateEmployeeTag")
        .WithSummary("Update an existing employee tag")
        .WithDescription("Updates tag details including name, category, color, and description.")
        .Produces<EmployeeTagDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete tag (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IRepository<EmployeeTag, Guid> tagRepository,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            // Fetch tag name for audit log display
            var spec = new EmployeeTagByIdSpec(id);
            var tag = await tagRepository.FirstOrDefaultAsync(spec, ct);

            var command = new DeleteTagCommand(id, tag?.Name)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrTagsManage)
        .WithName("DeleteEmployeeTag")
        .WithSummary("Soft-delete an employee tag")
        .WithDescription("Soft-deletes a tag and all its employee assignments.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Assign tags to employee
        group.MapPost("/employees/{employeeId:guid}/assign", async (
            Guid employeeId,
            AssignTagsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AssignTagsToEmployeeCommand(employeeId, request.TagIds)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<List<TagBriefDto>>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrTagsManage)
        .WithName("AssignTagsToEmployee")
        .WithSummary("Assign tags to an employee")
        .WithDescription("Assigns one or more tags to an employee. Skips already-assigned tags. Returns all current tags.")
        .Produces<List<TagBriefDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Remove tags from employee
        group.MapPost("/employees/{employeeId:guid}/remove", async (
            Guid employeeId,
            RemoveTagsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RemoveTagsFromEmployeeCommand(employeeId, request.TagIds)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<List<TagBriefDto>>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrTagsManage)
        .WithName("RemoveTagsFromEmployee")
        .WithSummary("Remove tags from an employee")
        .WithDescription("Removes one or more tags from an employee. Returns remaining tags.")
        .Produces<List<TagBriefDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get employees by tag
        group.MapGet("/{id:guid}/employees", async (
            Guid id,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetEmployeesByTagQuery(id, page ?? 1, pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<EmployeeListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrTagsRead)
        .WithName("GetEmployeesByTag")
        .WithSummary("Get employees assigned to a tag")
        .WithDescription("Returns a paginated list of employees that have a specific tag assigned.")
        .Produces<PagedResult<EmployeeListDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
