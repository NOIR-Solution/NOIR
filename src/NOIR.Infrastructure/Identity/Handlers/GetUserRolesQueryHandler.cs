namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting roles assigned to a user.
/// </summary>
public class GetUserRolesQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUserRolesQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetUserRolesQuery query, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(query.UserId);
        if (user is null)
        {
            return Result.Failure<IReadOnlyList<string>>(Error.NotFound("User", query.UserId));
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success<IReadOnlyList<string>>(roles.ToList());
    }
}
