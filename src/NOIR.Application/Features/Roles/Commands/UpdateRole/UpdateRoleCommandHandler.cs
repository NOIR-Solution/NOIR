namespace NOIR.Application.Features.Roles.Commands.UpdateRole;

/// <summary>
/// Wolverine handler for updating a role's name.
/// </summary>
public class UpdateRoleCommandHandler
{
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;

    public UpdateRoleCommandHandler(
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization)
    {
        _roleIdentityService = roleIdentityService;
        _localization = localization;
    }

    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        // Verify role exists
        var existingRole = await _roleIdentityService.FindByIdAsync(command.RoleId, cancellationToken);
        if (existingRole is null)
        {
            return Result.Failure<RoleDto>(
                Error.NotFound(_localization["roles.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        // Check if new name conflicts with another role
        var conflictingRole = await _roleIdentityService.FindByNameAsync(command.Name, cancellationToken);
        if (conflictingRole is not null && conflictingRole.Id != command.RoleId)
        {
            return Result.Failure<RoleDto>(
                Error.Validation(
                    string.Format(_localization["roles.alreadyExists"], command.Name),
                    ErrorCodes.Business.AlreadyExists));
        }

        // Update role
        var result = await _roleIdentityService.UpdateRoleAsync(command.RoleId, command.Name, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.Failure<RoleDto>(
                Error.ValidationErrors(result.Errors!, ErrorCodes.Validation.General));
        }

        // Get updated role with permissions
        var updatedRole = await _roleIdentityService.FindByIdAsync(command.RoleId, cancellationToken);
        if (updatedRole is null)
        {
            return Result.Failure<RoleDto>(
                Error.Failure(ErrorCodes.System.UnknownError, "Failed to retrieve updated role"));
        }

        var permissions = await _roleIdentityService.GetPermissionsAsync(command.RoleId, cancellationToken);
        var userCount = await _roleIdentityService.GetUserCountAsync(command.RoleId, cancellationToken);

        var roleDto = new RoleDto(
            updatedRole.Id,
            updatedRole.Name,
            updatedRole.NormalizedName,
            userCount,
            permissions);

        return Result.Success(roleDto);
    }
}
