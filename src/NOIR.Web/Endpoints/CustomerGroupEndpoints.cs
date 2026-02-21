using NOIR.Application.Features.CustomerGroups.Commands.CreateCustomerGroup;
using NOIR.Application.Features.CustomerGroups.Commands.DeleteCustomerGroup;
using NOIR.Application.Features.CustomerGroups.Commands.UpdateCustomerGroup;
using NOIR.Application.Features.CustomerGroups.Commands.AssignCustomersToGroup;
using NOIR.Application.Features.CustomerGroups.Commands.RemoveCustomersFromGroup;
using NOIR.Application.Features.CustomerGroups.DTOs;
using NOIR.Application.Features.CustomerGroups.Queries.GetCustomerGroupById;
using NOIR.Application.Features.CustomerGroups.Queries.GetCustomerGroups;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Customer Group API endpoints.
/// Provides CRUD operations and member management for customer groups.
/// </summary>
public static class CustomerGroupEndpoints
{
    public static void MapCustomerGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customer-groups")
            .WithTags("Customer Groups")
            .RequireFeature(ModuleNames.Ecommerce.CustomerGroups)
            .RequireAuthorization();

        // Get all customer groups (paged)
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            IMessageBus bus = null!) =>
        {
            var query = new GetCustomerGroupsQuery(search, isActive, pageNumber, pageSize);
            var result = await bus.InvokeAsync<Result<PagedResult<CustomerGroupListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomerGroupsRead)
        .WithName("GetCustomerGroups")
        .WithSummary("Get list of customer groups")
        .WithDescription("Returns paged list of customer groups with optional filtering.")
        .Produces<PagedResult<CustomerGroupListDto>>(StatusCodes.Status200OK);

        // Get customer group by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetCustomerGroupByIdQuery(id);
            var result = await bus.InvokeAsync<Result<CustomerGroupDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomerGroupsRead)
        .WithName("GetCustomerGroupById")
        .WithSummary("Get customer group by ID")
        .WithDescription("Returns customer group details by ID.")
        .Produces<CustomerGroupDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create customer group
        group.MapPost("/", async (
            CreateCustomerGroupRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateCustomerGroupCommand(
                request.Name,
                request.Description)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<CustomerGroupDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomerGroupsCreate)
        .WithName("CreateCustomerGroup")
        .WithSummary("Create a new customer group")
        .WithDescription("Creates a new customer group for segmentation.")
        .Produces<CustomerGroupDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update customer group
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCustomerGroupRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateCustomerGroupCommand(
                id,
                request.Name,
                request.Description,
                request.IsActive)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<CustomerGroupDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomerGroupsUpdate)
        .WithName("UpdateCustomerGroup")
        .WithSummary("Update an existing customer group")
        .WithDescription("Updates customer group details.")
        .Produces<CustomerGroupDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Delete customer group (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteCustomerGroupCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomerGroupsDelete)
        .WithName("DeleteCustomerGroup")
        .WithSummary("Soft-delete a customer group")
        .WithDescription("Soft-deletes a customer group. Will fail if it has members.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Assign customers to group
        group.MapPost("/{id:guid}/members", async (
            Guid id,
            AssignCustomersToGroupRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AssignCustomersToGroupCommand(id, request.CustomerIds)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomerGroupsManageMembers)
        .WithName("AssignCustomersToGroup")
        .WithSummary("Assign customers to a group")
        .WithDescription("Adds one or more customers as members of the specified group.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Remove customers from group
        group.MapDelete("/{id:guid}/members", async (
            Guid id,
            [FromBody] RemoveCustomersFromGroupRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RemoveCustomersFromGroupCommand(id, request.CustomerIds)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomerGroupsManageMembers)
        .WithName("RemoveCustomersFromGroup")
        .WithSummary("Remove customers from a group")
        .WithDescription("Removes one or more customers from the specified group.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
