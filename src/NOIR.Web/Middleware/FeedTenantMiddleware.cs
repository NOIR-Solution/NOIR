namespace NOIR.Web.Middleware;

/// <summary>
/// Middleware that sets the default tenant context for public feed routes (RSS, Sitemap).
/// Must run BEFORE Finbuckle's MultiTenantMiddleware so the header strategy can pick up the tenant.
/// </summary>
public class FeedTenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FeedTenantMiddleware> _logger;

    // Routes that need default tenant context for anonymous access
    private static readonly string[] FeedRoutes =
    [
        "/rss.xml",
        "/blog/feed.xml",
        "/sitemap.xml"
    ];

    public FeedTenantMiddleware(RequestDelegate next, ILogger<FeedTenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IOptions<PlatformSettings> platformSettings)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Check if this is a feed route that needs default tenant
        if (IsFeedRoute(path) && !HasTenantHeader(context))
        {
            var defaultTenantId = platformSettings.Value.DefaultTenant.Identifier;

            _logger.LogInformation(
                "[FeedTenantMiddleware] Setting X-Tenant header to '{TenantId}' for feed route: {Path}",
                defaultTenantId, path);

            // Add the X-Tenant header so Finbuckle's header strategy picks it up
            context.Request.Headers["X-Tenant"] = defaultTenantId;
        }
        else if (IsFeedRoute(path))
        {
            _logger.LogInformation(
                "[FeedTenantMiddleware] Feed route {Path} already has X-Tenant header: {TenantId}",
                path, context.Request.Headers["X-Tenant"]);
        }

        await _next(context);
    }

    private static bool IsFeedRoute(string path)
    {
        return Array.Exists(FeedRoutes, route =>
            path.Equals(route, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasTenantHeader(HttpContext context)
    {
        return context.Request.Headers.ContainsKey("X-Tenant") &&
               !string.IsNullOrEmpty(context.Request.Headers["X-Tenant"]);
    }
}

/// <summary>
/// Extension methods for FeedTenantMiddleware registration.
/// </summary>
public static class FeedTenantMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that sets default tenant for feed routes.
    /// Must be called BEFORE UseMultiTenant().
    /// </summary>
    public static IApplicationBuilder UseFeedTenantResolver(this IApplicationBuilder app)
    {
        return app.UseMiddleware<FeedTenantMiddleware>();
    }
}
