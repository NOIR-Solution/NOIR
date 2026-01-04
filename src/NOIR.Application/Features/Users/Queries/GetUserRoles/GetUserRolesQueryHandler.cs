namespace NOIR.Application.Features.Users.Queries.GetUserRoles;

/// <summary>
/// Wolverine handler for getting roles assigned to a user.
/// </summary>
public class GetUserRolesQueryHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ILocalizationService _localization;

    public GetUserRolesQueryHandler(
        IUserIdentityService userIdentityService,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _localization = localization;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetUserRolesQuery query, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _userIdentityService.FindByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.NotFound(_localization["users.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        var roles = await _userIdentityService.GetRolesAsync(query.UserId, cancellationToken);

        return Result.Success(roles);
    }
}
