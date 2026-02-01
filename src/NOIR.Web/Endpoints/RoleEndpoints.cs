namespace NOIR.Web.Endpoints;

/// <summary>
/// Role management API endpoints.
/// </summary>
public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags("Roles")
            .RequireAuthorization();

        // Get all roles
        group.MapGet("/", async (
            [AsParameters] GetRolesQuery query,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<PaginatedList<RoleListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesRead)
        .WithName("GetRoles")
        .WithSummary("Get paginated list of roles")
        .Produces<PaginatedList<RoleListDto>>(StatusCodes.Status200OK);

        // Get role by ID
        group.MapGet("/{roleId}", async (string roleId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<RoleDto>>(new GetRoleByIdQuery(roleId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesRead)
        .WithName("GetRoleById")
        .WithSummary("Get role by ID with permissions")
        .Produces<RoleDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create role
        group.MapPost("/", async (
            CreateRoleCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<RoleDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesCreate)
        .WithName("CreateRole")
        .WithSummary("Create a new role")
        .Produces<RoleDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Update role
        group.MapPut("/{roleId}", async (
            string roleId,
            UpdateRoleCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { RoleId = roleId, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<RoleDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesUpdate)
        .WithName("UpdateRole")
        .WithSummary("Update role name")
        .Produces<RoleDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete role
        group.MapDelete("/{roleId}", async (
            string roleId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteRoleCommand(roleId) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesDelete)
        .WithName("DeleteRole")
        .WithSummary("Delete a role")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get role permissions (direct assignments only)
        group.MapGet("/{roleId}/permissions", async (string roleId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<IReadOnlyList<string>>>(new GetRolePermissionsQuery(roleId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesRead)
        .WithName("GetRolePermissions")
        .WithSummary("Get permissions directly assigned to a role")
        .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get effective permissions (including inherited from parent roles)
        group.MapGet("/{roleId}/effective-permissions", async (
            string roleId, 
            IRoleIdentityService roleService) =>
        {
            var role = await roleService.FindByIdAsync(roleId);
            if (role is null)
            {
                return Results.NotFound(new ProblemDetails { Title = "Role not found" });
            }
            var permissions = await roleService.GetEffectivePermissionsAsync(roleId);
            return Results.Ok(permissions);
        })
        .RequireAuthorization(Permissions.RolesRead)
        .WithName("GetEffectivePermissions")
        .WithSummary("Get effective permissions for a role (including inherited)")
        .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Assign permissions to role
        group.MapPut("/{roleId}/permissions", async (
            string roleId,
            AssignPermissionToRoleCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { RoleId = roleId, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<IReadOnlyList<string>>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesManagePermissions)
        .WithName("AssignPermissionsToRole")
        .WithSummary("Assign permissions to a role")
        .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Remove permissions from role
        group.MapDelete("/{roleId}/permissions", async (
            string roleId,
            [FromBody] RemovePermissionFromRoleCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { RoleId = roleId, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<IReadOnlyList<string>>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.RolesManagePermissions)
        .WithName("RemovePermissionsFromRole")
        .WithSummary("Remove permissions from a role")
        .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
