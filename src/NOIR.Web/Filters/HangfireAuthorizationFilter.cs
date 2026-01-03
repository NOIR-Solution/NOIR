namespace NOIR.Web.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// Requires authenticated users with the system:hangfire permission.
/// Redirects unauthenticated users to the login page.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Check if user is authenticated
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            // Redirect to login page with return URL
            var returnUrl = Uri.EscapeDataString("/hangfire");
            httpContext.Response.Redirect($"/login?returnUrl={returnUrl}");
            return false;
        }

        // Require system:hangfire permission
        return httpContext.User.HasClaim(Permissions.ClaimType, Permissions.HangfireDashboard);
    }
}
