namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for CookieAuthService.
/// Tests cookie handling for authentication tokens.
/// </summary>
public class CookieAuthServiceTests
{
    #region Test Setup

    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IHostEnvironment> _environmentMock;
    private readonly CookieSettings _cookieSettings;
    private readonly CookieAuthService _service;
    private readonly DefaultHttpContext _httpContext;

    public CookieAuthServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _environmentMock = new Mock<IHostEnvironment>();

        _cookieSettings = new CookieSettings
        {
            AccessTokenCookieName = "noir.access",
            RefreshTokenCookieName = "noir.refresh",
            SameSiteMode = "Strict",
            Path = "/",
            SecureInProduction = true
        };

        _httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");

        _service = new CookieAuthService(
            _httpContextAccessorMock.Object,
            Options.Create(_cookieSettings),
            _environmentMock.Object);
    }

    #endregion

    #region SetAuthCookies Tests

    [Fact]
    public void SetAuthCookies_ShouldSetAccessTokenCookie()
    {
        // Arrange
        var accessToken = "test-access-token";
        var refreshToken = "test-refresh-token";
        var accessExpiry = DateTimeOffset.UtcNow.AddHours(1);
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        _service.SetAuthCookies(accessToken, refreshToken, accessExpiry, refreshExpiry);

        // Assert
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        cookies.Should().Contain("noir.access=test-access-token");
    }

    [Fact]
    public void SetAuthCookies_ShouldSetRefreshTokenCookie()
    {
        // Arrange
        var accessToken = "test-access-token";
        var refreshToken = "test-refresh-token";
        var accessExpiry = DateTimeOffset.UtcNow.AddHours(1);
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        _service.SetAuthCookies(accessToken, refreshToken, accessExpiry, refreshExpiry);

        // Assert
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        cookies.Should().Contain("noir.refresh=test-refresh-token");
    }

    [Fact]
    public void SetAuthCookies_ShouldSetHttpOnlyFlag()
    {
        // Arrange
        var accessToken = "test-access-token";
        var refreshToken = "test-refresh-token";
        var accessExpiry = DateTimeOffset.UtcNow.AddHours(1);
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        _service.SetAuthCookies(accessToken, refreshToken, accessExpiry, refreshExpiry);

        // Assert
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        cookies.Should().Contain("httponly");
    }

    [Fact]
    public void SetAuthCookies_ShouldSetSameSiteStrict()
    {
        // Arrange
        var accessToken = "test-access-token";
        var refreshToken = "test-refresh-token";
        var accessExpiry = DateTimeOffset.UtcNow.AddHours(1);
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        _service.SetAuthCookies(accessToken, refreshToken, accessExpiry, refreshExpiry);

        // Assert
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        cookies.Should().Contain("samesite=strict");
    }

    [Fact]
    public void SetAuthCookies_WhenNoHttpContext_ShouldThrow()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = new CookieAuthService(
            _httpContextAccessorMock.Object,
            Options.Create(_cookieSettings),
            _environmentMock.Object);

        // Act
        var act = () => service.SetAuthCookies("token", "refresh", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("HttpContext is not available");
    }

    #endregion

    #region ClearAuthCookies Tests

    [Fact]
    public void ClearAuthCookies_ShouldDeleteAccessTokenCookie()
    {
        // Arrange & Act
        _service.ClearAuthCookies();

        // Assert
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        cookies.Should().Contain("noir.access=");
    }

    [Fact]
    public void ClearAuthCookies_ShouldDeleteRefreshTokenCookie()
    {
        // Arrange & Act
        _service.ClearAuthCookies();

        // Assert
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        cookies.Should().Contain("noir.refresh=");
    }

    [Fact]
    public void ClearAuthCookies_WhenNoHttpContext_ShouldNotThrow()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = new CookieAuthService(
            _httpContextAccessorMock.Object,
            Options.Create(_cookieSettings),
            _environmentMock.Object);

        // Act
        var act = () => service.ClearAuthCookies();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region GetRefreshTokenFromCookie Tests

    [Fact]
    public void GetRefreshTokenFromCookie_WhenTokenExists_ShouldReturnToken()
    {
        // Arrange
        _httpContext.Request.Headers["Cookie"] = "noir.refresh=test-refresh-token";

        // Act
        var token = _service.GetRefreshTokenFromCookie();

        // Assert
        token.Should().Be("test-refresh-token");
    }

    [Fact]
    public void GetRefreshTokenFromCookie_WhenTokenMissing_ShouldReturnNull()
    {
        // Arrange - no cookie set

        // Act
        var token = _service.GetRefreshTokenFromCookie();

        // Assert
        token.Should().BeNull();
    }

    [Fact]
    public void GetRefreshTokenFromCookie_WhenNoHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = new CookieAuthService(
            _httpContextAccessorMock.Object,
            Options.Create(_cookieSettings),
            _environmentMock.Object);

        // Act
        var token = service.GetRefreshTokenFromCookie();

        // Assert
        token.Should().BeNull();
    }

    #endregion

    #region GetAccessTokenFromCookie Tests

    [Fact]
    public void GetAccessTokenFromCookie_WhenTokenExists_ShouldReturnToken()
    {
        // Arrange
        _httpContext.Request.Headers["Cookie"] = "noir.access=test-access-token";

        // Act
        var token = _service.GetAccessTokenFromCookie();

        // Assert
        token.Should().Be("test-access-token");
    }

    [Fact]
    public void GetAccessTokenFromCookie_WhenTokenMissing_ShouldReturnNull()
    {
        // Arrange - no cookie set

        // Act
        var token = _service.GetAccessTokenFromCookie();

        // Assert
        token.Should().BeNull();
    }

    [Fact]
    public void GetAccessTokenFromCookie_WhenNoHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = new CookieAuthService(
            _httpContextAccessorMock.Object,
            Options.Create(_cookieSettings),
            _environmentMock.Object);

        // Act
        var token = service.GetAccessTokenFromCookie();

        // Assert
        token.Should().BeNull();
    }

    #endregion

    #region Production Environment Tests

    [Fact]
    public void SetAuthCookies_InProduction_ShouldSetSecureFlag()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var prodService = new CookieAuthService(
            _httpContextAccessorMock.Object,
            Options.Create(_cookieSettings),
            _environmentMock.Object);

        var accessToken = "test-access-token";
        var refreshToken = "test-refresh-token";
        var accessExpiry = DateTimeOffset.UtcNow.AddHours(1);
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        prodService.SetAuthCookies(accessToken, refreshToken, accessExpiry, refreshExpiry);

        // Assert
        var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
        cookies.Should().Contain("secure");
    }

    #endregion
}
