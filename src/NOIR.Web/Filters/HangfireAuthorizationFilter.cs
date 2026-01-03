namespace NOIR.Web.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// Requires authenticated users with the system:hangfire permission.
/// Redirects unauthenticated users to the login page.
/// In development, redirects to the React frontend dev server (port 3000).
/// In production, both frontend and backend run on the same port.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public HangfireAuthorizationFilter(IHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Check if user is authenticated
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Uri.EscapeDataString("/hangfire");

            // Development: Redirect to separate React dev server (http://localhost:3000)
            // Production: Redirect to relative path (frontend and backend are same origin)
            if (_environment.IsDevelopment())
            {
                var frontendUrl = _configuration["Spa:DevServerUrl"] ?? "http://localhost:3000";
                httpContext.Response.Redirect($"{frontendUrl}/login?returnUrl={returnUrl}");
            }
            else
            {
                httpContext.Response.Redirect($"/login?returnUrl={returnUrl}");
            }

            // CRITICAL: Return true to prevent Hangfire from overwriting our redirect with a 401
            // The redirect was already written to the response above
            return true;
        }

        // Require system:hangfire permission
        return httpContext.User.HasClaim(Permissions.ClaimType, Permissions.HangfireDashboard);
    }
}
