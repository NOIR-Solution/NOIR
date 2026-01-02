namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Authorization requirement for resource-based access.
/// </summary>
public class ResourcePermissionRequirement : IAuthorizationRequirement
{
    public string Action { get; }

    public ResourcePermissionRequirement(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        Action = action;
    }
}
