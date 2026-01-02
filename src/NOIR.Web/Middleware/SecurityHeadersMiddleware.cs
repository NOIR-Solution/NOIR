namespace NOIR.Web.Middleware;

/// <summary>
/// Middleware that adds security headers to all responses.
/// Applies path-specific Content-Security-Policy:
/// - API endpoints: Strict CSP (default-src 'none')
/// - Scalar docs: Allows CDN for scripts/styles
/// - Other routes: SPA-friendly CSP for future React
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    // CSP for pure API endpoints (JSON only) - most restrictive
    private const string ApiCsp =
        "default-src 'none'; " +
        "frame-ancestors 'none';";

    // CSP for Scalar API documentation - allows CDN resources
    private const string ScalarDocsCsp =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data: https://fonts.gstatic.com https://fonts.googleapis.com; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';";

    // CSP for SPA routes (future React frontend)
    private const string SpaCsp =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';";

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // XSS protection (legacy browsers)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer policy
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions policy
        context.Response.Headers.Append("Permissions-Policy",
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

        // Content Security Policy - path-specific
        var csp = GetCspForPath(context.Request.Path);
        context.Response.Headers.Append("Content-Security-Policy", csp);

        await _next(context);
    }

    private static string GetCspForPath(PathString path)
    {
        // Scalar API documentation
        if (path.StartsWithSegments("/api/docs") || path.StartsWithSegments("/api/openapi"))
        {
            return ScalarDocsCsp;
        }

        // Pure API endpoints - strictest CSP
        if (path.StartsWithSegments("/api"))
        {
            return ApiCsp;
        }

        // SPA routes (everything else)
        return SpaCsp;
    }
}

/// <summary>
/// Extension method for adding SecurityHeadersMiddleware to the pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
