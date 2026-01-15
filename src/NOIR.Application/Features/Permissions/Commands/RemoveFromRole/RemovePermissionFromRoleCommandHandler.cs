namespace NOIR.Application.Features.Permissions.Commands.RemoveFromRole;

/// <summary>
/// Wolverine handler for removing permissions from a role.
/// </summary>
public class RemovePermissionFromRoleCommandHandler
{
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;

    public RemovePermissionFromRoleCommandHandler(
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization,
        IPermissionCacheInvalidator cacheInvalidator)
    {
        _roleIdentityService = roleIdentityService;
        _localization = localization;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(RemovePermissionFromRoleCommand command, CancellationToken cancellationToken)
    {
        // Verify role exists
        var role = await _roleIdentityService.FindByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.NotFound(_localization["roles.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        // Remove permissions
        var result = await _roleIdentityService.RemovePermissionsAsync(
            command.RoleId,
            command.Permissions,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.ValidationErrors(result.Errors!, ErrorCodes.Validation.General));
        }

        // Invalidate permission cache for all users in this role
        await _cacheInvalidator.InvalidateRoleAsync(role.Name);

        // Return updated permissions
        var updatedPermissions = await _roleIdentityService.GetPermissionsAsync(command.RoleId, cancellationToken);

        return Result.Success(updatedPermissions);
    }
}
