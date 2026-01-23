using NOIR.Application.Features.TenantSettings.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for tenant settings endpoints.
/// Tests the full HTTP request/response cycle for /api/tenant-settings.
/// Covers branding, contact, regional, and SMTP settings.
/// </summary>
[Collection("Integration")]
public class TenantSettingsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TenantSettingsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    /// <summary>
    /// Gets an HTTP client authenticated as the tenant admin.
    /// Tenant admin has tenant-settings:read and tenant-settings:update permissions.
    /// </summary>
    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    /// <summary>
    /// Creates a regular user without tenant-settings permissions.
    /// </summary>
    private async Task<(string Email, string Password, AuthResponse Auth)> CreateTestUserAsync()
    {
        var adminClient = await GetAdminClientAsync();
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

    #region Branding Settings Tests

    [Fact]
    public async Task GetBrandingSettings_AsAdmin_ShouldReturnSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/tenant-settings/branding");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<BrandingSettingsDto>();
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBrandingSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant-settings/branding");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBrandingSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/tenant-settings/branding");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateBrandingSettings_ValidRequest_ShouldReturnUpdatedSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new UpdateBrandingSettingsRequest(
            LogoUrl: "https://example.com/logo.png",
            FaviconUrl: "https://example.com/favicon.ico",
            PrimaryColor: "#3498db",
            SecondaryColor: "#2ecc71",
            DarkModeDefault: true);

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/tenant-settings/branding", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<BrandingSettingsDto>();
        settings.Should().NotBeNull();
        settings!.LogoUrl.Should().Be("https://example.com/logo.png");
        settings.PrimaryColor.Should().Be("#3498db");
        settings.DarkModeDefault.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBrandingSettings_NullValues_ShouldClearSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new UpdateBrandingSettingsRequest(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/tenant-settings/branding", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<BrandingSettingsDto>();
        settings.Should().NotBeNull();
        settings!.DarkModeDefault.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBrandingSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateBrandingSettingsRequest(null, null, null, null, false);

        // Act
        var response = await _client.PutAsJsonAsync("/api/tenant-settings/branding", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateBrandingSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = new UpdateBrandingSettingsRequest(null, null, null, null, false);

        // Act
        var response = await userClient.PutAsJsonAsync("/api/tenant-settings/branding", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Contact Settings Tests

    [Fact]
    public async Task GetContactSettings_AsAdmin_ShouldReturnSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/tenant-settings/contact");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<ContactSettingsDto>();
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task GetContactSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant-settings/contact");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetContactSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/tenant-settings/contact");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateContactSettings_ValidRequest_ShouldReturnUpdatedSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new UpdateContactSettingsRequest(
            Email: "contact@example.com",
            Phone: "+1-555-0100",
            Address: "123 Main St, City, ST 12345");

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/tenant-settings/contact", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<ContactSettingsDto>();
        settings.Should().NotBeNull();
        settings!.Email.Should().Be("contact@example.com");
        settings.Phone.Should().Be("+1-555-0100");
        settings.Address.Should().Be("123 Main St, City, ST 12345");
    }

    [Fact]
    public async Task UpdateContactSettings_NullValues_ShouldClearSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new UpdateContactSettingsRequest(
            Email: null,
            Phone: null,
            Address: null);

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/tenant-settings/contact", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<ContactSettingsDto>();
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateContactSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateContactSettingsRequest(null, null, null);

        // Act
        var response = await _client.PutAsJsonAsync("/api/tenant-settings/contact", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateContactSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = new UpdateContactSettingsRequest(null, null, null);

        // Act
        var response = await userClient.PutAsJsonAsync("/api/tenant-settings/contact", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Regional Settings Tests

    [Fact]
    public async Task GetRegionalSettings_AsAdmin_ShouldReturnSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/tenant-settings/regional");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<RegionalSettingsDto>();
        settings.Should().NotBeNull();
        settings!.Timezone.Should().NotBeNullOrEmpty();
        settings.Language.Should().NotBeNullOrEmpty();
        settings.DateFormat.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRegionalSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant-settings/regional");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRegionalSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/tenant-settings/regional");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateRegionalSettings_ValidRequest_ShouldReturnUpdatedSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        // Note: Validator only accepts specific values:
        // Languages: en, vi, ja, ko, zh, fr, de, es, it, pt
        // DateFormats: YYYY-MM-DD, MM/DD/YYYY, DD/MM/YYYY, DD.MM.YYYY
        var request = new UpdateRegionalSettingsRequest(
            Timezone: "America/New_York",
            Language: "en",
            DateFormat: "MM/DD/YYYY");

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/tenant-settings/regional", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<RegionalSettingsDto>();
        settings.Should().NotBeNull();
        settings!.Timezone.Should().Be("America/New_York");
        settings.Language.Should().Be("en");
        settings.DateFormat.Should().Be("MM/DD/YYYY");
    }

    [Fact]
    public async Task UpdateRegionalSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateRegionalSettingsRequest("UTC", "en", "yyyy-MM-dd");

        // Act
        var response = await _client.PutAsJsonAsync("/api/tenant-settings/regional", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateRegionalSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = new UpdateRegionalSettingsRequest("UTC", "en", "yyyy-MM-dd");

        // Act
        var response = await userClient.PutAsJsonAsync("/api/tenant-settings/regional", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region SMTP Settings Tests

    [Fact]
    public async Task GetSmtpSettings_AsAdmin_ShouldReturnSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/tenant-settings/smtp");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<TenantSmtpSettingsDto>();
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSmtpSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant-settings/smtp");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSmtpSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/tenant-settings/smtp");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSmtpSettings_ValidRequest_ShouldReturnUpdatedSettings()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new UpdateTenantSmtpSettingsRequest(
            Host: "smtp.example.com",
            Port: 587,
            Username: "user@example.com",
            Password: "smtp-password",
            FromEmail: "noreply@example.com",
            FromName: "Test App",
            UseSsl: true);

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/tenant-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<TenantSmtpSettingsDto>();
        settings.Should().NotBeNull();
        settings!.Host.Should().Be("smtp.example.com");
        settings.Port.Should().Be(587);
        settings.FromEmail.Should().Be("noreply@example.com");
        settings.FromName.Should().Be("Test App");
        settings.UseSsl.Should().BeTrue();
        settings.IsInherited.Should().BeFalse(); // Tenant-specific now
    }

    [Fact]
    public async Task UpdateSmtpSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateTenantSmtpSettingsRequest(
            "smtp.test.com", 587, null, null, "test@test.com", "Test", true);

        // Act
        var response = await _client.PutAsJsonAsync("/api/tenant-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSmtpSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = new UpdateTenantSmtpSettingsRequest(
            "smtp.test.com", 587, null, null, "test@test.com", "Test", true);

        // Act
        var response = await userClient.PutAsJsonAsync("/api/tenant-settings/smtp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevertSmtpSettings_AsAdmin_ShouldRevertToDefaults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync("/api/tenant-settings/smtp/revert", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<TenantSmtpSettingsDto>();
        settings.Should().NotBeNull();
        settings!.IsInherited.Should().BeTrue(); // Should be using platform defaults now
    }

    [Fact]
    public async Task RevertSmtpSettings_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/tenant-settings/smtp/revert", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevertSmtpSettings_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.PostAsync("/api/tenant-settings/smtp/revert", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestSmtpConnection_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new TestTenantSmtpRequest("test@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/tenant-settings/smtp/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestSmtpConnection_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = new TestTenantSmtpRequest("test@example.com");

        // Act
        var response = await userClient.PostAsJsonAsync("/api/tenant-settings/smtp/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Comprehensive Authorization Tests

    [Fact]
    public async Task AllReadEndpoints_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act & Assert - All read endpoints should be forbidden
        var brandingResponse = await userClient.GetAsync("/api/tenant-settings/branding");
        brandingResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var contactResponse = await userClient.GetAsync("/api/tenant-settings/contact");
        contactResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var regionalResponse = await userClient.GetAsync("/api/tenant-settings/regional");
        regionalResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var smtpResponse = await userClient.GetAsync("/api/tenant-settings/smtp");
        smtpResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AllUpdateEndpoints_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act & Assert - All update endpoints should be forbidden
        var brandingRequest = new UpdateBrandingSettingsRequest(null, null, null, null, false);
        var brandingResponse = await userClient.PutAsJsonAsync("/api/tenant-settings/branding", brandingRequest);
        brandingResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var contactRequest = new UpdateContactSettingsRequest(null, null, null);
        var contactResponse = await userClient.PutAsJsonAsync("/api/tenant-settings/contact", contactRequest);
        contactResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var regionalRequest = new UpdateRegionalSettingsRequest("UTC", "en", "yyyy-MM-dd");
        var regionalResponse = await userClient.PutAsJsonAsync("/api/tenant-settings/regional", regionalRequest);
        regionalResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var smtpRequest = new UpdateTenantSmtpSettingsRequest(
            "smtp.test.com", 587, null, null, "test@test.com", "Test", true);
        var smtpResponse = await userClient.PutAsJsonAsync("/api/tenant-settings/smtp", smtpRequest);
        smtpResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
