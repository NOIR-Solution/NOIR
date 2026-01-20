namespace NOIR.Web.Middleware;

using System.Security.Claims;
using NOIR.Infrastructure.Services;

/// <summary>
/// Middleware that loads the complete current user profile from the database on each request.
/// This ensures all user data (roles, display name, avatar, etc.) is fresh and consistent.
/// Data is cached in HttpContext.Items for the request lifetime.
/// </summary>
public class CurrentUserLoaderMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserLoaderMiddleware(RequestDelegate _next)
    {
        this._next = _next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUserIdentityService userIdentityService)
    {
        // Only load for authenticated users
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    // Load complete user profile from database
                    var user = await userIdentityService.FindByIdAsync(userId, context.RequestAborted);
                    if (user != null)
                    {
                        // Load roles
                        var roles = await userIdentityService.GetRolesAsync(userId, context.RequestAborted);

                        // Store complete user data for request lifetime
                        var userData = new CurrentUserData(
                            user.Id,
                            user.Email,
                            user.FirstName,
                            user.LastName,
                            user.DisplayName,
                            user.FullName,
                            user.AvatarUrl,
                            user.PhoneNumber,
                            roles,
                            user.TenantId,
                            user.IsActive);

                        context.Items[CurrentUserData.CacheKey] = userData;
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    var logger = context.RequestServices.GetService<ILogger<CurrentUserLoaderMiddleware>>();
                    logger?.LogError(ex, "Failed to load user data for user {UserId}", userId);
                }
            }
        }

        await _next(context);
    }
}
