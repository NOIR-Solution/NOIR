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

    #region Registration Tests

    [Fact]
    public async Task Register_ValidUser_ShouldReturnSuccess()
    {
        // Arrange
        var command = new RegisterCommand(
            $"test_{Guid.NewGuid():N}@example.com",
            "ValidPassword123!",
            "John",
            "Doe");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be(command.Email);
        authResponse.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange - Register first user
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        var firstCommand = new RegisterCommand(email, "ValidPassword123!", "First", "User");
        await _client.PostAsJsonAsync("/api/auth/register", firstCommand);

        // Act - Try to register with same email
        var secondCommand = new RegisterCommand(email, "AnotherPassword123!", "Second", "User");
        var response = await _client.PostAsJsonAsync("/api/auth/register", secondCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new RegisterCommand("invalid-email", "ValidPassword123!", null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new RegisterCommand(
            $"test_{Guid.NewGuid():N}@example.com",
            "weak", // Too short
            null,
            null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_EmptyEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new RegisterCommand("", "ValidPassword123!", null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange - First register a user
        var email = $"login_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Test", "User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act - Login with registered credentials
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
        // Arrange - First register a user
        var email = $"wrongpass_{Guid.NewGuid():N}@example.com";
        var registerCommand = new RegisterCommand(email, "CorrectPassword123!", "Test", "User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

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
        // Arrange - Register and get initial tokens
        var email = $"refresh_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Test", "User");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
        var initialAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - Refresh tokens
        var refreshCommand = new RefreshTokenCommand(
            initialAuth!.AccessToken,
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
        // Arrange - Register and login
        var email = $"me_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "John", "Doe");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

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
        // Arrange
        var command = new RegisterCommand(
            $"content_{Guid.NewGuid():N}@example.com",
            "ValidPassword123!",
            null,
            null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

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
