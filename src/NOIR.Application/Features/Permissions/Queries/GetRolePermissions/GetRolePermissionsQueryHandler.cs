namespace NOIR.Application.Features.Permissions.Queries.GetRolePermissions;

/// <summary>
/// Wolverine handler for getting all permissions assigned to a role.
/// </summary>
public class GetRolePermissionsQueryHandler
{
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;

    public GetRolePermissionsQueryHandler(
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization)
    {
        _roleIdentityService = roleIdentityService;
        _localization = localization;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetRolePermissionsQuery query, CancellationToken cancellationToken)
    {
        // Verify role exists
        var role = await _roleIdentityService.FindByIdAsync(query.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.NotFound(_localization["roles.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        var permissions = await _roleIdentityService.GetPermissionsAsync(query.RoleId, cancellationToken);

        return Result.Success(permissions);
    }
}
