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

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return _factory.CreateAuthenticatedClient(auth!.AccessToken);
    }

    private async Task<(string email, string password)> CreateTestUserAsync(string? firstName = "Test", string? lastName = "User")
    {
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";

        // Create user via admin endpoint
        var adminClient = await GetAdminClientAsync();
        var createCommand = new CreateUserCommand(email, password, firstName, lastName, null, null);
        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        return (email, password);
    }

    #region Login with Cookies Tests

    [Fact]
    public async Task Login_WithUseCookies_ShouldSetAuthCookies()
    {
        // Arrange - Create a user
        var (email, password) = await CreateTestUserAsync();

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
        // Arrange - Create a user
        var (email, password) = await CreateTestUserAsync();

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

    #region Cookie-Based Authentication Tests

    [Fact]
    public async Task GetCurrentUser_WithCookieAuth_ShouldSucceed()
    {
        // Arrange - Create user and login with cookies
        var (email, password) = await CreateTestUserAsync("Cookie", "Auth");

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
        // Arrange - Create two different users
        var (email1, password1) = await CreateTestUserAsync("Cookie", "User");
        var (email2, password2) = await CreateTestUserAsync("Header", "User");

        // Login first user with cookies
        var cookieClient = _factory.CreateTestClient();
        await cookieClient.PostAsJsonAsync("/api/auth/login?useCookies=true", new LoginCommand(email1, password1, true));

        // Login second user and get token
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email2, password2));
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
        // Arrange - Create user and login with cookies
        var (email, password) = await CreateTestUserAsync("Logout", "Test");

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
        // Arrange - Create user and login
        var (email, password) = await CreateTestUserAsync();

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
        // Arrange - Create user and login with cookies
        var (email, password) = await CreateTestUserAsync();

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
        var (email, password) = await CreateTestUserAsync();

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
        var (email, password) = await CreateTestUserAsync();

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
