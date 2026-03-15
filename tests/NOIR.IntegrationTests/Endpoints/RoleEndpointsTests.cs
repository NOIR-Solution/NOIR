using NOIR.Application.Features.Users.Commands.CreateUser;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for role management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class RoleEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RoleEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GetRoles Tests

    [Fact]
    public async Task GetRoles_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<RoleListDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeEmpty();
        result.Items.ShouldContain(r => r.Name == "Admin");
    }

    [Fact]
    public async Task GetRoles_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRoles_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/roles?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<RoleListDto>>();
        result.ShouldNotBeNull();
        result!.PageNumber.ShouldBe(1);
        result.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region GetRoleById Tests

    [Fact]
    public async Task GetRoleById_ValidId_ShouldReturnRole()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get the list to find a role ID
        var listResponse = await adminClient.GetAsync("/api/roles");
        var roles = await listResponse.Content.ReadFromJsonAsync<PaginatedList<RoleListDto>>();
        var roleId = roles!.Items.First().Id;

        // Act
        var response = await adminClient.GetAsync($"/api/roles/{roleId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var role = await response.Content.ReadFromJsonAsync<RoleDto>();
        role.ShouldNotBeNull();
        role!.Id.ShouldBe(roleId);
    }

    [Fact]
    public async Task GetRoleById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/roles/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region CreateRole Tests

    [Fact]
    public async Task CreateRole_ValidRequest_ShouldReturnCreatedRole()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var command = new CreateRoleCommand(roleName);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/roles", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var role = await response.Content.ReadFromJsonAsync<RoleDto>();
        role.ShouldNotBeNull();
        role!.Name.ShouldBe(roleName);
    }

    [Fact]
    public async Task CreateRole_WithPermissions_ShouldAssignPermissions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var permissions = new[] { Permissions.UsersRead, Permissions.RolesRead };
        var command = new CreateRoleCommand(roleName, Permissions: permissions);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/roles", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var role = await response.Content.ReadFromJsonAsync<RoleDto>();
        role.ShouldNotBeNull();
        role!.Permissions.ShouldBe(permissions);
    }

    [Fact]
    public async Task CreateRole_DuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var command = new CreateRoleCommand(roleName);

        // Create the role first
        await adminClient.PostAsJsonAsync("/api/roles", command);

        // Act - Try to create with same name
        var response = await adminClient.PostAsJsonAsync("/api/roles", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateRole_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new CreateRoleCommand("");

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/roles", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRole_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new CreateRoleCommand("NewRole");

        // Act
        var response = await _client.PostAsJsonAsync("/api/roles", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region UpdateRole Tests

    [Fact]
    public async Task UpdateRole_ValidRequest_ShouldReturnUpdatedRole()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a role
        var originalName = $"TestRole_{Guid.NewGuid():N}";
        var createCommand = new CreateRoleCommand(originalName);
        var createResponse = await adminClient.PostAsJsonAsync("/api/roles", createCommand);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        // Update the role
        var newName = $"UpdatedRole_{Guid.NewGuid():N}";
        var updateCommand = new UpdateRoleCommand(createdRole!.Id, newName);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/roles/{createdRole.Id}", updateCommand);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedRole = await response.Content.ReadFromJsonAsync<RoleDto>();
        updatedRole.ShouldNotBeNull();
        updatedRole!.Name.ShouldBe(newName);
    }

    [Fact]
    public async Task UpdateRole_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid().ToString();
        var command = new UpdateRoleCommand(invalidId, "NewName");

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/roles/{invalidId}", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRole_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a role
        var originalName = $"TestRole_{Guid.NewGuid():N}";
        var createCommand = new CreateRoleCommand(originalName);
        var createResponse = await adminClient.PostAsJsonAsync("/api/roles", createCommand);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        // Update with empty name
        var updateCommand = new UpdateRoleCommand(createdRole!.Id, "");

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/roles/{createdRole.Id}", updateCommand);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DeleteRole Tests

    [Fact]
    public async Task DeleteRole_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a role
        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var createCommand = new CreateRoleCommand(roleName);
        var createResponse = await adminClient.PostAsJsonAsync("/api/roles", createCommand);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/roles/{createdRole!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted
        var getResponse = await adminClient.GetAsync($"/api/roles/{createdRole.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRole_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/roles/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRole_AdminRole_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Get the Admin role
        var listResponse = await adminClient.GetAsync("/api/roles");
        var roles = await listResponse.Content.ReadFromJsonAsync<PaginatedList<RoleListDto>>();
        var adminRole = roles!.Items.FirstOrDefault(r => r.Name == "Admin");

        // Skip if Admin role not found
        if (adminRole == null)
            return;

        // Act
        var response = await adminClient.DeleteAsync($"/api/roles/{adminRole.Id}");

        // Assert - Should not allow deleting Admin role
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Role Permissions Tests

    [Fact]
    public async Task GetRolePermissions_ValidId_ShouldReturnPermissions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a role with permissions
        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var permissions = new[] { Permissions.UsersRead, Permissions.RolesRead };
        var createCommand = new CreateRoleCommand(roleName, Permissions: permissions);
        var createResponse = await adminClient.PostAsJsonAsync("/api/roles", createCommand);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/roles/{createdRole!.Id}/permissions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var resultPermissions = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>();
        resultPermissions.ShouldNotBeNull();
        resultPermissions.ShouldBe(permissions);
    }

    [Fact]
    public async Task AssignPermissionsToRole_ValidRequest_ShouldReturnUpdatedPermissions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a role without permissions
        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var createCommand = new CreateRoleCommand(roleName);
        var createResponse = await adminClient.PostAsJsonAsync("/api/roles", createCommand);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        // Assign permissions
        var permissions = new[] { Permissions.UsersRead, Permissions.UsersUpdate };
        var assignCommand = new AssignPermissionToRoleCommand(createdRole!.Id, permissions);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/roles/{createdRole.Id}/permissions", assignCommand);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var resultPermissions = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>();
        resultPermissions.ShouldBe(permissions);
    }

    [Fact]
    public async Task RemovePermissionsFromRole_ValidRequest_ShouldReturnUpdatedPermissions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a role with permissions
        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var permissions = new[] { Permissions.UsersRead, Permissions.UsersUpdate, Permissions.RolesRead };
        var createCommand = new CreateRoleCommand(roleName, Permissions: permissions);
        var createResponse = await adminClient.PostAsJsonAsync("/api/roles", createCommand);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>();

        // Remove some permissions
        var permissionsToRemove = new[] { Permissions.UsersUpdate };
        var removeCommand = new RemovePermissionFromRoleCommand(createdRole!.Id, permissionsToRemove);

        // Act
        var response = await adminClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/roles/{createdRole.Id}/permissions")
        {
            Content = JsonContent.Create(removeCommand)
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var resultPermissions = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>();
        resultPermissions.ShouldNotContain(Permissions.UsersUpdate);
        resultPermissions.ShouldContain(Permissions.UsersRead);
        resultPermissions.ShouldContain(Permissions.RolesRead);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task RoleEndpoints_WithoutRequiredPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions via admin API
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion
}
