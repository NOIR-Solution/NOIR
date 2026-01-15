namespace NOIR.Application.Features.Roles.Queries.GetRoleById;

/// <summary>
/// Wolverine handler for getting a role by ID with its permissions.
/// </summary>
public class GetRoleByIdQueryHandler
{
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;

    public GetRoleByIdQueryHandler(
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization)
    {
        _roleIdentityService = roleIdentityService;
        _localization = localization;
    }

    public async Task<Result<RoleDto>> Handle(GetRoleByIdQuery query, CancellationToken cancellationToken)
    {
        // Find role
        var role = await _roleIdentityService.FindByIdAsync(query.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RoleDto>(
                Error.NotFound(_localization["roles.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        // Get parent role name if exists
        string? parentRoleName = null;
        if (!string.IsNullOrEmpty(role.ParentRoleId))
        {
            var parentRole = await _roleIdentityService.FindByIdAsync(role.ParentRoleId, cancellationToken);
            parentRoleName = parentRole?.Name;
        }

        // Get permissions, effective permissions, and user count
        var permissions = await _roleIdentityService.GetPermissionsAsync(query.RoleId, cancellationToken);
        var effectivePermissions = await _roleIdentityService.GetEffectivePermissionsAsync(query.RoleId, cancellationToken);
        var userCount = await _roleIdentityService.GetUserCountAsync(query.RoleId, cancellationToken);

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
            userCount,
            permissions,
            effectivePermissions);

        return Result.Success(roleDto);
    }
}
