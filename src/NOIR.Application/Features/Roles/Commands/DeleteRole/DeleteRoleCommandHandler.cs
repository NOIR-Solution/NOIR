namespace NOIR.Application.Features.Roles.Commands.DeleteRole;

/// <summary>
/// Wolverine handler for deleting a role.
/// Prevents deletion of system roles and roles with assigned users.
/// </summary>
public class DeleteRoleCommandHandler
{
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;

    public DeleteRoleCommandHandler(
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization)
    {
        _roleIdentityService = roleIdentityService;
        _localization = localization;
    }

    public async Task<Result<bool>> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        // Verify role exists
        var role = await _roleIdentityService.FindByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<bool>(
                Error.NotFound(_localization["roles.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        // Prevent deletion of system roles
        var systemRoles = new[] { NOIR.Domain.Common.Roles.Admin, NOIR.Domain.Common.Roles.User };
        if (systemRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<bool>(
                Error.Validation("role", _localization["roles.cannotDeleteSystemRole"], ErrorCodes.Business.CannotDelete));
        }

        // Check if role has users
        var userCount = await _roleIdentityService.GetUserCountAsync(command.RoleId, cancellationToken);
        if (userCount > 0)
        {
            return Result.Failure<bool>(
                Error.Validation(
                    "role",
                    string.Format(_localization["roles.hasAssignedUsers"], userCount),
                    ErrorCodes.Business.CannotDelete));
        }

        // Delete role
        var result = await _roleIdentityService.DeleteRoleAsync(command.RoleId, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.Failure<bool>(
                Error.ValidationErrors(result.Errors!, ErrorCodes.Validation.General));
        }

        return Result.Success(true);
    }
}
