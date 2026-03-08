using NOIR.Application.Features.Hr.Commands.CreateDepartment;
using NOIR.Application.Features.Hr.Commands.UpdateDepartment;
using NOIR.Application.Features.Hr.Commands.DeleteDepartment;
using NOIR.Application.Features.Hr.Commands.ReorderDepartments;
using NOIR.Application.Features.Hr.Queries.GetDepartments;
using NOIR.Application.Features.Hr.Queries.GetDepartmentById;
using NOIR.Application.Features.Hr.DTOs;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Department API endpoints.
/// Provides CRUD operations for HR departments.
/// </summary>
public static class DepartmentEndpoints
{
    public static void MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/hr/departments")
            .WithTags("HR - Departments")
            .RequireFeature(ModuleNames.Erp.HrSub.Departments)
            .RequireAuthorization();

        // Get all departments (tree structure)
        group.MapGet("/", async (
            [FromQuery] bool? includeInactive,
            IMessageBus bus) =>
        {
            var query = new GetDepartmentsQuery(includeInactive ?? false);
            var result = await bus.InvokeAsync<Result<List<DepartmentTreeNodeDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrDepartmentsRead)
        .WithName("GetDepartments")
        .WithSummary("Get department tree")
        .WithDescription("Returns all departments as a hierarchical tree structure with employee counts.")
        .Produces<List<DepartmentTreeNodeDto>>(StatusCodes.Status200OK);

        // Get department by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetDepartmentByIdQuery(id);
            var result = await bus.InvokeAsync<Result<DepartmentDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrDepartmentsRead)
        .WithName("GetDepartmentById")
        .WithSummary("Get department by ID")
        .WithDescription("Returns full department details including sub-departments and manager info.")
        .Produces<DepartmentDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create department
        group.MapPost("/", async (
            CreateDepartmentRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateDepartmentCommand(
                request.Name,
                request.Code,
                request.Description,
                request.ParentDepartmentId,
                request.ManagerId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<DepartmentDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrDepartmentsCreate)
        .WithName("CreateDepartment")
        .WithSummary("Create a new department")
        .WithDescription("Creates a new department with optional parent and manager assignments.")
        .Produces<DepartmentDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update department
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateDepartmentRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateDepartmentCommand(
                id,
                request.Name,
                request.Code,
                request.Description,
                request.ManagerId,
                request.ParentDepartmentId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<DepartmentDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrDepartmentsUpdate)
        .WithName("UpdateDepartment")
        .WithSummary("Update an existing department")
        .WithDescription("Updates department details. Validates against circular parent references.")
        .Produces<DepartmentDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete department (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IRepository<Department, Guid> departmentRepository,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            // Fetch department name for audit log display
            var spec = new DepartmentByIdReadOnlySpec(id);
            var dept = await departmentRepository.FirstOrDefaultAsync(spec, ct);

            var command = new DeleteDepartmentCommand(id, dept?.Name)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrDepartmentsDelete)
        .WithName("DeleteDepartment")
        .WithSummary("Soft-delete a department")
        .WithDescription("Soft-deletes a department. Fails if department has employees or active sub-departments.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Reorder departments
        group.MapPut("/reorder", async (
            ReorderDepartmentsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ReorderDepartmentsCommand(
                request.Items.Select(i => new ReorderItem(i.Id, i.SortOrder, i.ParentDepartmentId)).ToList())
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.HrDepartmentsUpdate)
        .WithName("ReorderDepartments")
        .WithSummary("Reorder departments")
        .WithDescription("Updates the sort order of multiple departments in a single request.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }
}
