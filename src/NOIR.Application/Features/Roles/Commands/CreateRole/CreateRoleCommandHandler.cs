using DomainPermissions = NOIR.Domain.Common.Permissions;

namespace NOIR.Application.Features.Roles.Commands.CreateRole;

/// <summary>
/// Wolverine handler for creating a new role.
/// Optionally assigns permissions to the new role.
/// Validates that permissions are appropriate for the role's tenant scope.
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
        // Validate permission scope for tenant-specific roles
        if (command.Permissions?.Any() == true)
        {
            var invalidPermissions = DomainPermissions.Scopes.ValidateForTenant(command.Permissions, command.TenantId);
            if (invalidPermissions.Count > 0)
            {
                var invalidList = string.Join(", ", invalidPermissions);
                return Result.Failure<RoleDto>(
                    Error.Validation(
                        _localization.Get("validation.permissions.invalidForTenant", invalidList),
                        ErrorCodes.Validation.General));
            }
        }

        // Check if role already exists
        var existingRole = await _roleIdentityService.FindByNameAsync(command.Name, cancellationToken);
        if (existingRole is not null)
        {
            return Result.Failure<RoleDto>(
                Error.Conflict(
                    string.Format(_localization["roles.alreadyExists"], command.Name),
                    ErrorCodes.Business.AlreadyExists));
        }

        // Validate parent role if specified
        string? parentRoleName = null;
        if (!string.IsNullOrEmpty(command.ParentRoleId))
        {
            var parentRole = await _roleIdentityService.FindByIdAsync(command.ParentRoleId, cancellationToken);
            if (parentRole is null)
            {
                return Result.Failure<RoleDto>(
                    Error.NotFound(
                        _localization["roles.parentNotFound"],
                        ErrorCodes.Auth.RoleNotFound));
            }
            parentRoleName = parentRole.Name;
        }

        // Create role with all properties
        var createResult = await _roleIdentityService.CreateRoleAsync(
            command.Name,
            command.Description,
            command.ParentRoleId,
            command.TenantId,
            isSystemRole: false, // User-created roles are not system roles
            command.SortOrder,
            command.IconName,
            command.Color,
            cancellationToken);

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

        // Get permissions and effective permissions
        var permissions = await _roleIdentityService.GetPermissionsAsync(role.Id, cancellationToken);
        var effectivePermissions = await _roleIdentityService.GetEffectivePermissionsAsync(role.Id, cancellationToken);

        var roleDto = new RoleDto(
            role.Id,
            role.Name,
            role.NormalizedName,
            role.Description,
            role.ParentRoleId,
            parentRoleName,
            role.TenantId,
            role.IsSystemRole,
            role.SortOrder,
            role.IconName,
            role.Color,
            0, // New role has no users
            permissions,
            effectivePermissions);

        return Result.Success(roleDto);
    }
}
