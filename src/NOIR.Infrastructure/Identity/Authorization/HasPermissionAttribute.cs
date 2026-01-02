namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Authorization attribute that requires a specific permission.
/// Usage: [HasPermission(Permissions.UsersRead)]
/// </summary>
public class HasPermissionAttribute : AuthorizeAttribute
{
    private const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permission)
        : base($"{PolicyPrefix}{permission}")
    {
    }
}
