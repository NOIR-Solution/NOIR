namespace NOIR.IntegrationTests;

/// <summary>
/// Integration tests for authentication endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<(string email, string password, AuthResponse auth)> CreateTestUserAsync()
    {
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";

        // Create user via admin endpoint
        var adminClient = await GetAdminClientAsync();
        var createCommand = new CreateUserCommand(email, password, "Test", "User", null, null);
        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as the new user
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (email, password, auth!);
    }

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange - Create a user via admin endpoint
        var (email, password, _) = await CreateTestUserAsync();

        // Act - Login with the credentials
        var loginCommand = new LoginCommand(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be(email);
        authResponse.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidEmail_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "anyPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WrongPassword_ShouldReturnUnauthorized()
    {
        // Arrange - Create a user via admin endpoint
        var (email, _, _) = await CreateTestUserAsync();

        // Act - Login with wrong password
        var loginCommand = new LoginCommand(email, "WrongPassword123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new LoginCommand("", "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_ValidTokens_ShouldReturnNewTokens()
    {
        // Arrange - Create user and get initial tokens
        var (_, _, initialAuth) = await CreateTestUserAsync();

        // Act - Refresh tokens
        var refreshCommand = new RefreshTokenCommand(
            initialAuth.AccessToken,
            initialAuth.RefreshToken);
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth.Should().NotBeNull();
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBeNullOrEmpty();
        // New tokens should be different
        newAuth.RefreshToken.Should().NotBe(initialAuth.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidAccessToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid-access-token", "any-refresh-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_EmptyTokens_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new RefreshTokenCommand("", "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_Authenticated_ShouldReturnUserProfile()
    {
        // Arrange - Create user and login
        var adminClient = await GetAdminClientAsync();
        var email = $"me_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var createCommand = new CreateUserCommand(email, password, "John", "Doe", null, null);
        await adminClient.PostAsJsonAsync("/api/users", createCommand);

        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Create authenticated client
        var authenticatedClient = _factory.CreateAuthenticatedClient(authResponse!.AccessToken);

        // Act
        var response = await authenticatedClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userDto = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be(email);
        userDto.FirstName.Should().Be("John");
        userDto.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task GetCurrentUser_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act - Call without authentication
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_InvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange - Create client with invalid token
        var invalidClient = _factory.CreateAuthenticatedClient("invalid.jwt.token");

        // Act
        var response = await invalidClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task Auth_ShouldReturnProperContentType()
    {
        // Arrange - Login with admin credentials
        var command = new LoginCommand("admin@noir.local", "123qwe");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Auth_ShouldNotLeakSensitiveInfo_OnInvalidLogin()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "wrongpassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Error message should be generic
        content.Should().Contain("Invalid email or password");
        content.Should().NotContain("does not exist");
        content.Should().NotContain("incorrect password");
    }

    #endregion
}
