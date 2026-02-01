namespace NOIR.Web.Endpoints;

/// <summary>
/// User management API endpoints (admin operations).
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // Get all users
        group.MapGet("/", async (
            [AsParameters] GetUsersQuery query,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<PaginatedList<UserListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersRead)
        .WithName("GetUsers")
        .WithSummary("Get paginated list of users")
        .Produces<PaginatedList<UserListDto>>(StatusCodes.Status200OK);

        // Create user
        group.MapPost("/", async (
            CreateUserCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<UserDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersCreate)
        .WithName("CreateUser")
        .WithSummary("Create a new user")
        .Produces<UserDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Get user by ID
        group.MapGet("/{userId}", async (string userId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<UserProfileDto>>(new GetUserByIdQuery(userId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersRead)
        .WithName("GetUserById")
        .WithSummary("Get user by ID")
        .Produces<UserProfileDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update user
        group.MapPut("/{userId}", async (
            string userId,
            UpdateUserCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var cmd = command with { TargetUserId = userId, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<UserDto>>(cmd);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersUpdate)
        .WithName("UpdateUser")
        .WithSummary("Update user details")
        .Produces<UserDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete user (soft delete)
        group.MapDelete("/{userId}", async (
            string userId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteUserCommand(userId) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersDelete)
        .WithName("DeleteUser")
        .WithSummary("Soft-delete a user (removes from system)")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Lock user
        group.MapPost("/{userId}/lock", async (
            string userId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new LockUserCommand(userId, Lock: true) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersUpdate)
        .WithName("LockUser")
        .WithSummary("Lock a user account (prevents login)")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Unlock user
        group.MapPost("/{userId}/unlock", async (
            string userId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new LockUserCommand(userId, Lock: false) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersUpdate)
        .WithName("UnlockUser")
        .WithSummary("Unlock a user account (allows login)")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get user roles
        group.MapGet("/{userId}/roles", async (string userId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<IReadOnlyList<string>>>(new GetUserRolesQuery(userId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersRead)
        .WithName("GetUserRoles")
        .WithSummary("Get roles assigned to a user")
        .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Assign roles to user
        group.MapPut("/{userId}/roles", async (
            string userId,
            AssignRolesToUserCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { TargetUserId = userId, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<UserDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersManageRoles)
        .WithName("AssignRolesToUser")
        .WithSummary("Assign roles to a user (replaces existing)")
        .Produces<UserDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get user effective permissions
        group.MapGet("/{userId}/permissions", async (string userId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<UserPermissionsDto>>(new GetUserPermissionsQuery(userId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.UsersRead)
        .WithName("GetUserPermissions")
        .WithSummary("Get effective permissions for a user")
        .Produces<UserPermissionsDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
