namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting a user's profile by ID.
/// Used internally for before-state fetching in audit logging.
/// </summary>
public class GetUserByIdQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUserByIdQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return Result.Failure<UserProfileDto>(Error.Validation("UserId", "User ID is required."));
        }

        var user = await _userManager.FindByIdAsync(query.UserId);
        if (user is null)
        {
            return Result.Failure<UserProfileDto>(Error.NotFound("User", query.UserId));
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
