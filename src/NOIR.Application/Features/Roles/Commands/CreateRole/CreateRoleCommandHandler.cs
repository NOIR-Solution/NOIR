namespace NOIR.Application.Features.Roles.Commands.CreateRole;

/// <summary>
/// Wolverine handler for creating a new role.
/// Optionally assigns permissions to the new role.
/// </summary>
public class CreateRoleCommandHandler
{
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;

    public CreateRoleCommandHandler(
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization)
    {
        _roleIdentityService = roleIdentityService;
        _localization = localization;
    }

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        // Check if role already exists
        var existingRole = await _roleIdentityService.FindByNameAsync(command.Name, cancellationToken);
        if (existingRole is not null)
        {
            return Result.Failure<RoleDto>(
                Error.Conflict(
                    string.Format(_localization["roles.alreadyExists"], command.Name),
                    ErrorCodes.Business.AlreadyExists));
        }

        // Create role
        var createResult = await _roleIdentityService.CreateRoleAsync(command.Name, cancellationToken);
        if (!createResult.Succeeded)
        {
            return Result.Failure<RoleDto>(
                Error.ValidationErrors(createResult.Errors!, ErrorCodes.Validation.General));
        }

        // Get the created role
        var role = await _roleIdentityService.FindByNameAsync(command.Name, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RoleDto>(
                Error.Failure(ErrorCodes.System.UnknownError, "Failed to retrieve created role"));
        }

        // Add permissions if provided
        if (command.Permissions?.Any() == true)
        {
            var addPermResult = await _roleIdentityService.AddPermissionsAsync(
                role.Id,
                command.Permissions,
                cancellationToken);

            if (!addPermResult.Succeeded)
            {
                return Result.Failure<RoleDto>(
                    Error.ValidationErrors(addPermResult.Errors!, ErrorCodes.Validation.General));
            }
        }

        // Get permissions
        var permissions = await _roleIdentityService.GetPermissionsAsync(role.Id, cancellationToken);

        var roleDto = new RoleDto(
            role.Id,
            role.Name,
            role.NormalizedName,
            0, // New role has no users
            permissions);

        return Result.Success(roleDto);
    }
}
