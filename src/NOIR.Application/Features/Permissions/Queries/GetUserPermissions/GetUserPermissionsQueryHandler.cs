namespace NOIR.Application.Features.Permissions.Queries.GetUserPermissions;

/// <summary>
/// Wolverine handler for getting effective permissions for a user.
/// Aggregates permissions from all roles assigned to the user.
/// </summary>
public class GetUserPermissionsQueryHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;

    public GetUserPermissionsQueryHandler(
        IUserIdentityService userIdentityService,
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _roleIdentityService = roleIdentityService;
        _localization = localization;
    }

    public async Task<Result<UserPermissionsDto>> Handle(GetUserPermissionsQuery query, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _userIdentityService.FindByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserPermissionsDto>(
                Error.NotFound(_localization["users.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Get user's roles
        var roles = await _userIdentityService.GetRolesAsync(query.UserId, cancellationToken);

        // Aggregate permissions from all roles
        var allPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roles)
        {
            var role = await _roleIdentityService.FindByNameAsync(roleName, cancellationToken);
            if (role is not null)
            {
                var rolePermissions = await _roleIdentityService.GetPermissionsAsync(role.Id, cancellationToken);
                foreach (var permission in rolePermissions)
                {
                    allPermissions.Add(permission);
                }
            }
        }

        var userPermissionsDto = new UserPermissionsDto(
            query.UserId,
            user.Email,
            roles,
            allPermissions.OrderBy(p => p).ToList());

        return Result.Success(userPermissionsDto);
    }
}
