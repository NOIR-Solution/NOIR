namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for updating a role's name.
/// </summary>
public class UpdateRoleCommandHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;
    private readonly ILocalizationService _localization;

    public UpdateRoleCommandHandler(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext,
        IPermissionCacheInvalidator cacheInvalidator,
        ILocalizationService localization)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
        _cacheInvalidator = cacheInvalidator;
        _localization = localization;
    }

    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand command, CancellationToken ct)
    {
        var role = await _roleManager.FindByIdAsync(command.RoleId);
        if (role is null)
        {
            return Result.Failure<RoleDto>(
                Error.NotFound(_localization["auth.role.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        // Check if new name already exists (for a different role)
        var existingRole = await _roleManager.FindByNameAsync(command.Name);
        if (existingRole is not null && existingRole.Id != command.RoleId)
        {
            return Result.Failure<RoleDto>(
                Error.Conflict(_localization["auth.role.nameConflict"], ErrorCodes.Business.AlreadyExists));
        }

        role.Name = command.Name;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return Result.Failure<RoleDto>(Error.Failure(ErrorCodes.System.DatabaseError, _localization["auth.role.updateFailed"]));
        }

        // Get permissions for the role
        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();

        // Get user count
        var userCount = await _dbContext.UserRoles
            .CountAsync(ur => ur.RoleId == role.Id, ct);

        _cacheInvalidator.InvalidateAll();

        return Result.Success(new RoleDto(
            role.Id,
            role.Name!,
            role.NormalizedName,
            userCount,
            permissions));
    }
}
