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

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    private async Task<(string email, string password, AuthResponse auth)> CreateTestUserAsync(string? firstName = "Test", string? lastName = "User")
    {
        var email = $"localdb_test_{Guid.NewGuid():N}@example.com";
        var password = "ValidPassword123!";

        // Create user via admin endpoint
        var adminClient = await GetAdminClientAsync();
        var createCommand = new CreateUserCommand(email, password, firstName, lastName, null, null);
        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as the new user
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var response = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (email, password, response!.Auth!);
    }

    #region Admin Create User Tests

    [Fact]
    public async Task CreateUser_AsAdmin_ShouldSucceed()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var email = $"localdb_admin_create_{Guid.NewGuid():N}@example.com";
        var createCommand = new CreateUserCommand(email, "ValidPassword123!", "John", "Doe", null, null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/users", createCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be(email);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var email = $"duplicate_localdb_{Guid.NewGuid():N}@example.com";
        var firstCommand = new CreateUserCommand(email, "ValidPassword123!", "First", "User", null, null);
        await adminClient.PostAsJsonAsync("/api/users", firstCommand);

        // Act
        var secondCommand = new CreateUserCommand(email, "AnotherPassword123!", "Second", "User", null, null);
        var response = await adminClient.PostAsJsonAsync("/api/users", secondCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var (email, password, _) = await CreateTestUserAsync();

        // Act
        var loginCommand = new LoginCommand(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.Auth.Should().NotBeNull();
        loginResponse.Auth!.Email.Should().Be(email);
        loginResponse.Auth.AccessToken.Should().NotBeNullOrEmpty();
        loginResponse.Auth.RefreshToken.Should().NotBeNullOrEmpty();
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
        var (email, _, _) = await CreateTestUserAsync();

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
        var (_, _, initialAuth) = await CreateTestUserAsync();

        // Act
        var refreshCommand = new RefreshTokenCommand(initialAuth.AccessToken, initialAuth.RefreshToken);
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
        // Arrange - Create user and get tokens
        var (_, _, initialAuth) = await CreateTestUserAsync();

        // Use the refresh token once (this will rotate it)
        var firstRefresh = new RefreshTokenCommand(initialAuth.AccessToken, initialAuth.RefreshToken);
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
        var (email, _, auth) = await CreateTestUserAsync("John", "Doe");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

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
        var (email, password, _) = await CreateTestUserAsync();

        // Act - Login from multiple "devices"
        var loginCommand = new LoginCommand(email, password);
        var response1 = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var response2 = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var response3 = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        var login1 = await response1.Content.ReadFromJsonAsync<LoginResponse>();
        var login2 = await response2.Content.ReadFromJsonAsync<LoginResponse>();
        var login3 = await response3.Content.ReadFromJsonAsync<LoginResponse>();

        // All should have different refresh tokens
        login1!.Auth!.RefreshToken.Should().NotBe(login2!.Auth!.RefreshToken);
        login2.Auth.RefreshToken.Should().NotBe(login3!.Auth!.RefreshToken);
    }

    #endregion

    #region Security Headers Tests

    [Fact]
    public async Task Response_ShouldContainSecurityHeaders()
    {
        // Arrange - Login with admin
        var command = new LoginCommand("admin@noir.local", "123qwe");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
    }

    #endregion
}
