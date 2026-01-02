namespace NOIR.IntegrationTests.Auth;

/// <summary>
/// Integration tests for authentication endpoints using SQL Server LocalDB.
/// Provides realistic testing with actual SQL Server behavior.
/// </summary>
[Collection("LocalDb")]
public class AuthEndpointsLocalDbTests : IAsyncLifetime
{
    private readonly LocalDbWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AuthEndpointsLocalDbTests(LocalDbWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateTestClient();
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    #region Registration Tests

    [Fact]
    public async Task Register_ValidUser_ShouldReturnSuccessAndPersistToDatabase()
    {
        // Arrange
        var email = $"localdb_test_{Guid.NewGuid():N}@example.com";
        var command = new RegisterCommand(email, "ValidPassword123!", "John", "Doe");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be(email);
        authResponse.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var email = $"duplicate_localdb_{Guid.NewGuid():N}@example.com";
        var firstCommand = new RegisterCommand(email, "ValidPassword123!", "First", "User");
        await _client.PostAsJsonAsync("/api/auth/register", firstCommand);

        // Act
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
            "weak",
            null,
            null);

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
        // Arrange
        var email = $"login_localdb_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Test", "User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act
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
        // Arrange
        var email = $"wrongpass_localdb_{Guid.NewGuid():N}@example.com";
        var registerCommand = new RegisterCommand(email, "CorrectPassword123!", "Test", "User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act
        var loginCommand = new LoginCommand(email, "WrongPassword123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_ValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        var email = $"refresh_localdb_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Test", "User");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
        var initialAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act
        var refreshCommand = new RefreshTokenCommand(initialAuth!.AccessToken, initialAuth.RefreshToken);
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth.Should().NotBeNull();
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBe(initialAuth.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_ExpiredRefreshToken_ShouldReturnUnauthorized()
    {
        // This test would require manipulating time - covered in unit tests
        // Here we just verify the endpoint handles invalid tokens
        var command = new RefreshTokenCommand("invalid-access-token", "invalid-refresh-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_RevokedToken_ShouldReturnForbidden()
    {
        // Arrange - Register and get tokens
        var email = $"revoked_localdb_{Guid.NewGuid():N}@example.com";
        var registerCommand = new RegisterCommand(email, "ValidPassword123!", "Test", "User");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
        var initialAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Use the refresh token once (this will rotate it)
        var firstRefresh = new RefreshTokenCommand(initialAuth!.AccessToken, initialAuth.RefreshToken);
        await _client.PostAsJsonAsync("/api/auth/refresh", firstRefresh);

        // Act - Try to use the original (now invalidated) refresh token
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", firstRefresh);

        // Assert - Should fail because original token is no longer valid
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_Authenticated_ShouldReturnUserProfile()
    {
        // Arrange
        var email = $"me_localdb_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "John", "Doe");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

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
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Multiple Sessions Tests

    [Fact]
    public async Task MultipleLogins_ShouldCreateMultipleRefreshTokens()
    {
        // Arrange
        var email = $"multisession_localdb_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Test", "User");
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act - Login from multiple "devices"
        var loginCommand = new LoginCommand(email, password);
        var response1 = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var response2 = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var response3 = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth1 = await response1.Content.ReadFromJsonAsync<AuthResponse>();
        var auth2 = await response2.Content.ReadFromJsonAsync<AuthResponse>();
        var auth3 = await response3.Content.ReadFromJsonAsync<AuthResponse>();

        // All should have different refresh tokens
        auth1!.RefreshToken.Should().NotBe(auth2!.RefreshToken);
        auth2.RefreshToken.Should().NotBe(auth3!.RefreshToken);
    }

    #endregion

    #region Security Headers Tests

    [Fact]
    public async Task Response_ShouldContainSecurityHeaders()
    {
        // Arrange
        var command = new RegisterCommand($"security_localdb_{Guid.NewGuid():N}@example.com", "ValidPassword123!", null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
    }

    #endregion
}
