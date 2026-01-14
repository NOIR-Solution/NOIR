namespace NOIR.Application.Features.Auth.Queries.GetUserById;

/// <summary>
/// Wolverine handler for getting a user's profile by ID.
/// Used internally for before-state fetching in audit logging.
/// Uses IUserIdentityService for user lookup operations.
/// </summary>
public class GetUserByIdQueryHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public GetUserByIdQueryHandler(
        IUserIdentityService userIdentityService,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return Result.Failure<UserProfileDto>(
                Error.Validation("UserId", _localization["validation.userId.required"], ErrorCodes.Validation.Required));
        }

        var user = await _userIdentityService.FindByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserProfileDto>(
                Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        var roles = await _userIdentityService.GetRolesAsync(query.UserId, cancellationToken);

        var dto = new UserProfileDto(
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
            user.CreatedAt,
            user.ModifiedAt);

        return Result.Success(dto);
    }
}
