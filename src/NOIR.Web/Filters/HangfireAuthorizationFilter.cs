namespace NOIR.Web.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// In production, requires authenticated users with the system:hangfire permission.
/// In development, allows unauthenticated access for easier debugging.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Contains conditional compilation (#if DEBUG) that cannot be unit tested")]
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In development, allow unauthenticated access
#if DEBUG
        return true;
#else
        // In production, require system:hangfire permission
        // This is more granular than role-based auth and consistent with the rest of the app
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.HasClaim(Permissions.ClaimType, Permissions.HangfireDashboard);
#endif
    }
}
