using NOIR.Application.Features.Tenants.Commands.CreateTenant;
using NOIR.Application.Features.Tenants.Commands.UpdateTenant;
using NOIR.Application.Features.Tenants.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for tenant management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class TenantEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TenantEndpointsTests(CustomWebApplicationFactory factory)
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
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (email, password, auth!);
    }

    private static CreateTenantCommand CreateTestTenantCommand()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreateTenantCommand(
            Identifier: $"test-tenant-{uniqueId}",
            Name: $"Test Tenant {uniqueId}",
            IsActive: true);
    }

    #region Get Tenants Tests

    [Fact]
    public async Task GetTenants_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<TenantListDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenants_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenants_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without tenant read permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTenants_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/tenants?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<TenantListDto>>();
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetTenants_WithSearch_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a tenant with a unique name
        var command = CreateTestTenantCommand();
        await adminClient.PostAsJsonAsync("/api/tenants", command);

        // Act
        var response = await adminClient.GetAsync($"/api/tenants?search={command.Name}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<TenantListDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().Contain(t => t.Name == command.Name);
    }

    #endregion

    #region Get Tenant By Id Tests

    [Fact]
    public async Task GetTenantById_ValidId_ShouldReturnTenant()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a tenant
        var command = CreateTestTenantCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/tenants", command);
        createResponse.EnsureSuccessStatusCode();
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/tenants/{createdTenant!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        tenant.Should().NotBeNull();
        tenant!.Id.Should().Be(createdTenant.Id);
        tenant.Name.Should().Be(command.Name);
    }

    [Fact]
    public async Task GetTenantById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/tenants/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTenantById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/tenants/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Create Tenant Tests

    [Fact]
    public async Task CreateTenant_ValidRequest_ShouldReturnCreatedTenant()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = CreateTestTenantCommand();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        tenant.Should().NotBeNull();
        tenant!.Identifier.Should().Be(command.Identifier);
        tenant.Name.Should().Be(command.Name);
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTenant_DuplicateIdentifier_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = CreateTestTenantCommand();

        // Create the first tenant
        await adminClient.PostAsJsonAsync("/api/tenants", command);

        // Act - Try to create with same identifier
        var response = await adminClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTenant_EmptyIdentifier_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new CreateTenantCommand(
            Identifier: "",
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTenant_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new CreateTenantCommand(
            Identifier: $"test-tenant-{Guid.NewGuid():N}",
            Name: "",
            IsActive: true);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTenant_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = CreateTestTenantCommand();

        // Act
        var response = await _client.PostAsJsonAsync("/api/tenants", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTenant_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without tenant create permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var command = CreateTestTenantCommand();

        // Act
        var response = await userClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Update Tenant Tests

    [Fact]
    public async Task UpdateTenant_ValidRequest_ShouldReturnUpdatedTenant()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a tenant
        var createCommand = CreateTestTenantCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/tenants", createCommand);
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantDto>();

        // Update the tenant
        var updateRequest = new UpdateTenantRequest(
            Identifier: createdTenant!.Identifier!,
            Name: "Updated Tenant Name",
            IsActive: false);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/tenants/{createdTenant.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        updatedTenant.Should().NotBeNull();
        updatedTenant!.Name.Should().Be("Updated Tenant Name");
        updatedTenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTenant_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateTenantRequest(
            Identifier: "test-identifier",
            Name: "Updated Name",
            IsActive: true);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/tenants/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTenant_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a tenant
        var createCommand = CreateTestTenantCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/tenants", createCommand);
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantDto>();

        // Update with empty name
        var updateRequest = new UpdateTenantRequest(
            Identifier: createdTenant!.Identifier!,
            Name: "",
            IsActive: true);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/tenants/{createdTenant.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTenant_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without tenant update permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var updateRequest = new UpdateTenantRequest(
            Identifier: "test-identifier",
            Name: "Updated Name",
            IsActive: true);

        // Act
        var response = await userClient.PutAsJsonAsync($"/api/tenants/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Delete Tenant Tests

    [Fact]
    public async Task DeleteTenant_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a tenant
        var command = CreateTestTenantCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/tenants", command);
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/tenants/{createdTenant!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's deleted (should return 404)
        var getResponse = await adminClient.GetAsync($"/api/tenants/{createdTenant.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTenant_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/tenants/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTenant_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/tenants/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteTenant_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without tenant delete permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.DeleteAsync($"/api/tenants/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task TenantEndpoints_ShouldRequireSpecificPermissions()
    {
        // Arrange - Create a user without tenant permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act & Assert - All operations should be forbidden
        var listResponse = await userClient.GetAsync("/api/tenants");
        listResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var getResponse = await userClient.GetAsync($"/api/tenants/{Guid.NewGuid()}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var createResponse = await userClient.PostAsJsonAsync("/api/tenants", CreateTestTenantCommand());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var updateRequest = new UpdateTenantRequest("test", "Test", true);
        var updateResponse = await userClient.PutAsJsonAsync($"/api/tenants/{Guid.NewGuid()}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var deleteResponse = await userClient.DeleteAsync($"/api/tenants/{Guid.NewGuid()}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
