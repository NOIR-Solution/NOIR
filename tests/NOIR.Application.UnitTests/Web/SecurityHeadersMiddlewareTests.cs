namespace NOIR.Application.UnitTests.Web;

/// <summary>
/// Unit tests for SecurityHeadersMiddleware.
/// Tests security header injection on HTTP responses.
/// </summary>
public class SecurityHeadersMiddlewareTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        return context;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptRequestDelegate()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Act
        var middleware = new SecurityHeadersMiddleware(next);

        // Assert
        middleware.ShouldNotBeNull();
    }

    #endregion

    #region InvokeAsync Tests

    [Fact]
    public async Task InvokeAsync_ShouldAddXFrameOptionsHeader()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Frame-Options"].ToString().ShouldBe("DENY");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddXContentTypeOptionsHeader()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddXXssProtectionHeader()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-XSS-Protection"].ToString().ShouldBe("1; mode=block");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddReferrerPolicyHeader()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["Referrer-Policy"].ToString().ShouldBe("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddPermissionsPolicyHeader()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["Permissions-Policy"].ToString().ShouldContain("accelerometer=()");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddContentSecurityPolicyHeader()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["Content-Security-Policy"].ToString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ForApiEndpoint_ShouldUseStrictCsp()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();
        context.Request.Path = "/api/auth/login";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - API endpoints get strictest CSP
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldContain("default-src 'none'");
        csp.ShouldContain("frame-ancestors 'none'");
        csp.ShouldNotContain("cdn.jsdelivr.net");
    }

    [Fact]
    public async Task InvokeAsync_ForScalarDocs_ShouldAllowCdn()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();
        context.Request.Path = "/api/docs";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Scalar docs allow CDN for scripts
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldContain("cdn.jsdelivr.net");
        csp.ShouldContain("fonts.googleapis.com");
    }

    [Fact]
    public async Task InvokeAsync_ForOpenApiSpec_ShouldAllowCdn()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();
        context.Request.Path = "/api/openapi/v1.json";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - OpenAPI spec route uses Scalar CSP
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldContain("cdn.jsdelivr.net");
    }

    [Fact]
    public async Task InvokeAsync_ForSpaRoute_ShouldUseSpaCsp()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();
        context.Request.Path = "/dashboard";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - SPA routes get default CSP
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldContain("default-src 'self'");
        csp.ShouldContain("script-src 'self'");
        csp.ShouldNotContain("cdn.jsdelivr.net");
    }

    [Fact]
    public async Task InvokeAsync_ForRootPath_ShouldUseSpaCsp()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();
        context.Request.Path = "/";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Root uses SPA CSP
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldContain("default-src 'self'");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBe(true);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddAllSecurityHeaders()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SecurityHeadersMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - verify all expected headers are present
        context.Response.Headers.ShouldContainKey("X-Frame-Options");
        context.Response.Headers.ShouldContainKey("X-Content-Type-Options");
        context.Response.Headers.ShouldContainKey("X-XSS-Protection");
        context.Response.Headers.ShouldContainKey("Referrer-Policy");
        context.Response.Headers.ShouldContainKey("Permissions-Policy");
        context.Response.Headers.ShouldContainKey("Content-Security-Policy");
    }

    #endregion
}
