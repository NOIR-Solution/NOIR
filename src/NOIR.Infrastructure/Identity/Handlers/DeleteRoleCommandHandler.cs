namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for deleting a role.
/// </summary>
public class DeleteRoleCommandHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;

    public DeleteRoleCommandHandler(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext,
        IPermissionCacheInvalidator cacheInvalidator)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<bool>> Handle(DeleteRoleCommand command, CancellationToken ct)
    {
        var role = await _roleManager.FindByIdAsync(command.RoleId);
        if (role is null)
        {
            return Result.Failure<bool>(Error.NotFound("Role", command.RoleId));
        }

        // Prevent deletion of built-in roles
        if (role.Name == Roles.Admin || role.Name == Roles.User)
        {
            return Result.Failure<bool>(Error.Validation("RoleId", $"Cannot delete built-in role '{role.Name}'"));
        }

        // Check if any users are assigned to this role
        var hasUsers = await _dbContext.UserRoles.AnyAsync(ur => ur.RoleId == role.Id, ct);
        if (hasUsers)
        {
            return Result.Failure<bool>(Error.Conflict($"Cannot delete role '{role.Name}' because it has assigned users"));
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<bool>(Error.Failure("Role.DeleteFailed", $"Failed to delete role: {errors}"));
        }

        _cacheInvalidator.InvalidateAll();

        return Result.Success(true);
    }
}
