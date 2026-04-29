using NOIR.Application.Features.Permissions.Queries.GetAllPermissions;
using NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for permission endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class PermissionEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PermissionEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

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
            RoleNames: null); // No roles - regular user without admin permissions

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as the created user
        var loginCommand = new LoginCommand(email, password);
        var loginResult = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();

        return (email, password, loginResponse!.Auth!);
    }

    #region Get All Permissions Tests

    [Fact]
    public async Task GetAllPermissions_AsAdmin_ShouldReturnPermissionsList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/permissions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<PermissionDto>>();
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetAllPermissions_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/permissions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllPermissions_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without roles read permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/permissions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllPermissions_ShouldContainExpectedPermissions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/permissions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<PermissionDto>>();
        result.ShouldNotBeNull();

        // Verify some expected permissions exist
        result.ShouldContain(p => p.Name == Permissions.UsersRead);
        result.ShouldContain(p => p.Name == Permissions.RolesRead);
        result.ShouldContain(p => p.Name == Permissions.TenantsRead);
    }

    [Fact]
    public async Task GetAllPermissions_ShouldIncludeMetadata()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/permissions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<PermissionDto>>();
        result.ShouldNotBeNull();

        var firstPermission = result!.First();
        firstPermission.Id.ShouldNotBeNullOrEmpty();
        firstPermission.Name.ShouldNotBeNullOrEmpty();
        firstPermission.Resource.ShouldNotBeNullOrEmpty();
        firstPermission.Action.ShouldNotBeNullOrEmpty();
        firstPermission.DisplayName.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Get Permission Templates Tests

    [Fact]
    public async Task GetPermissionTemplates_AsAdmin_ShouldReturnTemplatesList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/permission-templates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<PermissionTemplateDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPermissionTemplates_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/permission-templates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPermissionTemplates_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without roles read permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/permission-templates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPermissionTemplates_TemplatesShouldHavePermissions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/permission-templates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<PermissionTemplateDto>>();
        result.ShouldNotBeNull();

        // If there are templates, they should have required fields
        if (result!.Count > 0)
        {
            var firstTemplate = result.First();
            firstTemplate.Id.ShouldNotBe(Guid.Empty);
            firstTemplate.Name.ShouldNotBeNullOrEmpty();
            firstTemplate.Permissions.ShouldNotBeNull();
        }
    }

    #endregion

    #region Authorization Consistency Tests

    [Fact]
    public async Task PermissionEndpoints_ShouldRequireRolesReadPermission()
    {
        // Arrange - Admin should have RolesRead permission
        var adminClient = await GetAdminClientAsync();

        // Act
        var permissionsResponse = await adminClient.GetAsync("/api/permissions");
        var templatesResponse = await adminClient.GetAsync("/api/permission-templates");

        // Assert - Both should succeed for admin
        permissionsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        templatesResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PermissionEndpoints_RegularUser_ShouldBeForbidden()
    {
        // Arrange - Create a user without RolesRead permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var permissionsResponse = await userClient.GetAsync("/api/permissions");
        var templatesResponse = await userClient.GetAsync("/api/permission-templates");

        // Assert - Both should be forbidden for regular users
        permissionsResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        templatesResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion
}
