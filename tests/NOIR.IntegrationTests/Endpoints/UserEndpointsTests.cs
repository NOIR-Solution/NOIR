namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for user management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<(HttpClient client, AuthResponse auth)> CreateTestUserAsync()
    {
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";
        var registerCommand = new RegisterCommand(email, password, "Test", "User");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var userClient = _factory.CreateAuthenticatedClient(auth!.AccessToken);
        return (userClient, auth);
    }

    #region GetUsers Tests

    [Fact]
    public async Task GetUsers_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<UserListDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(u => u.Email == "admin@noir.local");
    }

    [Fact]
    public async Task GetUsers_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/users?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<UserListDto>>();
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetUsers_WithSearch_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/users?search=admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<UserListDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().Contain(u => u.Email.Contains("admin"));
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserById_ValidId_ShouldReturnUser()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get the user list to find a user ID
        var listResponse = await adminClient.GetAsync("/api/users");
        var users = await listResponse.Content.ReadFromJsonAsync<PaginatedList<UserListDto>>();
        var userId = users!.Items.First().Id;

        // Act
        var response = await adminClient.GetAsync($"/api/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_ValidRequest_ShouldReturnUpdatedUser()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a test user
        var (_, auth) = await CreateTestUserAsync();

        // Get the user details first
        var getResponse = await adminClient.GetAsync($"/api/users/{auth.UserId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update the user
        var newFirstName = "UpdatedFirst";
        var newLastName = "UpdatedLast";
        var updateCommand = new UpdateUserCommand(auth.UserId, "Updated Display", newFirstName, newLastName, null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{auth.UserId}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(newFirstName);
        updatedUser.LastName.Should().Be(newLastName);
    }

    [Fact]
    public async Task UpdateUser_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid().ToString();
        var command = new UpdateUserCommand(invalidId, "Display", "First", "Last", null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{invalidId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_SetLockout_ShouldLockUser()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a test user
        var (_, auth) = await CreateTestUserAsync();

        // Update to enable lockout
        var updateCommand = new UpdateUserCommand(auth.UserId, null, null, null, true);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{auth.UserId}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
        updatedUser.Should().NotBeNull();
        updatedUser!.LockoutEnabled.Should().BeTrue();
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a test user
        var (_, auth) = await CreateTestUserAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/users/{auth.UserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteUser_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_AdminUser_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Get the admin user ID
        var listResponse = await adminClient.GetAsync("/api/users");
        var users = await listResponse.Content.ReadFromJsonAsync<PaginatedList<UserListDto>>();
        var adminUser = users!.Items.FirstOrDefault(u => u.Email == "admin@noir.local");

        // Skip if admin user not found
        if (adminUser == null)
            return;

        // Act
        var response = await adminClient.DeleteAsync($"/api/users/{adminUser.Id}");

        // Assert - Should not allow deleting admin user
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region User Roles Tests

    [Fact]
    public async Task GetUserRoles_ValidId_ShouldReturnRoles()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Get admin user ID
        var listResponse = await adminClient.GetAsync("/api/users");
        var users = await listResponse.Content.ReadFromJsonAsync<PaginatedList<UserListDto>>();
        var adminUser = users!.Items.First(u => u.Email == "admin@noir.local");

        // Act
        var response = await adminClient.GetAsync($"/api/users/{adminUser.Id}/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>();
        roles.Should().NotBeNull();
        roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task AssignRolesToUser_ValidRequest_ShouldReturnUpdatedUser()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a role
        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var createRoleCommand = new CreateRoleCommand(roleName);
        await adminClient.PostAsJsonAsync("/api/roles", createRoleCommand);

        // Create a test user
        var (_, auth) = await CreateTestUserAsync();

        // Assign role to user
        var assignCommand = new AssignRolesToUserCommand(auth.UserId, new[] { roleName });

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{auth.UserId}/roles", assignCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
        updatedUser.Should().NotBeNull();
        updatedUser!.Roles.Should().Contain(roleName);
    }

    [Fact]
    public async Task AssignRolesToUser_InvalidRole_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a test user
        var (_, auth) = await CreateTestUserAsync();

        // Try to assign non-existent role
        var assignCommand = new AssignRolesToUserCommand(auth.UserId, new[] { "NonExistentRole" });

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{auth.UserId}/roles", assignCommand);

        // Assert - Returns NotFound because the role doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignRolesToUser_InvalidUserId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid().ToString();
        var assignCommand = new AssignRolesToUserCommand(invalidId, new[] { "User" });

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{invalidId}/roles", assignCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region User Permissions Tests

    [Fact]
    public async Task GetUserPermissions_ValidId_ShouldReturnPermissions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Get admin user ID
        var listResponse = await adminClient.GetAsync("/api/users");
        var users = await listResponse.Content.ReadFromJsonAsync<PaginatedList<UserListDto>>();
        var adminUser = users!.Items.First(u => u.Email == "admin@noir.local");

        // Act
        var response = await adminClient.GetAsync($"/api/users/{adminUser.Id}/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var permissions = await response.Content.ReadFromJsonAsync<UserPermissionsDto>();
        permissions.Should().NotBeNull();
        permissions!.UserId.Should().Be(adminUser.Id);
        permissions.Permissions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUserPermissions_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/users/{Guid.NewGuid()}/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task UserEndpoints_WithoutManageRolesPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
        // Regular users have users:read but not users:manage-roles
        var (userClient, auth) = await CreateTestUserAsync();

        // Act - Try to modify roles (requires users:manage-roles permission)
        var assignCommand = new AssignRolesToUserCommand(auth.UserId, new[] { "User" });
        var response = await userClient.PutAsJsonAsync($"/api/users/{auth.UserId}/roles", assignCommand);

        // Assert - Should be forbidden as regular user doesn't have manage-roles permission
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserEndpoints_UpdateOwnProfile_ThroughUserEndpoint_ShouldReturnForbidden()
    {
        // Arrange - Regular user should not be able to use admin user endpoints
        var (userClient, auth) = await CreateTestUserAsync();
        var updateCommand = new UpdateUserCommand(auth.UserId, "New Display", "First", "Last", null);

        // Act
        var response = await userClient.PutAsJsonAsync($"/api/users/{auth.UserId}", updateCommand);

        // Assert - Should be forbidden as this is an admin endpoint
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
