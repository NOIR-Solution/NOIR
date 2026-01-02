namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for creating a new role with optional permissions.
/// </summary>
public class CreateRoleCommandHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;

    public CreateRoleCommandHandler(
        RoleManager<IdentityRole> roleManager,
        IPermissionCacheInvalidator cacheInvalidator)
    {
        _roleManager = roleManager;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand command, CancellationToken ct)
    {
        // Check if role already exists
        if (await _roleManager.RoleExistsAsync(command.Name))
        {
            return Result.Failure<RoleDto>(Error.Conflict($"Role '{command.Name}' already exists"));
        }

        var role = new IdentityRole(command.Name);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<RoleDto>(Error.Failure("Role.CreateFailed", $"Failed to create role: {errors}"));
        }

        // Assign permissions if provided
        var permissions = new List<string>();
        if (command.Permissions is { Count: > 0 })
        {
            foreach (var permission in command.Permissions)
            {
                var claim = new Claim(Permissions.ClaimType, permission);
                await _roleManager.AddClaimAsync(role, claim);
                permissions.Add(permission);
            }
        }

        _cacheInvalidator.InvalidateAll();

        return Result.Success(new RoleDto(
            role.Id,
            role.Name!,
            role.NormalizedName,
            0,
            permissions));
    }
}
