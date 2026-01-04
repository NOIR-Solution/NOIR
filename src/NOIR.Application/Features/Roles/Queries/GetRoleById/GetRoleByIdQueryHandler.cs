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

        // Get permissions and user count
        var permissions = await _roleIdentityService.GetPermissionsAsync(query.RoleId, cancellationToken);
        var userCount = await _roleIdentityService.GetUserCountAsync(query.RoleId, cancellationToken);

        var roleDto = new RoleDto(
            role.Id,
            role.Name,
            role.NormalizedName,
            userCount,
            permissions);

        return Result.Success(roleDto);
    }
}
