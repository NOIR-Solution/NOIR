namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting roles assigned to a user.
/// </summary>
public class GetUserRolesQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILocalizationService _localization;

    public GetUserRolesQueryHandler(
        UserManager<ApplicationUser> userManager,
        ILocalizationService localization)
    {
        _userManager = userManager;
        _localization = localization;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetUserRolesQuery query, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(query.UserId);
        if (user is null)
        {
            return Result.Failure<IReadOnlyList<string>>(Error.NotFound(_localization["auth.user.notFound"]));
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success<IReadOnlyList<string>>(roles.ToList());
    }
}
