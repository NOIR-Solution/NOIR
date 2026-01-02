namespace NOIR.Web.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// Only allows access to authenticated users with Admin role.
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
        // In production, require Admin role
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
#endif
    }
}
