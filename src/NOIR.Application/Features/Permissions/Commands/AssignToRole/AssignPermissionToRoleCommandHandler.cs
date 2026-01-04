namespace NOIR.Application.Features.Permissions.Commands.AssignToRole;

/// <summary>
/// Wolverine handler for assigning permissions to a role.
/// Adds permissions to the role's existing permissions.
/// </summary>
public class AssignPermissionToRoleCommandHandler
{
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;

    public AssignPermissionToRoleCommandHandler(
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization)
    {
        _roleIdentityService = roleIdentityService;
        _localization = localization;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(AssignPermissionToRoleCommand command, CancellationToken cancellationToken)
    {
        // Verify role exists
        var role = await _roleIdentityService.FindByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.NotFound(_localization["roles.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        // Add permissions
        var result = await _roleIdentityService.AddPermissionsAsync(
            command.RoleId,
            command.Permissions,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.ValidationErrors(result.Errors!, ErrorCodes.Validation.General));
        }

        // Return updated permissions
        var updatedPermissions = await _roleIdentityService.GetPermissionsAsync(command.RoleId, cancellationToken);

        return Result.Success(updatedPermissions);
    }
}
