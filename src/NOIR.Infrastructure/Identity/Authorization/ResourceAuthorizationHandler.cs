namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Authorization handler for resource-based access control.
/// Works with ASP.NET Core's IAuthorizationService for resource-based checks.
/// </summary>
public class ResourceAuthorizationHandler : AuthorizationHandler<ResourcePermissionRequirement, IResource>
{
    private readonly IResourceAuthorizationService _authService;

    public ResourceAuthorizationHandler(IResourceAuthorizationService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourcePermissionRequirement requirement,
        IResource resource)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return; // Not authenticated
        }

        var isAuthorized = await _authService.AuthorizeAsync(
            userId,
            resource,
            requirement.Action);

        if (isAuthorized)
        {
            context.Succeed(requirement);
        }
    }
}
