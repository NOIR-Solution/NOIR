namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting a user's profile by ID.
/// Used internally for before-state fetching in audit logging.
/// </summary>
public class GetUserByIdQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILocalizationService _localization;

    public GetUserByIdQueryHandler(
        UserManager<ApplicationUser> userManager,
        ILocalizationService localization)
    {
        _userManager = userManager;
        _localization = localization;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return Result.Failure<UserProfileDto>(Error.Validation("UserId", _localization["validation.userId.required"]));
        }

        var user = await _userManager.FindByIdAsync(query.UserId);
        if (user is null)
        {
            return Result.Failure<UserProfileDto>(Error.NotFound(_localization["auth.user.notFound"]));
        }

        var roles = await _userManager.GetRolesAsync(user);

        var dto = new UserProfileDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.FullName,
            roles,
            user.TenantId,
            user.IsActive,
            user.CreatedAt,
            user.ModifiedAt);

        return Result.Success(dto);
    }
}
