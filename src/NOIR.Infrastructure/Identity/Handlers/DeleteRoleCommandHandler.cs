namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for deleting a role.
/// </summary>
public class DeleteRoleCommandHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;
    private readonly ILocalizationService _localization;

    public DeleteRoleCommandHandler(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext,
        IPermissionCacheInvalidator cacheInvalidator,
        ILocalizationService localization)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
        _cacheInvalidator = cacheInvalidator;
        _localization = localization;
    }

    public async Task<Result<bool>> Handle(DeleteRoleCommand command, CancellationToken ct)
    {
        var role = await _roleManager.FindByIdAsync(command.RoleId);
        if (role is null)
        {
            return Result.Failure<bool>(
                Error.NotFound(_localization["auth.role.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        // Prevent deletion of built-in roles
        if (role.Name == Roles.Admin || role.Name == Roles.User)
        {
            return Result.Failure<bool>(
                Error.Validation("RoleId", _localization["auth.role.deleteBuiltIn"], ErrorCodes.Business.CannotDelete));
        }

        // Check if any users are assigned to this role
        var hasUsers = await _dbContext.UserRoles.AnyAsync(ur => ur.RoleId == role.Id, ct);
        if (hasUsers)
        {
            return Result.Failure<bool>(
                Error.Conflict(_localization["auth.role.hasAssignedUsers"], ErrorCodes.Business.CannotDelete));
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            return Result.Failure<bool>(Error.Failure(ErrorCodes.System.DatabaseError, _localization["auth.role.deleteFailed"]));
        }

        _cacheInvalidator.InvalidateAll();

        return Result.Success(true);
    }
}
