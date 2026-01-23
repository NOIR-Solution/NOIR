using NOIR.Application.Features.PlatformSettings.DTOs;
using NOIR.Web.Endpoints;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for platform settings endpoints.
/// Tests the full HTTP request/response cycle for /api/platform-settings.
/// These endpoints require platform-admin permissions (TenantId = null).
/// </summary>
[Collection("Integration")]
public class PlatformSettingsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PlatformSettingsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    /// <summary>
    /// Gets an HTTP client authenticated as the platform admin (TenantId = null).
    /// Platform admin has platform-settings:read and platform-settings:manage permissions.
    /// </summary>
    private async Task<HttpClient> GetPlatformAdminClientAsync()
    {
        var loginCommand = new LoginCommand("platform@noir.local", "Platform123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    /// <summary>
    /// Gets an HTTP client authenticated as the tenant admin (TenantId = default).
    /// Tenant admin does NOT have platform-settings permissions.
    /// </summary>
    private async Task<HttpClient> GetTenantAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    /// <summary>
    /// Creates a regular user without platform-settings permissions.
    /// </summary>
    private async Task<(string Email, string Password, AuthResponse Auth)> CreateTestUserAsync()
    {
        var adminClient = await GetTenantAdminClientAsync();
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";

        var createCommand = new CreateUserCommand(
            Email: email,
            Password: password,
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            RoleNames: null);

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        var loginCommand = new LoginCommand(email, password);
        var loginResult = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();

        return (email, password, loginResponse!.Auth!);
    }

    #region Get SMTP Settings Tests

    [Fact]
    public async Task GetSmtpSettings_AsPlatformAdmin_ShouldReturnSettings()
    {
        // Arrange
        var platformAdminClient = await GetPlatformAdminClientAsync();

        // Act
        var response = await platformAdminClient.GetAsync("/api/platform-settings/smtp");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<SmtpSettingsDto>();
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSmtpSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/platform-settings/smtp");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSmtpSettings_AsTenantAdmin_ShouldReturnForbidden()
    {
        // Arrange - Tenant admin doesn't have platform-settings permissions
        var tenantAdminClient = await GetTenantAdminClientAsync();

        // Act
        var response = await tenantAdminClient.GetAsync("/api/platform-settings/smtp");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSmtpSettings_AsRegularUser_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/platform-settings/smtp");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Update SMTP Settings Tests

    [Fact]
    public async Task UpdateSmtpSettings_AsPlatformAdmin_ValidRequest_ShouldReturnUpdatedSettings()
    {
        // Arrange
        var platformAdminClient = await GetPlatformAdminClientAsync();
        var request = new UpdateSmtpSettingsRequest(
            Host: "smtp.platform.com",
            Port: 465,
            Username: "platform-smtp-user",
            Password: "platform-smtp-password",
            FromEmail: "noreply@platform.com",
            FromName: "Platform System",
            UseSsl: true);

        // Act
        var response = await platformAdminClient.PutAsJsonAsync("/api/platform-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<SmtpSettingsDto>();
        settings.Should().NotBeNull();
        settings!.Host.Should().Be("smtp.platform.com");
        settings.Port.Should().Be(465);
        settings.Username.Should().Be("platform-smtp-user");
        settings.FromEmail.Should().Be("noreply@platform.com");
        settings.FromName.Should().Be("Platform System");
        settings.UseSsl.Should().BeTrue();
        settings.HasPassword.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSmtpSettings_NullPassword_ShouldKeepExistingPassword()
    {
        // Arrange
        var platformAdminClient = await GetPlatformAdminClientAsync();

        // First set a password
        var initialRequest = new UpdateSmtpSettingsRequest(
            Host: "smtp.test.com",
            Port: 587,
            Username: "user",
            Password: "initial-password",
            FromEmail: "test@test.com",
            FromName: "Test",
            UseSsl: true);
        await platformAdminClient.PutAsJsonAsync("/api/platform-settings/smtp", initialRequest);

        // Then update without password (null should keep existing)
        var updateRequest = new UpdateSmtpSettingsRequest(
            Host: "smtp.updated.com",
            Port: 587,
            Username: "user",
            Password: null,
            FromEmail: "updated@test.com",
            FromName: "Updated",
            UseSsl: true);

        // Act
        var response = await platformAdminClient.PutAsJsonAsync("/api/platform-settings/smtp", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<SmtpSettingsDto>();
        settings.Should().NotBeNull();
        settings!.Host.Should().Be("smtp.updated.com");
        settings.HasPassword.Should().BeTrue(); // Password should still be set
    }

    [Fact]
    public async Task UpdateSmtpSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateSmtpSettingsRequest(
            "smtp.test.com", 587, null, null, "test@test.com", "Test", true);

        // Act
        var response = await _client.PutAsJsonAsync("/api/platform-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSmtpSettings_AsTenantAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var tenantAdminClient = await GetTenantAdminClientAsync();
        var request = new UpdateSmtpSettingsRequest(
            "smtp.test.com", 587, null, null, "test@test.com", "Test", true);

        // Act
        var response = await tenantAdminClient.PutAsJsonAsync("/api/platform-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSmtpSettings_AsRegularUser_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = new UpdateSmtpSettingsRequest(
            "smtp.test.com", 587, null, null, "test@test.com", "Test", true);

        // Act
        var response = await userClient.PutAsJsonAsync("/api/platform-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSmtpSettings_EmptyHost_ShouldReturnBadRequest()
    {
        // Arrange
        var platformAdminClient = await GetPlatformAdminClientAsync();
        var request = new UpdateSmtpSettingsRequest(
            Host: "",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "test@test.com",
            FromName: "Test",
            UseSsl: true);

        // Act
        var response = await platformAdminClient.PutAsJsonAsync("/api/platform-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSmtpSettings_InvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var platformAdminClient = await GetPlatformAdminClientAsync();
        var request = new UpdateSmtpSettingsRequest(
            Host: "smtp.test.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "not-an-email",
            FromName: "Test",
            UseSsl: true);

        // Act
        var response = await platformAdminClient.PutAsJsonAsync("/api/platform-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Test SMTP Connection Tests

    [Fact]
    public async Task TestSmtpConnection_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new TestSmtpRequest("test@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/platform-settings/smtp/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestSmtpConnection_AsTenantAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var tenantAdminClient = await GetTenantAdminClientAsync();
        var request = new TestSmtpRequest("test@example.com");

        // Act
        var response = await tenantAdminClient.PostAsJsonAsync("/api/platform-settings/smtp/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestSmtpConnection_AsRegularUser_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = new TestSmtpRequest("test@example.com");

        // Act
        var response = await userClient.PostAsJsonAsync("/api/platform-settings/smtp/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestSmtpConnection_InvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var platformAdminClient = await GetPlatformAdminClientAsync();
        var request = new TestSmtpRequest("not-an-email");

        // Act
        var response = await platformAdminClient.PostAsJsonAsync("/api/platform-settings/smtp/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorization Boundary Tests

    [Fact]
    public async Task PlatformSettingsEndpoints_TenantAdminCannotAccess()
    {
        // Arrange - Tenant admin has many permissions but NOT platform-settings
        var tenantAdminClient = await GetTenantAdminClientAsync();

        // Act & Assert - All platform-settings endpoints should be forbidden
        var getResponse = await tenantAdminClient.GetAsync("/api/platform-settings/smtp");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var updateRequest = new UpdateSmtpSettingsRequest(
            "smtp.test.com", 587, null, null, "test@test.com", "Test", true);
        var updateResponse = await tenantAdminClient.PutAsJsonAsync("/api/platform-settings/smtp", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var testRequest = new TestSmtpRequest("test@example.com");
        var testResponse = await tenantAdminClient.PostAsJsonAsync("/api/platform-settings/smtp/test", testRequest);
        testResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformSettingsEndpoints_PlatformAdminCanAccessAll()
    {
        // Arrange
        var platformAdminClient = await GetPlatformAdminClientAsync();

        // Act & Assert - All endpoints should be accessible
        var getResponse = await platformAdminClient.GetAsync("/api/platform-settings/smtp");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateRequest = new UpdateSmtpSettingsRequest(
            Host: "smtp.platform-test.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "platform-test@example.com",
            FromName: "Platform Test",
            UseSsl: true);
        var updateResponse = await platformAdminClient.PutAsJsonAsync("/api/platform-settings/smtp", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
