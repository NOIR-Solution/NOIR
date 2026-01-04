namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting the current user's profile.
/// </summary>
public class GetCurrentUserQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public GetCurrentUserQueryHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        // Check if user is authenticated
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<CurrentUserDto>(Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Find user
        var user = await _userManager.FindByIdAsync(_currentUser.UserId);
        if (user is null)
        {
            return Result.Failure<CurrentUserDto>(Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Get roles
        var roles = await _userManager.GetRolesAsync(user);

        var userDto = new CurrentUserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.FullName,
            roles,
            user.TenantId,
            user.IsActive,
            user.CreatedAt);

        return Result.Success(userDto);
    }
}
