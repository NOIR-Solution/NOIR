namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting the current user's profile.
/// </summary>
public class GetCurrentUserQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        // Check if user is authenticated
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<CurrentUserDto>(Error.Unauthorized("User is not authenticated."));
        }

        // Find user
        var user = await _userManager.FindByIdAsync(_currentUser.UserId);
        if (user is null)
        {
            return Result.Failure<CurrentUserDto>(Error.NotFound("User", _currentUser.UserId));
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
