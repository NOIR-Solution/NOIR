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
        middleware.Should().NotBeNull();
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
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
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
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
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
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
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
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
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
        context.Response.Headers["Permissions-Policy"].ToString().Should().Contain("accelerometer=()");
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
        context.Response.Headers["Content-Security-Policy"].ToString().Should().NotBeNullOrEmpty();
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
        csp.Should().Contain("default-src 'none'");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().NotContain("cdn.jsdelivr.net");
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
        csp.Should().Contain("cdn.jsdelivr.net");
        csp.Should().Contain("fonts.googleapis.com");
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
        csp.Should().Contain("cdn.jsdelivr.net");
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
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self'");
        csp.Should().NotContain("cdn.jsdelivr.net");
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
        csp.Should().Contain("default-src 'self'");
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
        nextCalled.Should().BeTrue();
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
        context.Response.Headers.Should().ContainKey("X-Frame-Options");
        context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
        context.Response.Headers.Should().ContainKey("X-XSS-Protection");
        context.Response.Headers.Should().ContainKey("Referrer-Policy");
        context.Response.Headers.Should().ContainKey("Permissions-Policy");
        context.Response.Headers.Should().ContainKey("Content-Security-Policy");
    }

    #endregion
}
