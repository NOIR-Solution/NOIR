namespace NOIR.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Wolverine handler for getting the current user's profile.
/// Uses IUserIdentityService for user lookup operations.
/// </summary>
public class GetCurrentUserQueryHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public GetCurrentUserQueryHandler(
        IUserIdentityService userIdentityService,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        // Check if user is authenticated
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<CurrentUserDto>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Find user
        var user = await _userIdentityService.FindByIdAsync(_currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<CurrentUserDto>(
                Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Get roles
        var roles = await _userIdentityService.GetRolesAsync(_currentUser.UserId, cancellationToken);

        var userDto = new CurrentUserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.FullName,
            user.PhoneNumber,
            user.AvatarUrl,
            roles,
            _currentUser.TenantId,
            user.IsActive,
            user.CreatedAt);

        return Result.Success(userDto);
    }
}
