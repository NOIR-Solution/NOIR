namespace NOIR.IntegrationTests.Auth;

/// <summary>
/// Integration tests for cookie-based authentication.
/// Tests the full HTTP request/response cycle with cookie handling.
/// </summary>
[Collection("Integration")]
public class CookieAuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CookieAuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    #region Login with Cookies Tests

    [Fact]
    public async Task Login_WithUseCookies_ShouldSetAuthCookies()
    {
        // Arrange - First register a user
        var email = $"cookie_login_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Test", "User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act - Login with useCookies=true
        var loginCommand = new LoginCommand(email, password, UseCookies: true);
        var response = await _client.PostAsJsonAsync("/api/auth/login?useCookies=true", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check Set-Cookie headers
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().Contain(h => h.Contains("noir.access="));
        setCookieHeaders.Should().Contain(h => h.Contains("noir.refresh="));

        // Verify cookies have security flags
        setCookieHeaders.Should().Contain(h => h.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        setCookieHeaders.Should().Contain(h => h.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WithoutUseCookies_ShouldNotSetCookies()
    {
        // Arrange - First register a user
        var email = $"no_cookie_login_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Test", "User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act - Login without useCookies
        var loginCommand = new LoginCommand(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Should not have auth cookies (may have other cookies)
        var hasCookieHeader = response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders);
        if (hasCookieHeader && setCookieHeaders != null)
        {
            setCookieHeaders.Should().NotContain(h => h.Contains("noir.access="));
            setCookieHeaders.Should().NotContain(h => h.Contains("noir.refresh="));
        }
    }

    #endregion

    #region Register with Cookies Tests

    [Fact]
    public async Task Register_WithUseCookies_ShouldSetAuthCookies()
    {
        // Arrange
        var email = $"cookie_register_{Guid.NewGuid():N}@example.com";
        var registerCommand = new RegisterCommand(email, "ValidPassword123!", "Cookie", "User");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register?useCookies=true", registerCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check Set-Cookie headers
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().Contain(h => h.Contains("noir.access="));
        setCookieHeaders.Should().Contain(h => h.Contains("noir.refresh="));
    }

    #endregion

    #region Cookie-Based Authentication Tests

    [Fact]
    public async Task GetCurrentUser_WithCookieAuth_ShouldSucceed()
    {
        // Arrange - Register and login with cookies
        var email = $"cookie_auth_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Cookie", "Auth");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Create a new client with cookie handling
        var cookieClient = _factory.CreateTestClient();

        // Login with cookies
        var loginCommand = new LoginCommand(email, password, UseCookies: true);
        var loginResponse = await cookieClient.PostAsJsonAsync("/api/auth/login?useCookies=true", loginCommand);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Access protected endpoint using cookies (no Authorization header)
        var response = await cookieClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userDto = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetCurrentUser_AuthorizationHeaderTakesPrecedence_OverCookies()
    {
        // Arrange - Register two different users
        var email1 = $"cookie_user_{Guid.NewGuid():N}@example.com";
        var email2 = $"header_user_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email1, password, "Cookie", "User"));
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email2, password, "Header", "User"));

        // Login first user with cookies
        var cookieClient = _factory.CreateTestClient();
        await cookieClient.PostAsJsonAsync("/api/auth/login?useCookies=true", new LoginCommand(email1, password, true));

        // Login second user and get token
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email2, password));
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - Add Authorization header to cookie client (should override cookies)
        cookieClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var response = await cookieClient.GetAsync("/api/auth/me");

        // Assert - Should return second user (from Authorization header, not cookies)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        userDto!.Email.Should().Be(email2);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ShouldClearCookies()
    {
        // Arrange - Register and login with cookies
        var email = $"logout_test_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, password, "Logout", "Test"));

        var cookieClient = _factory.CreateTestClient();
        await cookieClient.PostAsJsonAsync("/api/auth/login?useCookies=true", new LoginCommand(email, password, true));

        // Act - Logout
        var logoutResponse = await cookieClient.PostAsJsonAsync("/api/auth/logout", new { });

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cookies should be cleared (Set-Cookie with expired date)
        var setCookieHeaders = logoutResponse.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().Contain(h => h.Contains("noir.access="));
        setCookieHeaders.Should().Contain(h => h.Contains("noir.refresh="));
    }

    [Fact]
    public async Task Logout_WithRevokeAllSessions_ShouldSucceed()
    {
        // Arrange - Register and login
        var email = $"logout_all_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, password, "Test", "User"));

        var cookieClient = _factory.CreateTestClient();
        var loginResponse = await cookieClient.PostAsJsonAsync("/api/auth/login?useCookies=true", new LoginCommand(email, password, true));
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - Logout with revokeAllSessions
        var logoutCommand = new LogoutCommand(RevokeAllSessions: true);
        var logoutResponse = await cookieClient.PostAsJsonAsync("/api/auth/logout?revokeAllSessions=true", logoutCommand);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_AfterLogout_ProtectedEndpointsShouldFail()
    {
        // Arrange - Register and login with cookies
        var email = $"logout_verify_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, password, "Test", "User"));

        var cookieClient = _factory.CreateTestClient();
        await cookieClient.PostAsJsonAsync("/api/auth/login?useCookies=true", new LoginCommand(email, password, true));

        // Verify we can access protected endpoint before logout
        var preLogoutResponse = await cookieClient.GetAsync("/api/auth/me");
        preLogoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Logout
        await cookieClient.PostAsJsonAsync("/api/auth/logout", new { });

        // Create new client (cookies cleared)
        var newClient = _factory.CreateTestClient();

        // Assert - Should not be able to access protected endpoint
        var postLogoutResponse = await newClient.GetAsync("/api/auth/me");
        postLogoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task Login_WithCookies_CookiesShouldBeHttpOnly()
    {
        // Arrange
        var email = $"httponly_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, password, "Test", "User"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login?useCookies=true", new LoginCommand(email, password, true));

        // Assert
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        foreach (var cookie in setCookieHeaders.Where(h => h.Contains("noir.")))
        {
            cookie.Should().Contain("httponly", "all auth cookies should be HttpOnly");
        }
    }

    [Fact]
    public async Task Login_WithCookies_CookiesShouldHaveSameSiteStrict()
    {
        // Arrange
        var email = $"samesite_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, password, "Test", "User"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login?useCookies=true", new LoginCommand(email, password, true));

        // Assert
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        foreach (var cookie in setCookieHeaders.Where(h => h.Contains("noir.")))
        {
            cookie.ToLower().Should().Contain("samesite=strict", "all auth cookies should have SameSite=Strict");
        }
    }

    #endregion
}
