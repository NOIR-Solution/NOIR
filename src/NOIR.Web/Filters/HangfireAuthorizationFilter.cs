namespace NOIR.Web.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// Requires authenticated users with the system:hangfire permission.
/// Redirects unauthenticated users to the login page.
/// In development, redirects to the React frontend dev server (port 3000).
/// In production, both frontend and backend run on the same port.
///
/// Note: Unlike our Minimal API endpoints which use .RequireAuthorization(),
/// this filter must manually invoke IAuthorizationService because Hangfire's
/// IDashboardAuthorizationFilter interface runs outside the ASP.NET Core
/// authorization middleware pipeline.
///
/// Both approaches leverage the same PermissionAuthorizationHandler, ensuring
/// consistent permission checking against the database with caching.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public HangfireAuthorizationFilter(
        IHostEnvironment environment,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _environment = environment;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
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

        // Use the authorization service to check permissions from the database
        // This leverages PermissionAuthorizationHandler which queries role claims from ASP.NET Identity
        using var scope = _serviceProvider.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // ConfigureAwait(false) is critical here to avoid deadlocks.
        // Hangfire's IDashboardAuthorizationFilter.Authorize() is synchronous, but we need to call
        // the async IAuthorizationService. Without ConfigureAwait(false), the continuation could
        // try to resume on the blocked thread, causing a deadlock under load.
        var result = authorizationService
            .AuthorizeAsync(httpContext.User, Permissions.HangfireDashboard)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        return result.Succeeded;
    }
}
