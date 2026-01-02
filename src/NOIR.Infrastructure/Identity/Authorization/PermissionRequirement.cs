namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Authorization requirement that demands a specific permission claim.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
